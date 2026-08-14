import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as signalR from '@microsoft/signalr';

export interface ChatMessage {
    id: number;
    consultationId: number;
    senderId: number;
    content: string;
    createdAt: string;
}

export type CallMode = 'video' | 'audio';
export type CallState = 'idle' | 'ringing' | 'connecting' | 'incoming' | 'in-call';

const RING_TIMEOUT_MS = 30_000;
const CONNECTION_TIMEOUT_MS = 20_000;

type SignalDescription = {
    type: RTCSdpType;
    sdp: string;
};

export const useChatStore = defineStore('chat', () => {
    const config = useRuntimeConfig();

    // ── connection / messaging ──
    const messages = ref<ChatMessage[]>([]);
    const connectionStatus = ref<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');
    const activeConsultationId = ref<number | null>(null);
    const unread = ref<Record<number, number>>({});
    let connection: signalR.HubConnection | null = null;

    // ── call engine ──
    const callState = ref<CallState>('idle');
    const incomingCall = ref<{ consultationId: number; mode: CallMode } | null>(null);
    const activeCallId = ref<number | null>(null);
    const callMode = ref<CallMode>('video');
    const ringSecondsRemaining = ref(0);
    const callDiagnostic = ref('');
    const localStream = ref<MediaStream | null>(null);
    const remoteStream = ref<MediaStream | null>(null);
    const isMuted = ref(false);
    const isCameraOff = ref(false);
    let pc: RTCPeerConnection | null = null;
    let awaitingOffer = false;
    let ringTimeout: ReturnType<typeof setTimeout> | null = null;
    let ringCountdown: ReturnType<typeof setInterval> | null = null;
    let pendingIceCandidates: RTCIceCandidateInit[] = [];
    let disconnectedTimeout: ReturnType<typeof setTimeout> | null = null;
    let connectionTimeout: ReturnType<typeof setTimeout> | null = null;

    const getConnection = () => connection;

    // ═══════════════ CONNECT (once per page, joins ALL groups) ═══════════════
    const connect = async (token: string, consultationIds: number[]) => {
        if (connection) return;

        const hubUrl = `${(config.public.apiBase as string).replace('/api', '')}/hubs/consultations`;
        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();

        registerHandlers();

        connectionStatus.value = 'connecting';
        try {
            await connection.start();
            for (const id of consultationIds) {
                await connection.invoke('JoinConsultation', id);
            }
            connectionStatus.value = 'connected';
        } catch (e) {
            console.error('SignalR Connection Error:', e);
            connectionStatus.value = 'error';
        }

        connection.onreconnecting(() => { connectionStatus.value = 'connecting'; });
        connection.onreconnected(async () => {
            connectionStatus.value = 'connected';
            for (const id of consultationIds) {
                await connection?.invoke('JoinConsultation', id).catch(console.error);
            }
        });
        connection.onclose(() => { connectionStatus.value = 'disconnected'; });
    };

    const registerHandlers = () => {
        if (!connection) return;

        connection.on('ReceiveMessage', (msg: ChatMessage) => {
            if (msg.consultationId === activeConsultationId.value) {
                messages.value.push(msg);
            } else {
                unread.value[msg.consultationId] = (unread.value[msg.consultationId] ?? 0) + 1;
            }
        });

        connection.on('IncomingCall', (p: { mode: CallMode; consultationId: number }) => {
            if (callState.value !== 'idle') return; // busy
            incomingCall.value = p;
            callDiagnostic.value = 'Incoming call received';
            callState.value = 'incoming';
        });

        connection.on('CallAccepted', async (p: { consultationId: number }) => {
            if (activeCallId.value !== p.consultationId) return;
            try {
                clearRingTimeout();
                callState.value = 'connecting';
                callDiagnostic.value = 'Accepted — creating offer';
                await createPeer(callMode.value);
                startConnectionTimeout();
                const offer = await pc!.createOffer();
                await pc!.setLocalDescription(offer);
                const description = getLocalDescription();
                if (!description) throw new Error('Unable to create the WebRTC offer.');
                callDiagnostic.value = 'Offer sent — waiting for answer';
                if (!await safeInvoke('SendWebRtcOffer', p.consultationId, { mode: callMode.value, offer: description, consultationId: p.consultationId })) {
                    cleanupCall();
                }
            } catch (error) {
                console.error('[call] unable to start media/offer', error);
                const message = `Offer error: ${error instanceof Error ? error.message : 'unknown error'}`;
                cleanupCall();
                callDiagnostic.value = message;
            }
        });

        connection.on('CallRejected', (p: { consultationId: number }) => {
            if (activeCallId.value === p.consultationId) cleanupCall();
        });

        connection.on('ReceiveWebRtcOffer', async (p: { mode: CallMode; offer: SignalDescription; consultationId: number }) => {
            if (activeCallId.value !== p.consultationId || !awaitingOffer) return;
            try {
                callDiagnostic.value = 'Offer received — creating answer';
                await pc!.setRemoteDescription(p.offer);
                await addPendingIceCandidates();
                const answer = await pc!.createAnswer();
                await pc!.setLocalDescription(answer);
                const description = getLocalDescription();
                if (!description) throw new Error('Unable to create the WebRTC answer.');
                callDiagnostic.value = 'Answer sent — negotiating media';
                if (!await safeInvoke('SendWebRtcAnswer', p.consultationId, { answer: description, consultationId: p.consultationId })) {
                    cleanupCall();
                }
            } catch (error) {
                console.error('[call] unable to process offer', error);
                const message = `Offer error: ${error instanceof Error ? error.message : 'unknown error'}`;
                cleanupCall();
                callDiagnostic.value = message;
            }
        });

        connection.on('ReceiveWebRtcAnswer', async (p: { answer: SignalDescription; consultationId: number }) => {
            if (activeCallId.value !== p.consultationId) return;
            try {
                callDiagnostic.value = 'Answer received — checking ICE connection';
                await pc?.setRemoteDescription(p.answer);
                await addPendingIceCandidates();
            } catch (error) {
                console.error('[call] unable to process answer', error);
                const message = `Answer error: ${error instanceof Error ? error.message : 'unknown error'}`;
                cleanupCall();
                callDiagnostic.value = message;
            }
        });

        connection.on('ReceiveIceCandidate', async (p: { candidate: RTCIceCandidateInit; consultationId: number }) => {
            if (activeCallId.value === p.consultationId && pc) {
                if (pc.remoteDescription) {
                    await pc.addIceCandidate(p.candidate).catch(console.error);
                    callDiagnostic.value = 'ICE candidate applied';
                } else {
                    pendingIceCandidates.push(p.candidate);
                }
            }
        });

        connection.on('UserEndedCall', (p: { consultationId: number }) => {
            if (activeCallId.value === p.consultationId || incomingCall.value?.consultationId === p.consultationId) {
                cleanupCall();
            }
        });
    };

    const safeInvoke = async (method: string, ...args: any[]): Promise<boolean> => {
        try {
            if (!connection || connection.state !== signalR.HubConnectionState.Connected) return false;
            await connection.invoke(method, ...args);
            return true;
        } catch (e) {
            console.error(`[call] ${method} failed`, e);
            callDiagnostic.value = `SignalR ${method} failed`;
            return false;
        }
    };

    // ═══════════════ CHAT ACTIONS ═══════════════
    const seedUnread = (map: Record<number, number>) => { unread.value = { ...map }; };

    const openChat = async (id: number) => {
        activeConsultationId.value = id;
        unread.value[id] = 0;
        messages.value = [];
        if (connection && connectionStatus.value === 'connected') {
            messages.value = await connection.invoke('GetRecentMessages', id, 50);
        }
    };

    const sendMessage = async (consultationId: number, content: string) => {
        if (!connection || connectionStatus.value !== 'connected') return;
        await connection.invoke('SendMessage', consultationId, content);
    };

    // ═══════════════ WEBRTC ENGINE ═══════════════
    const createPeer = async (mode: CallMode) => {
        const iceServers: RTCIceServer[] = [
            { urls: ['stun:stun.l.google.com:19302', 'stun:stun1.l.google.com:19302'] },
        ];
        const turnUrl = String(config.public.turnUrl || '').trim();
        if (turnUrl) {
            iceServers.push({
                urls: turnUrl,
                username: String(config.public.turnUsername || ''),
                credential: String(config.public.turnCredential || ''),
            });
        }

        pc = new RTCPeerConnection({ iceServers, iceCandidatePoolSize: 10 });
        pc.ontrack = (e) => {
            console.log('🎯 ontrack fired:', e.track.kind, e.track.id);
            console.log('   enabled:', e.track.enabled, 'muted:', e.track.muted);
            if (!remoteStream.value) {
                remoteStream.value = new MediaStream();
            }
            // Avoid duplicate tracks
            if (!remoteStream.value.getTracks().some(track => track.id === e.track.id)) {
                remoteStream.value.addTrack(e.track);
            }
            console.log('Local tracks:', stream.getTracks().map(t => `${t.kind}:${t.enabled}`));
            console.log('Remote track added:', e.track.kind, e.track.id);
        };
        pc.onconnectionstatechange = () => {
            if (!pc) return;
            console.log('[WebRTC] Connection state:', pc.connectionState);
            console.log('[WebRTC] ICE connection state:', pc.iceConnectionState);
            console.log('[WebRTC] ICE gathering state:', pc.iceGatheringState);
            callDiagnostic.value = `Peer connection: ${pc.connectionState}`;

            if (pc.connectionState === 'connected') {
                clearConnectionTimeout();
                callState.value = 'in-call';
            } else if (pc.connectionState === 'disconnected') {
                clearConnectionTimeout();
                disconnectedTimeout = setTimeout(() => {
                    if (pc?.connectionState === 'disconnected') cleanupCall();
                }, 8_000);
            } else if (pc.connectionState === 'failed' || pc.connectionState === 'closed') {
                cleanupCall();
            }
        };
        pc.onicecandidate = (e) => {
            if (e.candidate && activeCallId.value != null) {
                safeInvoke('SendIceCandidate', activeCallId.value, { candidate: e.candidate.toJSON(), consultationId: activeCallId.value });
            }
        };
        pc.oniceconnectionstatechange = () => {
            if (pc) callDiagnostic.value = `ICE connection: ${pc.iceConnectionState}`;
        };
        const stream = await navigator.mediaDevices.getUserMedia({ video: mode === 'video', audio: true });
        localStream.value = stream;
        stream.getTracks().forEach((t) => pc!.addTrack(t, stream));
    };

    const getLocalDescription = (): SignalDescription | null => {
        const description = pc?.localDescription;
        return description?.type && description.sdp
            ? { type: description.type, sdp: description.sdp }
            : null;
    };

    const addPendingIceCandidates = async () => {
        if (!pc?.remoteDescription) return;
        const candidates = pendingIceCandidates;
        pendingIceCandidates = [];
        for (const candidate of candidates) {
            await pc.addIceCandidate(candidate).catch(console.error);
        }
    };

    const clearRingTimeout = () => {
        if (ringTimeout) clearTimeout(ringTimeout);
        if (ringCountdown) clearInterval(ringCountdown);
        ringTimeout = null;
        ringCountdown = null;
        ringSecondsRemaining.value = 0;
    };

    const clearConnectionTimeout = () => {
        if (disconnectedTimeout) clearTimeout(disconnectedTimeout);
        if (connectionTimeout) clearTimeout(connectionTimeout);
        disconnectedTimeout = null;
        connectionTimeout = null;
    };

    const startConnectionTimeout = () => {
        if (connectionTimeout) clearTimeout(connectionTimeout);
        connectionTimeout = setTimeout(() => {
            if (callState.value === 'connecting') cleanupCall();
        }, CONNECTION_TIMEOUT_MS);
    };

    const cleanupCall = () => {
        clearRingTimeout();
        clearConnectionTimeout();
        localStream.value?.getTracks().forEach((t) => t.stop());
        localStream.value = null;
        remoteStream.value = null;
        pc?.close();
        pc = null;
        awaitingOffer = false;
        callDiagnostic.value = '';
        pendingIceCandidates = [];
        activeCallId.value = null;
        incomingCall.value = null;
        isMuted.value = false;
        isCameraOff.value = false;
        callState.value = 'idle';
    };

    const startCall = async (consultationId: number, mode: CallMode) => {
        if (!connection || callState.value !== 'idle') return;
        activeCallId.value = consultationId;
        callMode.value = mode;
        callState.value = 'ringing';
        callDiagnostic.value = 'Calling…';
        if (!await safeInvoke('RequestCall', consultationId, mode)) {
            cleanupCall();
            return;
        }
        ringSecondsRemaining.value = RING_TIMEOUT_MS / 1000;
        ringCountdown = setInterval(() => {
            ringSecondsRemaining.value = Math.max(0, ringSecondsRemaining.value - 1);
        }, 1000);
        ringTimeout = setTimeout(() => {
            if (callState.value === 'ringing' && activeCallId.value === consultationId) {
                cancelCall();
            }
        }, RING_TIMEOUT_MS);
    };

    const cancelCall = async () => {
        if (activeCallId.value != null) await safeInvoke('EndCall', activeCallId.value);
        cleanupCall(); // ✅ always clears locally, even if invoke fails
    };

    const acceptCall = async () => {
        if (!incomingCall.value) return;
        const { consultationId, mode } = incomingCall.value;
        activeCallId.value = consultationId;
        callMode.value = mode;
        incomingCall.value = null;
        awaitingOffer = true;
        callState.value = 'connecting';
        callDiagnostic.value = 'Requesting microphone/camera…';
        try {
            await createPeer(mode);
            startConnectionTimeout();
            if (!await safeInvoke('AcceptCall', consultationId)) cleanupCall();
        } catch (error) {
            console.error('[call] media permission/initialization failed', error);
            const message = `Media error: ${error instanceof Error ? error.message : 'unknown error'}`;
            cleanupCall();
            callDiagnostic.value = message;
        }
    };

    const rejectCall = async () => {
        if (incomingCall.value) await safeInvoke('RejectCall', incomingCall.value.consultationId);
        incomingCall.value = null;
        callState.value = 'idle';
    };

    const endCall = async () => {
        if (activeCallId.value != null) await safeInvoke('EndCall', activeCallId.value);
        cleanupCall();
    };

    const toggleMute = () => {
        localStream.value?.getAudioTracks().forEach((t) => (t.enabled = !t.enabled));
        isMuted.value = !isMuted.value;
    };

    const toggleCamera = () => {
        localStream.value?.getVideoTracks().forEach((t) => (t.enabled = !t.enabled));
        isCameraOff.value = !isCameraOff.value;
    };

    const disconnect = async () => {
        cleanupCall();
        if (connection) {
            try { await connection.stop(); } catch {}
            connection = null;
        }
        connectionStatus.value = 'disconnected';
        messages.value = [];
        activeConsultationId.value = null;
    };

    return {
        messages, connectionStatus, activeConsultationId, unread,
        callState, incomingCall, activeCallId, callMode, ringSecondsRemaining, callDiagnostic, localStream, remoteStream, isMuted, isCameraOff,
        getConnection, connect, disconnect, openChat, sendMessage, seedUnread,
        startCall, cancelCall, acceptCall, rejectCall, endCall, toggleMute, toggleCamera,
    };
});

import { ref, onBeforeUnmount } from 'vue';
import type { HubConnection } from '@microsoft/signalr';

export type CallMode = 'video' | 'audio';

const RTC_CONFIG: RTCConfiguration = {
    iceServers: [{ urls: ['stun:stun.l.google.com:19302', 'stun:stun1.l.google.com:19302'] }],
};

export function useWebRTC(getConnection: () => HubConnection | null, consultationId: number) {
    const localStream = ref<MediaStream | null>(null);
    const remoteStream = ref<MediaStream | null>(null);
    const ringing = ref(false);      // waiting for the other side to answer
    const connecting = ref(false);   // accepted, negotiating
    const inCall = ref(false);
    const incoming = ref<CallMode | null>(null);
    const rejected = ref(false);
    const callMode = ref<CallMode>('video');
    const isMuted = ref(false);
    const isCameraOff = ref(false);

    let pc: RTCPeerConnection | null = null;
    let awaitingOffer = false; // we accepted; the caller's offer is coming

    const createPeer = async (mode: CallMode) => {
        pc = new RTCPeerConnection(RTC_CONFIG);

        // ICE candidate handler
        pc.onicecandidate = (e) => {
            if (e.candidate) {
                getConnection()?.invoke('SendIceCandidate', consultationId, e.candidate.toJSON())
                    .catch(console.error);
            }
        };

        // Robust ontrack handling
        pc.ontrack = (e) => {
            if (!remoteStream.value) {
                remoteStream.value = new MediaStream();
            }
            // Avoid adding the same track twice
            if (!remoteStream.value.getTracks().some(t => t.id === e.track.id)) {
                remoteStream.value.addTrack(e.track);
            }
            console.log('Remote track added:', e.track.kind, e.track.id);
        };

        pc.onconnectionstatechange = () => {
            console.log('Connection state:', pc?.connectionState);
            if (pc && ['disconnected', 'failed', 'closed'].includes(pc.connectionState)) {
                cleanup();
            }
        };

        // Get user media with error handling
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: mode === 'video',
                audio: true
            });
            localStream.value = stream;
            stream.getTracks().forEach((t) => pc!.addTrack(t, stream));
            console.log('Local stream acquired');
        } catch (error) {
            console.error('Failed to get local media:', error);
            // Clean up and show error
            cleanup();
            // Optionally emit an error event to the store/UI
            throw error; // Let caller handle
        }
    };

    const cleanup = () => {
        localStream.value?.getTracks().forEach((t) => t.stop());
        localStream.value = null;
        remoteStream.value = null;
        pc?.close();
        pc = null;
        awaitingOffer = false;
        ringing.value = false;
        connecting.value = false;
        inCall.value = false;
        incoming.value = null;
        isMuted.value = false;
        isCameraOff.value = false;
    };

    const registerListeners = () => {
        const conn = getConnection();
        if (!conn) return;

        ['IncomingCall', 'CallAccepted', 'CallRejected', 'ReceiveWebRtcOffer', 'ReceiveWebRtcAnswer', 'ReceiveIceCandidate', 'UserEndedCall']
            .forEach((m) => conn.off(m));

        // 📞 They called us → show Accept/Reject (busy → ignore)
        conn.on('IncomingCall', (p: { mode: CallMode }) => {
            if (inCall.value || ringing.value || incoming.value) return;
            incoming.value = p.mode;
        });

        // ✅ They accepted → NOW we start camera + send offer
        conn.on('CallAccepted', async () => {
            ringing.value = false;
            connecting.value = true;
            await createPeer(callMode.value);
            const offer = await pc!.createOffer();
            await pc!.setLocalDescription(offer);
            await conn.invoke('SendWebRtcOffer', consultationId, { mode: callMode.value, offer });
        });

        // ❌ They rejected (or we show a brief notice)
        conn.on('CallRejected', () => {
            rejected.value = true;
            setTimeout(() => { rejected.value = false; cleanup(); }, 1500);
        });

        conn.on('ReceiveWebRtcOffer', async (p: { mode: CallMode; offer: RTCSessionDescriptionInit }) => {
            if (awaitingOffer) {
                // We are the callee who accepted: answer the caller's offer
                await pc!.setRemoteDescription(p.offer);
                const answer = await pc!.createAnswer();
                await pc!.setLocalDescription(answer);
                await conn.invoke('SendWebRtcAnswer', consultationId, answer);
            } else if (!inCall.value && !ringing.value) {
                incoming.value = p.mode; // safety fallback
            }
        });

        conn.on('ReceiveWebRtcAnswer', async (answer: RTCSessionDescriptionInit) => {
            if (pc) await pc.setRemoteDescription(answer);
            connecting.value = false;
            inCall.value = true;
        });

        conn.on('ReceiveIceCandidate', async (c: RTCIceCandidateInit) => {
            if (pc) await pc.addIceCandidate(c).catch(console.error);
        });

        conn.on('UserEndedCall', () => cleanup());
    };

    //  Caller: only signal the request — NO camera yet
    const startCall = (mode: CallMode) => {
        callMode.value = mode;
        ringing.value = true;
        getConnection()?.invoke('RequestCall', consultationId, mode);
    };

    const cancelCall = async () => {
        await getConnection()?.invoke('EndCall', consultationId);
        cleanup();
    };

    // 🟢 Callee: accept → start media, tell caller to send the offer
    const acceptCall = async () => {
        const mode = incoming.value ?? 'video';
        callMode.value = mode;
        incoming.value = null;
        awaitingOffer = true;
        connecting.value = true;
        await createPeer(mode);
        await getConnection()?.invoke('AcceptCall', consultationId);
    };

    const rejectCall = async () => {
        await getConnection()?.invoke('RejectCall', consultationId);
        incoming.value = null;
    };

    const endCall = async () => {
        await getConnection()?.invoke('EndCall', consultationId);
        cleanup();
    };

    const toggleMute = () => {
        localStream.value?.getAudioTracks().forEach((t) => (t.enabled = !t.enabled));
        isMuted.value = !isMuted.value;
    };

    const toggleCamera = () => {
        localStream.value?.getVideoTracks().forEach((t) => (t.enabled = !t.enabled));
        isCameraOff.value = !isCameraOff.value;
    };

    onBeforeUnmount(() => cleanup());

    return {
        localStream, remoteStream, ringing, connecting, inCall, incoming, rejected,
        callMode, isMuted, isCameraOff,
        registerListeners, startCall, cancelCall, acceptCall, rejectCall, endCall, toggleMute, toggleCamera,
    };
}
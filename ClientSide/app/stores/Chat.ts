import { defineStore } from 'pinia';
import { ref, watch } from 'vue';
import * as signalR from '@microsoft/signalr';
import type { ChatSummary, FreeInquiry } from '~/types/chat';
import { useNotificationStore } from '~/stores/notifications';

export interface ChatMessage {
    id: number;
    consultationId: number;
    senderId: number;
    content: string;
    createdAt: string;
}

export type CallMode = 'video' | 'audio';
export type CallState =
    | 'idle'
    | 'ringing'
    | 'connecting'
    | 'incoming'
    | 'in-call';

const RING_TIMEOUT_MS = 30_000;
const CONNECTION_TIMEOUT_MS = 20_000;

type SignalDescription = {
    type: RTCSdpType;
    sdp: string;
};

export const useChatStore = defineStore('chat', () => {
    const config = useRuntimeConfig();

    // =========================================================
    // CONNECTION / MESSAGING
    // =========================================================

    const consultations = ref<ChatSummary[]>([]);
    const inquiries = ref<FreeInquiry[]>([]);
    const messages = ref<ChatMessage[]>([]);

    const connectionStatus = ref<
        'connecting' | 'connected' | 'disconnected' | 'error'
    >('disconnected');

    const activeConsultationId = ref<number | null>(null);
    const unread = ref<Record<number, number>>({});

    let connection: signalR.HubConnection | null = null;
    let isInitialized = false;
    let initializePromise: Promise<void> | null = null;
    let lastToken: string | null = null;
    let lastIsLawyer = false;
    let reconcilePromise: Promise<void> | null = null;
    let reconcileAgain = false;

    // Live set of consultation ids we've joined the SignalR group for.
    // Used on reconnect instead of a stale closed-over array, and can
    // be grown later (e.g. when a new consultation is created) via
    // joinConsultation().
    const joinedConsultationIds = ref<Set<number>>(new Set());

    // =========================================================
    // CALL STATE
    // =========================================================

    const callState = ref<CallState>('idle');

    const incomingCall = ref<{
        consultationId: number;
        mode: CallMode;
    } | null>(null);

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

    // =========================================================
    // HELPERS
    // =========================================================

    const getConnection = () => connection;

    const setCallDiagnostic = (message: string) => {
        callDiagnostic.value = message;
        console.log('[Call]', message);
    };

    // =========================================================
    // INITIALIZE GLOBAL CHAT / CALL INFRASTRUCTURE
    // =========================================================

    const waitUntilConnected = async (timeoutMs = 20_000) => {
        if (
            connection &&
            connectionStatus.value === 'connected'
        ) {
            return true;
        }

        if (connectionStatus.value === 'error') {
            return false;
        }

        return new Promise<boolean>((resolve) => {
            const stop = watch(connectionStatus, (status) => {
                if (status === 'connected') {
                    stop();
                    resolve(true);
                } else if (status === 'error') {
                    stop();
                    resolve(false);
                }
            });

            setTimeout(() => {
                stop();
                resolve(
                    connectionStatus.value === 'connected'
                );
            }, timeoutMs);
        });
    };

    const initializeGlobal = async (
        token: string,
        isLawyer: boolean
    ) => {
        lastToken = token;
        lastIsLawyer = isLawyer;

        if (connection && connectionStatus.value === 'connected') {
            return;
        }

        if (initializePromise) {
            return initializePromise;
        }

        initializePromise = (async () => {
            isInitialized = true;

            const base = config.public.apiBase as string;
            const headers = {
                Authorization: `Bearer ${token}`,
            };

            try {
                const [myChats, myInquiries] = await Promise.all([
                    $fetch<ChatSummary[]>(
                        `${base}/consultations/my-consultations`,
                        { headers }
                    ),

                    isLawyer
                        ? $fetch<FreeInquiry[]>(
                            `${base}/consultations/free-messages`,
                            { headers }
                        )
                        : Promise.resolve<FreeInquiry[]>([]),
                ]);

                consultations.value = myChats;
                inquiries.value = myInquiries;

                const unreadMap: Record<number, number> = {};

                myChats.forEach((chat) => {
                    if (chat.unreadCount && chat.unreadCount > 0) {
                        unreadMap[chat.id] = chat.unreadCount;
                    }
                });

                seedUnread(unreadMap);

                await connect(token, myChats.map((chat) => chat.id));
            } catch (error) {
                console.error(
                    '[Chat] Failed to initialize global chat/call infrastructure',
                    error
                );

                connectionStatus.value = 'error';
                isInitialized = false;
            }
        })();

        try {
            await initializePromise;
        } finally {
            if (!connection) {
                initializePromise = null;
            }
        }
    };

    // =========================================================
    // SIGNALR CONNECTION
    // =========================================================

    const connect = async (
        token: string,
        consultationIds: number[]
    ) => {
        if (connection) {
            return;
        }

        const hubUrl =
            `${(config.public.apiBase as string).replace('/api', '')}` +
            `/hubs/consultations`;

        console.log('[SignalR] Connecting to:', hubUrl);

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .build();

        registerHandlers();

        connectionStatus.value = 'connecting';

        try {
            await connection.start();

            console.log('[SignalR] Connected');

            for (const id of consultationIds) {
                await joinConsultation(id);
            }

            connectionStatus.value = 'connected';
        } catch (error) {
            console.error(
                '[SignalR] Failed to start connection',
                error
            );

            connectionStatus.value = 'error';

            // Important:
            // Do not keep a dead connection object around.
            connection = null;
        }

        if (!connection) {
            return;
        }

        connection.onreconnecting((error) => {
            console.warn(
                '[SignalR] Reconnecting...',
                error
            );

            connectionStatus.value = 'connecting';

            if (callState.value !== 'idle') {
                cleanupCall();
                setCallDiagnostic(
                    'Call ended — connection was lost'
                );
            }
        });

        connection.onreconnected(async () => {
            console.log('[SignalR] Reconnected');

            connectionStatus.value = 'connected';

            // Use the live set of joined ids (grown via joinConsultation)
            // rather than the array captured when connect() was first
            // called — otherwise any consultation joined after startup
            // silently never gets rejoined after a reconnect.
            for (const id of Array.from(joinedConsultationIds.value)) {
                await connection
                    ?.invoke('JoinConsultation', id)
                    .catch((error) => {
                        console.error(
                            `[SignalR] Failed to rejoin consultation ${id}`,
                            error
                        );
                    });
            }

            void reconcileAfterReconnect();
        });

        connection.onclose((error) => {
            console.warn(
                '[SignalR] Connection closed',
                error
            );

            connectionStatus.value = 'disconnected';
        });
    };

    // Join (or rejoin) a single consultation group and remember it so
    // reconnect logic stays accurate even for consultations joined
    // after the initial connect() call (e.g. a newly created chat).
    const joinConsultation = async (id: number) => {
        if (!connection) {
            return;
        }

        if (
            joinedConsultationIds.value.has(id) &&
            connection.state ===
                signalR.HubConnectionState.Connected &&
            connectionStatus.value === 'connected'
        ) {
            return;
        }

        try {
            await connection.invoke('JoinConsultation', id);
            joinedConsultationIds.value.add(id);
            console.log(`[SignalR] Joined consultation ${id}`);
        } catch (error) {
            console.error(
                `[SignalR] Failed to join consultation ${id}`,
                error
            );
        }
    };

    // =========================================================
    // SIGNALR HANDLERS
    // =========================================================

    const registerHandlers = () => {
        if (!connection) {
            return;
        }

        // -----------------------------------------------------
        // MESSAGES
        // -----------------------------------------------------

        connection.on(
            'ReceiveMessage',
            (msg: ChatMessage) => {
                if (
                    msg.consultationId ===
                    activeConsultationId.value
                ) {
                    messages.value.push(msg);
                } else {
                    unread.value[msg.consultationId] =
                        (unread.value[msg.consultationId] ?? 0) + 1;
                }
            }
        );

        connection.on(
            'ReceiveNotification',
            (payload: {
                id?: number
                Id?: number
                title?: string
                Title?: string
                message?: string
                Message?: string
                isRead?: boolean
                IsRead?: boolean
                createdAt?: string
                CreatedAt?: string
                consultationId?: number | null
                ConsultationId?: number | null
            }) => {
                useNotificationStore().ingest({
                    id: payload.id ?? payload.Id ?? Date.now(),
                    title: payload.title ?? payload.Title ?? '',
                    message: payload.message ?? payload.Message ?? '',
                    isRead: payload.isRead ?? payload.IsRead ?? false,
                    createdAt:
                        payload.createdAt ??
                        payload.CreatedAt ??
                        new Date().toISOString(),
                })

                const consultationId = Number(
                    payload.consultationId ?? payload.ConsultationId
                )
                if (Number.isFinite(consultationId) && consultationId > 0) {
                    void syncConsultation(consultationId)
                }
            }
        );

        // -----------------------------------------------------
        // OS NOTIFICATION
        // -----------------------------------------------------

        const requestNotificationPermission = async () => {
            if (
                'Notification' in window &&
                Notification.permission === 'default'
            ) {
                try {
                    await Notification.requestPermission();
                } catch (error) {
                    console.warn(
                        '[Notification] Permission request failed',
                        error
                    );
                }
            }
        };

        const showCallNotification = () => {
            if (
                !('Notification' in window) ||
                Notification.permission !== 'granted' ||
                !incomingCall.value
            ) {
                return;
            }

            const isVideo =
                incomingCall.value.mode === 'video';

            const notification = new Notification(
                'مكالمة واردة',
                {
                    body: `لديك ${
                        isVideo
                            ? 'مكالمة فيديو'
                            : 'مكالمة صوتية'
                    } جديدة`,
                    icon: '/favicon.ico',
                    requireInteraction: true,
                }
            );

            notification.onclick = () => {
                window.focus();
                notification.close();
            };

            const checkState = setInterval(() => {
                if (callState.value !== 'incoming') {
                    notification.close();
                    clearInterval(checkState);
                }
            }, 1000);
        };

        // Ask once when handlers are initialized.
        requestNotificationPermission().catch(console.error);

        // -----------------------------------------------------
        // INCOMING CALL
        // -----------------------------------------------------

        connection.on(
            'IncomingCall',
            (p: {
                mode: CallMode;
                consultationId: number;
            }) => {
                if (callState.value !== 'idle') {
                    return;
                }

                incomingCall.value = p;
                activeCallId.value = p.consultationId;
                callMode.value = p.mode;

                setCallDiagnostic(
                    'Incoming call received'
                );

                // This state is watched by CallOverlay.vue.
                // CallOverlay is responsible for playing
                // the actual ringtone.
                callState.value = 'incoming';

                showCallNotification();
            }
        );

        // -----------------------------------------------------
        // CALL ACCEPTED
        // Caller receives this after callee accepts.
        // -----------------------------------------------------

        connection.on(
            'CallAccepted',
            async (p: { consultationId: number }) => {
                if (
                    activeCallId.value !==
                    p.consultationId
                ) {
                    return;
                }

                try {
                    clearRingTimeout();

                    callState.value = 'connecting';

                    setCallDiagnostic(
                        'Accepted — creating offer'
                    );

                    await createPeer(callMode.value);

                    startConnectionTimeout();

                    const offer =
                        await pc!.createOffer();

                    await pc!.setLocalDescription(
                        offer
                    );

                    const description =
                        getLocalDescription();

                    if (!description) {
                        throw new Error(
                            'Unable to create the WebRTC offer.'
                        );
                    }

                    setCallDiagnostic(
                        'Offer sent — waiting for answer'
                    );

                    const success = await safeInvoke(
                        'SendWebRtcOffer',
                        p.consultationId,
                        {
                            mode: callMode.value,
                            offer: description,
                            consultationId:
                            p.consultationId,
                        }
                    );

                    if (!success) {
                        await failCall(p.consultationId);
                    }
                } catch (error) {
                    console.error(
                        '[Call] Unable to start media/offer',
                        error
                    );

                    const message =
                        `Offer error: ${
                            error instanceof Error
                                ? error.message
                                : 'unknown error'
                        }`;

                    // Tell the other side we're bailing instead of
                    // leaving them ringing/connecting until they time out.
                    await failCall(p.consultationId, message);
                }
            }
        );

        // -----------------------------------------------------
        // CALL REJECTED
        // -----------------------------------------------------

        connection.on(
            'CallRejected',
            (p: { consultationId: number }) => {
                if (
                    activeCallId.value ===
                    p.consultationId
                ) {
                    setCallDiagnostic(
                        'Call rejected'
                    );

                    cleanupCall();
                }
            }
        );

        // -----------------------------------------------------
        // WEBRTC OFFER
        // -----------------------------------------------------

        connection.on(
            'ReceiveWebRtcOffer',
            async (p: {
                mode: CallMode;
                offer: SignalDescription;
                consultationId: number;
            }) => {
                if (
                    activeCallId.value !==
                    p.consultationId ||
                    !awaitingOffer
                ) {
                    return;
                }

                try {
                    setCallDiagnostic(
                        'Offer received — creating answer'
                    );

                    await pc!.setRemoteDescription(
                        p.offer
                    );

                    await addPendingIceCandidates();

                    const answer =
                        await pc!.createAnswer();

                    await pc!.setLocalDescription(
                        answer
                    );

                    const description =
                        getLocalDescription();

                    if (!description) {
                        throw new Error(
                            'Unable to create the WebRTC answer.'
                        );
                    }

                    setCallDiagnostic(
                        'Answer sent — negotiating media'
                    );

                    const success = await safeInvoke(
                        'SendWebRtcAnswer',
                        p.consultationId,
                        {
                            answer: description,
                            consultationId:
                            p.consultationId,
                        }
                    );

                    if (!success) {
                        await failCall(p.consultationId);
                    }
                } catch (error) {
                    console.error(
                        '[Call] Unable to process offer',
                        error
                    );

                    const message =
                        `Offer error: ${
                            error instanceof Error
                                ? error.message
                                : 'unknown error'
                        }`;

                    await failCall(p.consultationId, message);
                }
            }
        );

        // -----------------------------------------------------
        // WEBRTC ANSWER
        // -----------------------------------------------------

        connection.on(
            'ReceiveWebRtcAnswer',
            async (p: {
                answer: SignalDescription;
                consultationId: number;
            }) => {
                if (
                    activeCallId.value !==
                    p.consultationId
                ) {
                    return;
                }

                try {
                    setCallDiagnostic(
                        'Answer received — checking ICE connection'
                    );

                    await pc?.setRemoteDescription(
                        p.answer
                    );

                    await addPendingIceCandidates();
                } catch (error) {
                    console.error(
                        '[Call] Unable to process answer',
                        error
                    );

                    const message =
                        `Answer error: ${
                            error instanceof Error
                                ? error.message
                                : 'unknown error'
                        }`;

                    await failCall(p.consultationId, message);
                }
            }
        );

        // -----------------------------------------------------
        // ICE CANDIDATES
        // -----------------------------------------------------

        connection.on(
            'ReceiveIceCandidate',
            async (p: {
                candidate: RTCIceCandidateInit;
                consultationId: number;
            }) => {
                if (
                    activeCallId.value ===
                    p.consultationId &&
                    pc
                ) {
                    if (pc.remoteDescription) {
                        await pc
                            .addIceCandidate(
                                p.candidate
                            )
                            .catch(console.error);

                        setCallDiagnostic(
                            'ICE candidate applied'
                        );
                    } else {
                        pendingIceCandidates.push(
                            p.candidate
                        );
                    }
                }
            }
        );

        // -----------------------------------------------------
        // USER ENDED CALL
        // -----------------------------------------------------

        connection.on(
            'UserEndedCall',
            (p: { consultationId: number }) => {
                if (
                    activeCallId.value ===
                    p.consultationId ||
                    incomingCall.value
                        ?.consultationId ===
                    p.consultationId
                ) {
                    cleanupCall();
                }
            }
        );
    };

    // =========================================================
    // SAFE SIGNALR INVOKE
    // =========================================================

    const safeInvoke = async (
        method: string,
        ...args: any[]
    ): Promise<boolean> => {
        try {
            if (
                !connection ||
                connection.state !==
                signalR.HubConnectionState.Connected
            ) {
                console.warn(
                    `[SignalR] Cannot call ${method}: connection is not connected`
                );

                setCallDiagnostic(
                    'SignalR connection is not connected'
                );

                return false;
            }

            await connection.invoke(
                method,
                ...args
            );

            return true;
        } catch (error) {
            console.error(
                `[Call] ${method} failed`,
                error
            );

            setCallDiagnostic(
                `SignalR ${method} failed`
            );

            return false;
        }
    };

    // =========================================================
    // CHAT ACTIONS
    // =========================================================

    const seedUnread = (
        map: Record<number, number>
    ) => {
        unread.value = {
            ...map,
        };
    };

    const openChat = async (id: number) => {
        unread.value[id] = 0;

        const token =
            lastToken ??
            useCookie<string | null>('auth_token').value;

        if (token) {
            await initializeGlobal(token, lastIsLawyer);
        }

        await waitUntilConnected();
        await joinConsultation(id);

        let history: ChatMessage[] = [];

        if (
            connection &&
            connectionStatus.value === 'connected'
        ) {
            try {
                history = await connection.invoke(
                    'GetRecentMessages',
                    id,
                    50
                );
            } catch (error) {
                console.error(
                    '[Chat] Failed to load messages',
                    error
                );
            }
        }

        messages.value = history;
        activeConsultationId.value = id;
    };

    const ensureConsultation = async (
        summary: Partial<ChatSummary> & { id: number }
    ) => {
        const id = summary.id;
        if (!Number.isFinite(id) || id <= 0) {
            return;
        }

        const existingIndex = consultations.value.findIndex(
            (chat) => chat.id === id
        );

        if (existingIndex >= 0) {
            const existing = consultations.value[existingIndex];
            const next: ChatSummary = { ...existing, id };
            for (const [key, value] of Object.entries(summary)) {
                if (value !== undefined) {
                    (next as Record<string, unknown>)[key] = value;
                }
            }
            consultations.value.splice(existingIndex, 1, next);
        } else {
            consultations.value = [
                {
                    id,
                    otherUserName: summary.otherUserName ?? '',
                    otherUserImageUrl:
                        summary.otherUserImageUrl ?? null,
                    otherUserRole: summary.otherUserRole ?? '',
                    status: summary.status ?? 'Pending',
                    scheduledAt: summary.scheduledAt ?? '',
                    durationMinutes: summary.durationMinutes ?? 0,
                    lastMessageContent:
                        summary.lastMessageContent ?? null,
                    lastMessageDate: summary.lastMessageDate ?? null,
                    lastMessageSenderId:
                        summary.lastMessageSenderId ?? null,
                    isOnline: summary.isOnline ?? false,
                    unreadCount: summary.unreadCount ?? 0,
                },
                ...consultations.value,
            ];
        }

        const token =
            lastToken ??
            useCookie<string | null>('auth_token').value;

        if (token) {
            await initializeGlobal(token, lastIsLawyer);
        }

        await waitUntilConnected();
        await joinConsultation(id);
    };

    const syncConsultation = async (id: number) => {
        if (!Number.isFinite(id) || id <= 0) {
            return;
        }

        const token =
            lastToken ??
            useCookie<string | null>('auth_token').value;

        if (!token) {
            return;
        }

        const base = config.public.apiBase as string;

        try {
            const details = await $fetch<{
                consultationId: number
                otherUserName: string
                otherUserImageUrl: string | null
                otherUserRole: string
                status: string
                scheduledAt: string
                durationMinutes: number
                lastMessageContent: string | null
                lastMessageDate: string | null
                lastMessageSenderId: number | null
                isOnline: boolean
            }>(`${base}/consultations/${id}/details`, {
                headers: { Authorization: `Bearer ${token}` },
            });

            await ensureConsultation({
                id: details.consultationId ?? id,
                otherUserName: details.otherUserName,
                otherUserImageUrl: details.otherUserImageUrl,
                otherUserRole: details.otherUserRole,
                status: details.status,
                scheduledAt: details.scheduledAt,
                durationMinutes: details.durationMinutes,
                lastMessageContent: details.lastMessageContent,
                lastMessageDate: details.lastMessageDate,
                lastMessageSenderId: details.lastMessageSenderId,
                isOnline: details.isOnline,
            });
        } catch (error) {
            console.error(
                `[Chat] Failed to sync consultation ${id}`,
                error
            );
        }
    };

    const mergeRecentMessages = (incoming: ChatMessage[]) => {
        const byId = new Map<number, ChatMessage>();

        for (const msg of messages.value) {
            if (Number.isFinite(msg.id) && msg.id > 0) {
                byId.set(msg.id, msg);
            }
        }

        for (const msg of incoming) {
            if (Number.isFinite(msg.id) && msg.id > 0) {
                byId.set(msg.id, msg);
            }
        }

        messages.value = Array.from(byId.values()).sort((a, b) => {
            const aTime = new Date(a.createdAt).getTime();
            const bTime = new Date(b.createdAt).getTime();
            return aTime - bTime;
        });
    };

    const reconcileAfterReconnect = async () => {
        if (reconcilePromise) {
            reconcileAgain = true;
            return reconcilePromise;
        }

        reconcilePromise = (async () => {
            do {
                reconcileAgain = false;

                try {
                    const token =
                        lastToken ??
                        useCookie<string | null>('auth_token').value;

                    if (token) {
                        try {
                            const base = config.public.apiBase as string;
                            const myChats = await $fetch<ChatSummary[]>(
                                `${base}/consultations/my-consultations`,
                                {
                                    headers: {
                                        Authorization: `Bearer ${token}`,
                                    },
                                }
                            );

                            const unreadMap: Record<number, number> = {
                                ...unread.value,
                            };

                            for (const chat of myChats) {
                                await ensureConsultation(chat);

                                if (
                                    chat.id ===
                                    activeConsultationId.value
                                ) {
                                    unreadMap[chat.id] = 0;
                                } else if (
                                    typeof chat.unreadCount === 'number'
                                ) {
                                    unreadMap[chat.id] = chat.unreadCount;
                                }
                            }

                            seedUnread(unreadMap);
                        } catch (error) {
                            console.error(
                                '[Chat] Failed to reconcile consultations after reconnect',
                                error
                            );
                        }
                    }

                    const activeId = activeConsultationId.value;

                    if (
                        activeId &&
                        connection &&
                        connectionStatus.value === 'connected'
                    ) {
                        try {
                            const history = await connection.invoke<
                                ChatMessage[]
                            >('GetRecentMessages', activeId, 50);

                            mergeRecentMessages(history ?? []);
                        } catch (error) {
                            console.error(
                                '[Chat] Failed to reconcile messages after reconnect',
                                error
                            );
                        }
                    }
                } catch (error) {
                    console.error(
                        '[Chat] Failed to reconcile after reconnect',
                        error
                    );
                }
            } while (reconcileAgain);
        })();

        try {
            await reconcilePromise;
        } finally {
            reconcilePromise = null;
        }
    };

    const sendMessage = async (
        consultationId: number,
        content: string
    ) => {
        if (
            !connection ||
            connectionStatus.value !==
            'connected'
        ) {
            console.warn(
                '[Chat] Cannot send message: SignalR is not connected'
            );
            return;
        }

        try {
            await connection.invoke(
                'SendMessage',
                consultationId,
                content
            );
        } catch (error) {
            console.error(
                '[Chat] Failed to send message',
                error
            );
        }
    };

    // =========================================================
    // WEBRTC ENGINE
    // =========================================================

    const createPeer = async (
        mode: CallMode
    ) => {
        const iceServers: RTCIceServer[] = [
            {
                urls: [
                    'stun:stun.l.google.com:19302',
                    'stun:stun1.l.google.com:19302',
                ],
            },
        ];

        const turnUrl = String(
            config.public.turnUrl || ''
        ).trim();

        if (turnUrl) {
            iceServers.push({
                urls: turnUrl,
                username: String(
                    config.public.turnUsername || ''
                ),
                credential: String(
                    config.public.turnCredential || ''
                ),
            });
        }

        pc = new RTCPeerConnection({
            iceServers,
            iceCandidatePoolSize: 10,
        });

        pc.ontrack = (event) => {
            console.log(
                '🎯 [WebRTC] ontrack:',
                event.track.kind,
                event.track.id
            );

            if (!remoteStream.value) {
                remoteStream.value =
                    new MediaStream();
            }

            if (
                !remoteStream.value
                    .getTracks()
                    .some(
                        (track) =>
                            track.id ===
                            event.track.id
                    )
            ) {
                remoteStream.value.addTrack(
                    event.track
                );
            }

            console.log(
                '[WebRTC] Remote track added:',
                event.track.kind,
                event.track.id
            );
        };

        pc.onconnectionstatechange = () => {
            if (!pc) {
                return;
            }

            console.log(
                '[WebRTC] Connection state:',
                pc.connectionState
            );

            console.log(
                '[WebRTC] ICE connection state:',
                pc.iceConnectionState
            );

            console.log(
                '[WebRTC] ICE gathering state:',
                pc.iceGatheringState
            );

            callDiagnostic.value =
                `Peer connection: ${pc.connectionState}`;

            if (
                pc.connectionState ===
                'connected'
            ) {
                clearConnectionTimeout();

                callState.value =
                    'in-call';
            } else if (
                pc.connectionState ===
                'disconnected'
            ) {
                clearConnectionTimeout();

                disconnectedTimeout =
                    setTimeout(() => {
                        if (
                            pc?.connectionState ===
                            'disconnected'
                        ) {
                            cleanupCall();
                        }
                    }, 8_000);
            } else if (
                pc.connectionState ===
                'failed' ||
                pc.connectionState ===
                'closed'
            ) {
                cleanupCall();
            }
        };

        pc.onicecandidate = (event) => {
            if (
                event.candidate &&
                activeCallId.value != null
            ) {
                safeInvoke(
                    'SendIceCandidate',
                    activeCallId.value,
                    {
                        candidate:
                            event.candidate.toJSON(),
                        consultationId:
                        activeCallId.value,
                    }
                );
            }
        };

        pc.oniceconnectionstatechange =
            () => {
                if (pc) {
                    callDiagnostic.value =
                        `ICE connection: ${pc.iceConnectionState}`;
                }
            };

        try {
            const stream =
                await navigator.mediaDevices.getUserMedia(
                    {
                        video:
                            mode === 'video',
                        audio: {
                            echoCancellation: true,
                            noiseSuppression: true,
                            autoGainControl: true,
                        },
                    }
                );

            localStream.value = stream;

            stream.getTracks().forEach(
                (track) => {
                    pc!.addTrack(
                        track,
                        stream
                    );
                }
            );
        } catch (error) {
            console.error(
                '[WebRTC] getUserMedia failed',
                error
            );

            pc?.close();
            pc = null;

            throw error;
        }
    };

    const getLocalDescription =
        (): SignalDescription | null => {
            const description =
                pc?.localDescription;

            return description?.type &&
            description.sdp
                ? {
                    type:
                    description.type,
                    sdp:
                    description.sdp,
                }
                : null;
        };

    const addPendingIceCandidates =
        async () => {
            if (!pc?.remoteDescription) {
                return;
            }

            const candidates =
                pendingIceCandidates;

            pendingIceCandidates = [];

            for (const candidate of candidates) {
                await pc
                    .addIceCandidate(
                        candidate
                    )
                    .catch(console.error);
            }
        };

    // =========================================================
    // TIMER HELPERS
    // =========================================================

    const clearRingTimeout = () => {
        if (ringTimeout) {
            clearTimeout(ringTimeout);
        }

        if (ringCountdown) {
            clearInterval(
                ringCountdown
            );
        }

        ringTimeout = null;
        ringCountdown = null;
        ringSecondsRemaining.value = 0;
    };

    const clearConnectionTimeout =
        () => {
            if (disconnectedTimeout) {
                clearTimeout(
                    disconnectedTimeout
                );
            }

            if (connectionTimeout) {
                clearTimeout(
                    connectionTimeout
                );
            }

            disconnectedTimeout = null;
            connectionTimeout = null;
        };

    const startConnectionTimeout =
        () => {
            if (connectionTimeout) {
                clearTimeout(
                    connectionTimeout
                );
            }

            connectionTimeout =
                setTimeout(() => {
                    if (
                        callState.value ===
                        'connecting'
                    ) {
                        setCallDiagnostic(
                            'Connection timeout'
                        );

                        cleanupCall();
                    }
                }, CONNECTION_TIMEOUT_MS);
        };

    // =========================================================
    // CLEANUP
    // =========================================================

    const cleanupCall = () => {
        clearRingTimeout();
        clearConnectionTimeout();

        localStream.value
            ?.getTracks()
            .forEach((track) =>
                track.stop()
            );

        localStream.value = null;
        remoteStream.value = null;

        pc?.close();
        pc = null;

        awaitingOffer = false;
        pendingIceCandidates = [];

        activeCallId.value = null;
        incomingCall.value = null;

        isMuted.value = false;
        isCameraOff.value = false;

        callDiagnostic.value = '';
        callState.value = 'idle';
    };

    // Local failure during offer/answer negotiation: notify the other
    // side via EndCall so they don't sit ringing/connecting until their
    // own timeout fires, then clean up locally.
    const failCall = async (
        consultationId: number,
        message?: string
    ) => {
        await safeInvoke('EndCall', consultationId);

        cleanupCall();

        if (message) {
            callDiagnostic.value = message;
        }
    };

    // =========================================================
    // START OUTGOING CALL
    // =========================================================

    const startCall = async (
        consultationId: number,
        mode: CallMode
    ) => {
        if (callState.value !== 'idle') {
            console.warn(
                '[Call] Cannot start call: another call is active'
            );
            return;
        }

        if (
            !connection ||
            connection.state !==
            signalR.HubConnectionState.Connected
        ) {
            console.error(
                '[Call] Cannot start call: SignalR is not connected'
            );

            setCallDiagnostic(
                'Server connection unavailable'
            );

            connectionStatus.value =
                'disconnected';

            return;
        }

        activeCallId.value =
            consultationId;

        callMode.value = mode;

        setCallDiagnostic(
            'Calling…'
        );

        // IMPORTANT:
        // Set ringing only after RequestCall succeeds.
        const requested = await safeInvoke(
            'RequestCall',
            consultationId,
            mode
        );

        if (!requested) {
            cleanupCall();
            return;
        }

        callState.value = 'ringing';

        ringSecondsRemaining.value =
            RING_TIMEOUT_MS / 1000;

        ringCountdown =
            setInterval(() => {
                ringSecondsRemaining.value =
                    Math.max(
                        0,
                        ringSecondsRemaining.value -
                        1
                    );

                // Self-terminate instead of relying solely on other
                // code paths to clear the interval.
                if (ringSecondsRemaining.value === 0 && ringCountdown) {
                    clearInterval(ringCountdown);
                    ringCountdown = null;
                }
            }, 1000);

        ringTimeout = setTimeout(() => {
            if (
                callState.value ===
                'ringing' &&
                activeCallId.value ===
                consultationId
            ) {
                cancelCall();
            }
        }, RING_TIMEOUT_MS);
    };

    // =========================================================
    // CANCEL OUTGOING CALL
    // =========================================================

    const cancelCall = async () => {
        const callId =
            activeCallId.value;

        if (callId != null) {
            await safeInvoke(
                'EndCall',
                callId
            );
        }

        cleanupCall();
    };

    // =========================================================
    // ACCEPT INCOMING CALL
    // =========================================================

    const acceptCall = async () => {
        if (!incomingCall.value) {
            return;
        }

        if (
            !connection ||
            connection.state !==
            signalR.HubConnectionState.Connected
        ) {
            console.error(
                '[Call] Cannot accept call: SignalR is not connected'
            );

            setCallDiagnostic(
                'Server connection unavailable'
            );

            return;
        }

        const {
            consultationId,
            mode,
        } = incomingCall.value;

        activeCallId.value =
            consultationId;

        callMode.value = mode;

        incomingCall.value = null;

        awaitingOffer = true;

        callState.value =
            'connecting';

        setCallDiagnostic(
            'Requesting microphone/camera…'
        );

        try {
            await createPeer(mode);

            startConnectionTimeout();

            const accepted =
                await safeInvoke(
                    'AcceptCall',
                    consultationId
                );

            if (!accepted) {
                await failCall(consultationId);
            }
        } catch (error) {
            console.error(
                '[Call] Media permission/initialization failed',
                error
            );

            const message =
                `Media error: ${
                    error instanceof Error
                        ? error.message
                        : 'unknown error'
                }`;

            // Let the caller know we couldn't accept instead of
            // leaving them stuck in "connecting" for 20s.
            await failCall(consultationId, message);
        }
    };

    // =========================================================
    // REJECT INCOMING CALL
    // =========================================================

    const rejectCall = async () => {
        const call =
            incomingCall.value;

        if (call) {
            await safeInvoke(
                'RejectCall',
                call.consultationId
            );
        }

        incomingCall.value = null;
        activeCallId.value = null;
        callState.value = 'idle';
    };

    // =========================================================
    // END ACTIVE CALL
    // =========================================================

    const endCall = async () => {
        const callId =
            activeCallId.value;

        if (callId != null) {
            await safeInvoke(
                'EndCall',
                callId
            );
        }

        cleanupCall();
    };

    // =========================================================
    // CALL CONTROLS
    // =========================================================

    const toggleMute = () => {
        // Drive tracks from the *new* desired state rather than
        // inverting each track's own `enabled` flag — keeps every
        // track (including ones added later via renegotiation) in
        // sync with `isMuted` instead of potentially drifting apart.
        const nextMuted = !isMuted.value;

        localStream.value
            ?.getAudioTracks()
            .forEach((track) => {
                track.enabled = !nextMuted;
            });

        isMuted.value = nextMuted;
    };

    const toggleCamera = () => {
        const nextCameraOff = !isCameraOff.value;

        localStream.value
            ?.getVideoTracks()
            .forEach((track) => {
                track.enabled = !nextCameraOff;
            });

        isCameraOff.value = nextCameraOff;
    };

    // =========================================================
    // DISCONNECT EVERYTHING
    // =========================================================

    const disconnect = async () => {
        cleanupCall();

        if (connection) {
            try {
                await connection.stop();
            } catch (error) {
                console.warn(
                    '[SignalR] Stop failed',
                    error
                );
            }

            connection = null;
        }

        isInitialized = false;
        initializePromise = null;
        lastToken = null;
        lastIsLawyer = false;
        joinedConsultationIds.value = new Set();

        connectionStatus.value =
            'disconnected';

        messages.value = [];
        activeConsultationId.value =
            null;
    };

    // =========================================================
    // PUBLIC API
    // =========================================================

    return {
        // Chat
        messages,
        connectionStatus,
        activeConsultationId,
        unread,
        consultations,
        inquiries,

        // Call state
        callState,
        incomingCall,
        activeCallId,
        callMode,
        ringSecondsRemaining,
        callDiagnostic,

        localStream,
        remoteStream,

        isMuted,
        isCameraOff,

        // Connection
        getConnection,
        connect,
        disconnect,
        joinConsultation,
        ensureConsultation,
        syncConsultation,

        // Chat actions
        openChat,
        sendMessage,
        seedUnread,
        initializeGlobal,

        // Call actions
        startCall,
        cancelCall,
        acceptCall,
        rejectCall,
        endCall,

        // Controls
        toggleMute,
        toggleCamera,
    };
});
import * as signalR from "@microsoft/signalr";

export interface Message {
    id: number;
    consultationId: number;
    senderId: number;
    content: string;
    createdAt: string;
    isMine?: boolean;
}

export const useSignalRChat = (consultationId: number, currentUserId: number) => {
    const messages = ref<Message[]>([]);
    const isConnected = ref(false);
    const isConnecting = ref(false);
    const connectionError = ref<string | null>(null);

    let connection: signalR.HubConnection | null = null;

    const startConnection = async () => {
        if (isConnecting.value) return;
        isConnecting.value = true;
        connectionError.value = null;

        try {
            const token = useCookie("auth_token").value;
            const config = useRuntimeConfig();
            const apiBase = String(config.public.apiBase || "http://localhost:5112/api/");
            const hubUrl = new URL("../hubs/consultations", apiBase).toString();

            connection = new signalR.HubConnectionBuilder()
                .withUrl(`${hubUrl}?consultationId=${consultationId}`, {
                    accessTokenFactory: () => token || "",
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Warning)
                .build();

            // Listen for incoming messages
            connection.on("ReceiveMessage", (message: Message) => {
                messages.value.push({
                    ...message,
                    isMine: message.senderId === currentUserId,
                });
            });

            // Handle disconnects
            connection.onclose(() => {
                isConnected.value = false;
                isConnecting.value = false;
            });

            connection.onreconnecting(() => {
                isConnected.value = false;
            });

            connection.onreconnected(() => {
                isConnected.value = true;
                // Rejoin the consultation group after reconnect
                connection?.invoke("JoinConsultation", consultationId).catch(console.error);
            });

            await connection.start();
            await connection.invoke("JoinConsultation", consultationId);

            isConnected.value = true;
            isConnecting.value = false;

            // Load recent messages
            await loadRecentMessages();
        } catch (err: any) {
            connectionError.value = err.message || "فشل الاتصال بالخادم";
            isConnecting.value = false;
            console.error("SignalR Connection Error:", err);
        }
    };

    const loadRecentMessages = async () => {
        if (!connection) return;
        try {
            const recent = await connection.invoke<any[]>("GetRecentMessages", consultationId, 50);
            messages.value = recent.map((m) => ({
                ...m,
                isMine: m.senderId === currentUserId,
            }));
        } catch (err) {
            console.error("Failed to load messages:", err);
        }
    };

    const sendMessage = async (content: string) => {
        if (!connection || !isConnected.value) {
            throw new Error("غير متصل بالخادم");
        }
        await connection.invoke("SendMessage", consultationId, content);
    };

    const stopConnection = async () => {
        if (connection) {
            await connection.stop();
            connection = null;
            isConnected.value = false;
        }
    };

    // Auto-cleanup
    onUnmounted(() => {
        stopConnection();
    });

    return {
        messages,
        isConnected,
        isConnecting,
        connectionError,
        startConnection,
        sendMessage,
        stopConnection,
    };
};

import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as signalR from '@microsoft/signalr';

export interface ChatMessage {
    id: number;
    consultationId: number;
    senderId: number;
    content: string;
    createdAt: string;
}

export interface ChatSummary {
    id: number;
    otherUserName: string;
    otherUserImageUrl: string | null;
    otherUserRole: string;
    status: string;
    scheduledAt: string;
    durationMinutes: number;
    lastMessageContent: string | null;
    lastMessageDate: string | null;
    lastMessageSenderId: number | null;
    isOnline: boolean;
    unreadCount?: number;
}

export interface FreeInquiry {
    id: number;
    senderName: string;
    senderPhone: string;
    senderEmail: string;
    content: string;
    createdAt: string;
    isRepliedTo: boolean;
}

export const useChatStore = defineStore('chat', () => {
    const messages = ref<ChatMessage[]>([]);
    const connectionStatus = ref<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');
    let connection: signalR.HubConnection | null = null;

    const connect = async (consultationId: number, token: string, currentUserId: number) => {
        if (connection) {
            await disconnect();
        }

        const config = useRuntimeConfig();
        const hubUrl = `${config.public.apiBase.replace('/api', '')}/hubs/consultation`;

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (message: ChatMessage) => {
            messages.value.push(message);
        });

        connection.onreconnecting(() => {
            connectionStatus.value = 'connecting';
        });

        connection.onreconnected(() => {
            connectionStatus.value = 'connected';
        });

        connection.onclose(() => {
            connectionStatus.value = 'disconnected';
        });

        try {
            connectionStatus.value = 'connecting';
            await connection.start();
            await connection.invoke('JoinConsultation', consultationId);
            connectionStatus.value = 'connected';
        } catch (err) {
            console.error('SignalR Connection Error:', err);
            connectionStatus.value = 'error';
        }
    };

    const disconnect = async () => {
        if (connection) {
            try {
                await connection.stop();
            } catch (e) {
                console.error('Error stopping connection:', e);
            }
            connection = null;
            connectionStatus.value = 'disconnected';
            messages.value = [];
        }
    };

    const sendMessage = async (consultationId: number, content: string) => {
        if (!connection || connectionStatus.value !== 'connected') return;
        try {
            await connection.invoke('SendMessage', consultationId, content);
        } catch (err: any) {
            throw new Error(err.message || 'Failed to send message');
        }
    };

    const fetchHistory = async (consultationId: number): Promise<ChatMessage[]> => {
        if (!connection) return [];
        const history = await connection.invoke('GetRecentMessages', consultationId, 50);
        return history;
    };

    return {
        messages,
        connectionStatus,
        connect,
        disconnect,
        sendMessage,
        fetchHistory
    };
});
// app/stores/chat.ts
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

export const useChatStore = defineStore('chat', () => {
    const messages = ref<ChatMessage[]>([]);
    const connectionStatus = ref<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');
    let connection: signalR.HubConnection | null = null;

    const connect = async (consultationId: number, token: string, currentUserId: number) => {
        if (connection) return;

        const config = useRuntimeConfig();
        const hubUrl = `${config.public.apiBase.replace('/api/', '')}/consultationHub`; // Adjust to your actual hub URL

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveMessage', (message: ChatMessage) => {
            messages.value.push(message);
            // Trigger a custom event or use nextTick in component to scroll
            window.dispatchEvent(new CustomEvent('scroll-chat-bottom'));
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
            await connection.stop();
            connection = null;
            connectionStatus.value = 'disconnected';
            messages.value = []; // Clear messages on disconnect
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
        // Calls your C# GetRecentMessages method
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
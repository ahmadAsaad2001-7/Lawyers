export interface ChatSummary {
    id: number; // consultation id
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

    // ✅ Add this optional property for frontend UI state
    unreadCount?: number;
}

export interface FreeInquiry {
    id: number;
    senderName: string;
    senderPhone: string;
    senderEmail: string;
    content: string;
    isRepliedTo: boolean;
    createdAt: string;
}
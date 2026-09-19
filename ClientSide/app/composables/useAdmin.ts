import { useAuthStore } from '~/stores/auth'
import type { PagedResult } from '~/types/Lawyer'

export interface AdminUserDto {
    id: number; email: string; phoneNumber: string | null; role: string
    profileImageUrl: string; createdAt: string; isDeleted: boolean; displayName: string
    lawyerProfileId: number | null; specialization: string | null; hourlyRate: number | null
    isVerified: boolean | null; averageRating: number | null; lawFirmName: string | null
    clientProfileId: number | null; consultationCount: number; postsCount: number; freeMessagesCount: number
}

export interface DashboardStats {
    totalUsers: number; totalRevenue: number; activeConsultations: number
    pendingVerifications: number; pendingVotes: number
}

export interface ChartPoint { date: string; label: string; revenue: number; consultations: number }

export interface RecentActivity { id: number; clientName: string; lawyerName: string; amount: number; date: string }

export interface AdminVote {
    id: number; actionType: string; targetUserId: number; targetUserName: string
    targetEmail: string; targetProfileImageUrl: string
    initiatorName: string; reason: string; approvalCount: number; disapprovalCount: number
    isResolved: boolean; createdAt: string; hasCurrentAdminVoted: boolean
}

export interface PendingLawyer {
    userId: number; fullName: string; email: string; barLicenseNumber: string
    specialization: string; lawFirmName: string; registeredAt: string
}

export interface ContactLogEntry {
    contactType: string; relatedEntityId: number; timestamp: string
    initiatorName: string; initiatorRole: string; targetName: string; targetRole: string; status: string
}
export interface PeriodAnalytics {
    periodRevenue: number
    quarterRevenue: number
    consultationsCount: number
    completedCount: number
    completionRate: number
    newUsersCount: number
    topLawyers: TopLawyer[]
}
export interface TopLawyer {
    userId: number
    fullName: string
    revenue: number
    consultations: number
    averageRating: number
}
export interface AppNotification { id: number; title: string; message: string; isRead: boolean; createdAt: string }

// ✅ NEW: Suspended user interface
export interface SuspendedUser {
    userId: number
    displayName: string
    email: string
    profileImageUrl: string | null
    reason: string
    startedAt: string
    endsAt: string
    remainingDays: number
}

export const useAdmin = () => {
    const config = useRuntimeConfig()
    const authStore = useAuthStore()
    const base = config.public.apiBase as string

    const h = () => ({ Authorization: `Bearer ${authStore.token ?? ''}` })    // ── Dashboard ──
    const getStats = () =>
        $fetch<DashboardStats>(`${base}/admin/dashboard/stats`, { headers: h() })
    const getChart = (params: { start: string; end: string; period: string }) =>
        $fetch<ChartPoint[]>(`${base}/admin/dashboard/chart`, { query: params, headers: h() })
    const getRecentActivities = (limit = 5) =>
        $fetch<RecentActivity[]>(`${base}/admin/dashboard/recent-activities`, { query: { limit }, headers: h() })
    const getPeriodAnalytics = (start: string, end: string) =>
        $fetch<PeriodAnalytics>(`${base}/admin/dashboard/analytics`, { query: { start, end }, headers: h() })
    // ── Users ──
    const getUsers = (params: Record<string, any>) =>
        $fetch<PagedResult<AdminUserDto>>(`${base}/admin/users`, { query: params, headers: h() })
    const getUserById = (userId: number) =>
        $fetch<AdminUserDto>(`${base}/admin/users/${userId}`, { headers: h() })
    const getContactLog = (userId: number, page = 1) =>
        $fetch<PagedResult<ContactLogEntry>>(`${base}/admin/users/${userId}/contact-log`, { query: { page }, headers: h() })
    const getUserChart = (userId: number, params: { start: string; end: string; period: string }) =>
        $fetch<ChartPoint[]>(`${base}/admin/users/${userId}/chart`, { query: params, headers: h() })
    const proposeBan = (userId: number, reason: string) =>
        $fetch(`${base}/admin/users/${userId}/propose-ban`, { method: 'POST', body: { targetUserId: userId, reason }, headers: h() })

    // ── Suspension System (NEW) ──
    const getSuspendedUsers = (page = 1, pageSize = 20) =>
        $fetch<PagedResult<SuspendedUser>>(`${base}/admin/suspended`, { query: { page, pageSize }, headers: h() })
    const getSuspendedUser = (userId: number) =>
        $fetch<SuspendedUser>(`${base}/admin/suspended/${userId}`, { headers: h() })
    const suspendUser = (userId: number, reason: string, days: number) =>
        $fetch(`${base}/admin/users/${userId}/suspend`, { method: 'POST', body: { userId, reason, days }, headers: h() })
    const extendSuspend = (userId: number, additionalDays: number) =>
        $fetch(`${base}/admin/users/${userId}/extend-suspend`, { method: 'POST', body: { userId, additionalDays }, headers: h() })
    const decreaseSuspend = (userId: number, daysToRemove: number) =>
        $fetch(`${base}/admin/users/${userId}/decrease-suspend`, { method: 'POST', body: { userId, daysToRemove }, headers: h() })
    const unsuspendUser = (userId: number) =>
        $fetch(`${base}/admin/users/${userId}/unsuspend`, { method: 'POST', body: { userId }, headers: h() })

    // ── Lawyers ──
    const getPendingLawyers = (page = 1) =>
        $fetch<PagedResult<PendingLawyer>>(`${base}/admin/lawyers/pending`, { query: { page }, headers: h() })
    const getLawyerChart = (lawyerProfileId: number, params: { start: string; end: string; period: string }) =>
        $fetch<ChartPoint[]>(`${base}/admin/lawyers/${lawyerProfileId}/chart`, { query: params, headers: h() })
    const proposeVerification = (userId: number, reason: string) =>
        $fetch(`${base}/admin/lawyers/${userId}/propose-verification`, { method: 'POST', body: { lawyerUserId: userId, reason }, headers: h() })
    const proposeUnverification = (userId: number, reason: string) =>
        $fetch(`${base}/admin/lawyers/${userId}/propose-unverification`, { method: 'POST', body: { lawyerUserId: userId, reason }, headers: h() })

    // ── Votes ──
    const getVotes = (includeResolved = false, page = 1, pageSize = 20) =>
        $fetch<PagedResult<AdminVote>>(`${base}/admin/votes`, { query: { includeResolved, page, pageSize }, headers: h() })
    const castVote = (voteId: number, isApproved: boolean) =>
        $fetch(`${base}/admin/votes/${voteId}/cast`, { method: 'POST', body: { voteId, isApproved }, headers: h() })

    // ── Moderation ──
    const deletePost = (postId: number, reason: string) =>
        $fetch(`${base}/admin/posts/${postId}`, { method: 'DELETE', body: { reason }, headers: h() })

    // ── Notifications (all roles) ──
    const getNotifications = (unreadOnly = false, limit = 50) =>
        $fetch<AppNotification[]>(`${base}/notifications`, { query: { unreadOnly, limit }, headers: h() })
    const getUnreadCount = () =>
        $fetch<number>(`${base}/notifications/unread-count`, { headers: h() })
    const markRead = (id: number) =>
        $fetch(`${base}/notifications/${id}/read`, { method: 'PATCH', headers: h() })

    return {
        // Dashboard
        getStats, getChart, getRecentActivities, getPeriodAnalytics, // ✅ Added here

        // Users
        getUsers, getUserById, getContactLog, getUserChart, proposeBan,

        // Suspension
        getSuspendedUsers, getSuspendedUser, suspendUser, extendSuspend, decreaseSuspend, unsuspendUser,

        // Lawyers
        getPendingLawyers, getLawyerChart, proposeVerification, proposeUnverification,

        // Votes
        getVotes, castVote,

        // Moderation
        deletePost,

        // Notifications
        getNotifications, getUnreadCount, markRead,
    }
}
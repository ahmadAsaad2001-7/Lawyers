import { defineStore } from 'pinia'

export interface AppNotification {
    id: number
    title: string
    message: string
    isRead: boolean
    createdAt: string
}

export const useNotificationStore = defineStore('notifications', () => {
    const items = ref<AppNotification[]>([])
    const unreadCount = ref(0)
    const isLoading = ref(false)

    const apiBase = () => {
        const config = useRuntimeConfig()
        const base = String(config.public.apiBase || '')
        return base.endsWith('/') ? base.slice(0, -1) : base
    }

    const authHeaders = () => {
        const token = useCookie<string | null>('auth_token')
        return { Authorization: `Bearer ${token.value ?? ''}` }
    }

    const ingest = (n: AppNotification) => {
        if (n.id && items.value.some((x) => x.id === n.id)) return
        items.value.unshift({
            id: n.id,
            title: n.title,
            message: n.message,
            isRead: !!n.isRead,
            createdAt: n.createdAt,
        })
        if (!n.isRead) unreadCount.value += 1
    }

    const load = async () => {
        const token = useCookie<string | null>('auth_token')
        if (!token.value) return
        isLoading.value = true
        try {
            const [list, count] = await Promise.all([
                $fetch<AppNotification[]>(`${apiBase()}/notifications`, {
                    query: { limit: 50 },
                    headers: authHeaders(),
                }),
                $fetch<number>(`${apiBase()}/notifications/unread-count`, {
                    headers: authHeaders(),
                }),
            ])
            items.value = list ?? []
            unreadCount.value = count ?? 0
        } catch (err) {
            console.warn('[notifications] failed to load', err)
        } finally {
            isLoading.value = false
        }
    }

    const markAllSeen = async () => {
        if (unreadCount.value === 0) return
        unreadCount.value = 0
        items.value = items.value.map((n) => ({ ...n, isRead: true }))
        try {
            await $fetch(`${apiBase()}/notifications/read-all`, {
                method: 'PATCH',
                headers: authHeaders(),
            })
        } catch (err) {
            console.warn('[notifications] failed to mark seen', err)
            await load()
        }
    }

    const reset = () => {
        items.value = []
        unreadCount.value = 0
        isLoading.value = false
    }

    return { items, unreadCount, isLoading, load, ingest, markAllSeen, reset }
})

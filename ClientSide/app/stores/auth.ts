import { defineStore } from 'pinia'

export interface AuthResponseDto {
    userId?: number | string
    userName?: string
    token?: string | null
    email: string
    role: string
    message?: string
}

export interface RegisterPayload {
    email: string
    password: string
    fullName: string
    role: number // Enum value (e.g. 1 = Client, 2 = Lawyer)
    lawFirmName?: string
    address?: string
    phoneNumber?: string
}

export interface UserState {
    userId?: number | string
    userName?: string
    email: string
    role: string
}

export const useAuthStore = defineStore('auth', () => {
    const config = useRuntimeConfig()

    const token = useCookie<string | null>('auth_token', {
        maxAge: 60 * 60 * 24 * 7,
        sameSite: 'lax'
    })

    const user = ref<UserState | null>(null)
    const isLoading = ref(false)

    const isAuthenticated = computed(() => !!token.value)

    // POST /api/auth/login -> LoginCommandHandler
    async function login(credentials: { email: string; password: string }) {
        isLoading.value = true
        try {
            const response = await $fetch<AuthResponseDto>('/auth/login', {
                method: 'POST',
                baseURL: config.public.apiBase,
                body: credentials
            })

            if (response?.token) {
                token.value = response.token
                user.value = {
                    userId: response.userId,
                    userName: response.userName,
                    email: response.email,
                    role: response.role
                }
            }
            return response
        } finally {
            isLoading.value = false
        }
    }

    // POST /api/auth/register -> RegisterCommandHandler (No token returned, requires verification)
    async function register(payload: RegisterPayload) {
        isLoading.value = true
        try {
            return await $fetch<AuthResponseDto>('/auth/register', {
                method: 'POST',
                baseURL: config.public.apiBase,
                body: payload
            })
        } finally {
            isLoading.value = false
        }
    }

    // POST /api/auth/forgot-password -> ForgotPasswordHandler
    async function forgotPassword(email: string) {
        return await $fetch<{ message: string }>('/auth/forgot-password', {
            method: 'POST',
            baseURL: config.public.apiBase,
            body: { email }
        })
    }

    // GET /api/auth/me
    async function fetchMe() {
        if (!token.value) return

        try {
            const response = await $fetch<UserState>('/auth/me', {
                baseURL: config.public.apiBase,
                headers: { Authorization: `Bearer ${token.value}` }
            })
            user.value = response
        } catch {
            logout()
        }
    }
// GET /api/auth/confirm-email
    async function confirmEmail(userId: number | string, token: string) {
        return await $fetch<{ message: string }>('/auth/confirm-email', {
            baseURL: config.public.apiBase,
            params: { userId, token }
        })
    }

// POST /api/auth/reset-password
    async function resetPassword(payload: { email: string; token: string; newPassword: string }) {
        return await $fetch<{ message: string }>('/auth/reset-password', {
            method: 'POST',
            baseURL: config.public.apiBase,
            body: payload
        })
    }
    async function initAuth() {
        if (token.value && !user.value) {
            await fetchMe()
        }
    }

    function logout() {
        token.value = null;
        user.value = null;
        navigateTo('/auth/login'); // Fixed route path
    }

    return {
        user,
        token,
        isAuthenticated,
        isLoading,
        login,
        register,
        forgotPassword,
        fetchMe,
        initAuth,
        logout
    }
})
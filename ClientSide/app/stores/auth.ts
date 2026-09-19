import { defineStore } from 'pinia'
import { useChatStore } from '~/stores/Chat'

export interface AddressPayload {
    street: string
    city: string
    state: string
    country: string
    postalCode: string
}

export interface AuthResponseDto {
    userId?: number | string
    userName?: string
    token?: string | null
    email: string
    role: string
    message?: string
    fullName?: string
    profileImageUrl?: string
    isPlatformVerified?: boolean
}

export interface RegisterPayload {
    email: string
    password: string
    fullName: string
    role: number // Enum value matching Roles: 0 = Client, 1 = Lawyer, 2 = PendingLawyer(server-assigned), 3 = Admin
    lawFirmName?: string
    address: AddressPayload
    phoneNumber: string
    barLicenseNumber?: string
    specialization?: string
    bio?: string
    hourlyRate?: number
}

export interface UserState {
    userId?: number | string
    userName?: string
    email: string
    role: string
    fullName?: string
    profileImageUrl?: string
    isPlatformVerified?: boolean
}

export const useAuthStore = defineStore('auth', () => {
    const config = useRuntimeConfig()

    const token = useCookie<string | null>('auth_token', {
        maxAge: 60 * 60 * 24 * 7,
        sameSite: 'lax'
    })

    const user = ref<UserState | null>(null)
    const isLoading = ref(false)

    // A token alone is not proof of a live session: it may be expired or revoked.
    // Waiting for /me prevents the profile UI from briefly showing a logged-in user.
    const isAuthenticated = computed(() => !!token.value && !!user.value)

    // POST /api/auth/login -> LoginCommandHandler
    async function login(credentials: { email: string; password: string }) {
        isLoading.value = true
        try {
            const response = await $fetch<AuthResponseDto>('/auth/login', {
                method: 'POST',
                baseURL: config.public.apiBase,
                body: credentials,
            })

            if (response?.token) {
                // Clear any previous identity BEFORE applying the new one.
                // Also tear down any lingering chat/call session from a
                // prior user on this tab before wiring up the new one.
                user.value = null

                if (import.meta.client) {
                    await useChatStore().disconnect()
                }

                token.value = response.token
                await fetchMe()

                if (import.meta.client && token.value && user.value) {
                    await useChatStore().initializeGlobal(
                        token.value,
                        user.value.role === 'Lawyer'
                    )
                }
            }
            return response
        } finally {
            isLoading.value = false
        }
    }

    // GET /api/auth/google -> Google OAuth challenge.
    // role: pass 'Lawyer' when the user is signing up as a lawyer via Google,
    // so the backend creates them as PendingLawyer instead of Client.
    function loginWithGoogle(role?: 'Client' | 'Lawyer') {
        if (import.meta.client) {
            const url = role
                ? `${config.public.apiBase}/auth/google?role=${role}`
                : `${config.public.apiBase}/auth/google`
            window.location.href = url
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

    // Tracks an in-flight /auth/me request so concurrent callers
    // share one request instead of firing two independent ones.
    let fetchMePromise: Promise<void> | null = null;

    // GET /api/auth/me
    async function fetchMe() {
        if (!token.value) return;

        if (fetchMePromise) {
            return fetchMePromise;
        }

        fetchMePromise = (async (): Promise<void> => {
            try {
                const response: UserState = await $fetch<UserState>('/auth/me', {
                    baseURL: config.public.apiBase,
                    headers: { Authorization: `Bearer ${token.value}` }
                });

                // Trust the backend's exact enum casing (e.g. "PendingLawyer")
                // instead of reformatting it — a naive charAt(0).toUpperCase()
                // transform would mangle multi-word roles like PendingLawyer
                // if the backend ever sends anything but perfect PascalCase.
                user.value = {
                    ...response,
                    role: response.role ?? '',
                }
            } catch (err: any) {
                // Only log out when the server explicitly says the token is invalid.
                // SSL/network/500 errors must NOT destroy the session.
                if (err?.statusCode === 401) {
                    await logout();
                } else {
                    console.warn('[auth] /auth/me failed (network/SSL?), keeping session:', err?.message);
                }
            } finally {
                fetchMePromise = null;
            }
        })();

        return fetchMePromise;
    }

    // POST /api/auth/refresh -> re-issues a JWT reflecting current DB role/verification.
    // Call this after a "you've been verified" notification, or when a
    // PendingLawyer dashboard mounts, instead of forcing a full re-login.
    async function refresh() {
        if (!token.value) return;

        try {
            const response = await $fetch<AuthResponseDto>('/auth/refresh', {
                method: 'POST',
                baseURL: config.public.apiBase,
                headers: { Authorization: `Bearer ${token.value}` }
            });

            if (response?.token) {
                token.value = response.token;
                await fetchMe();
            }
        } catch (err: any) {
            // Same policy as fetchMe: only a real 401 means the session is dead.
            if (err?.statusCode === 401) {
                await logout();
            } else {
                console.warn('[auth] refresh failed:', err?.message);
            }
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

    async function logout() {

        if (import.meta.client) {
            await useChatStore().disconnect()
        }

        token.value = null;
        user.value = null;

        if (import.meta.client) {
            navigateTo('/auth/login');
        }
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
        refresh,
        confirmEmail,
        resetPassword,
        initAuth,
        logout,
        loginWithGoogle
    }
})
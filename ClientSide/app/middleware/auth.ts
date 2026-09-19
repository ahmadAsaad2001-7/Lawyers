export default defineNuxtRouteMiddleware(async (to) => {
    const authStore = useAuthStore()

    // 1. Ensure auth state is initialized on the client-side
    // (We skip this on the server to avoid self-signed SSL certificate errors in dev)
    if (import.meta.client) {

        // If we have a token but no user data, fetch it
        if (!authStore.isAuthenticated && authStore.token) {
            await authStore.initAuth()
        }

        // 2. If still not authenticated, redirect to login
        if (!authStore.isAuthenticated) {
            return navigateTo({
                path: '/auth/login',
                query: { redirect: to.fullPath } // Remember where they wanted to go
            })
        }
    }
})
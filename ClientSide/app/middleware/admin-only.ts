// middleware/admin-only.ts
export default defineNuxtRouteMiddleware(async (to) => {

    if (import.meta.server) return

    const authStore = useAuthStore()
    await authStore.initAuth()

    // Not logged in → login page (remember where they wanted to go)
    if (!authStore.isAuthenticated) {
        return navigateTo({ path: '/auth/login', query: { redirect: to.fullPath } })
    }

    // Logged in but not an admin → home page
    if ((authStore.user?.role ?? '').trim().toLowerCase() !== 'admin') {
        return navigateTo('/')
    }
})
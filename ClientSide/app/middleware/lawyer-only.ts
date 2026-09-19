// middleware/lawyer-only.ts
export default defineNuxtRouteMiddleware(() => {
    const authStore = useAuthStore()

    // Ensure auth is initialized
    if (import.meta.client) {
        if (!authStore.isAuthenticated || authStore.user?.role !== 'Lawyer') {
            return navigateTo('/')
        }
    }
})
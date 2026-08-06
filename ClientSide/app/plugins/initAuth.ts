// plugins/initAuth.ts
export default defineNuxtPlugin(() => {
    const authStore = useAuthStore();
    authStore.initAuth();
});
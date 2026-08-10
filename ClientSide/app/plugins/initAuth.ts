// plugins/initAuth.ts
import { useAuthStore } from "~/stores/auth";

export default defineNuxtPlugin(async () => {
    // usePinia() is auto-imported by @pinia/nuxt
    const authStore = useAuthStore(usePinia());

    await authStore.initAuth();
});
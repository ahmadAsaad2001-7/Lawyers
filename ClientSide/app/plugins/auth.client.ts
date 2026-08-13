import { useAuthStore } from '~/stores/auth';
// app/plugins/auth.client.ts  ← the .client suffix is the fix
export default defineNuxtPlugin(() => {
    const auth = useAuthStore();
    auth.initAuth();
});
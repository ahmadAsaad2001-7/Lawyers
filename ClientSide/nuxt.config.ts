// https://nuxt.com/docs/api/configuration/nuxt-config
const environment = (globalThis as {
    process?: { env?: Record<string, string | undefined> }
}).process?.env ?? {}

export default defineNuxtConfig({
    compatibilityDate: '2024-04-03',
    devtools: { enabled: true },

    // Tells Nuxt that your code (pages, components, etc.) is in the 'app' directory
    srcDir: 'app/',
    // Keep static assets in the project-root public directory rather than app/public.
    dir: {
        public: '../public',
    },
    css: ['~/assets/css/main.css'],
    modules: ['@pinia/nuxt', '@nuxt/ui-pro', '@nuxt/fonts'],

    // ✅ Configure the Cairo font to be loaded automatically from Google
    fonts: {
        families: [
            { name: 'Cairo', provider: 'google' }
        ]
    },

    runtimeConfig: {
        public: {
            apiBase: environment.NUXT_PUBLIC_API_BASE || 'https://albayinahapi.runasp.net/api',

            turnUrl: environment.NUXT_PUBLIC_TURN_URL || '',
            turnUsername: environment.NUXT_PUBLIC_TURN_USERNAME || '',
            turnCredential: environment.NUXT_PUBLIC_TURN_CREDENTIAL || ''
        }
    }
})
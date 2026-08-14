// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
    compatibilityDate: '2024-04-03',
    devtools: { enabled: true },

    // Tells Nuxt that your code (pages, components, etc.) is in the 'app' directory
    srcDir: 'app/',
    // Keep static assets in the project-root public directory rather than app/public.
    dir: {
        public: '../public',
    },
    modules: ['@pinia/nuxt',
    '@nuxtjs/tailwindcss',
    ],
    runtimeConfig: {
        public: {
            apiBase: process.env.NUXT_PUBLIC_API_BASE || 'https://localhost:7129/api',
            // TURN is required for calls between users behind restrictive NATs,
            // mobile networks, or corporate firewalls. Leave these empty for
            // local STUN-only development.
            turnUrl: process.env.NUXT_PUBLIC_TURN_URL || '',
            turnUsername: process.env.NUXT_PUBLIC_TURN_USERNAME || '',
            turnCredential: process.env.NUXT_PUBLIC_TURN_CREDENTIAL || ''
        }
    }
})

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
    compatibilityDate: '2024-04-03',
    devtools: { enabled: true },

    // Tells Nuxt that your code (pages, components, etc.) is in the 'app' directory
    srcDir: 'app/',
    modules: ['@pinia/nuxt',
    '@nuxtjs/tailwindcss',
    ],
    runtimeConfig: {
        public: {
            apiBase: process.env.NUXT_PUBLIC_API_BASE || 'https://localhost:7129/api'
        }
    }
})
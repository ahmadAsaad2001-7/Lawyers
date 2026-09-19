<script setup lang="ts">
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

onMounted(async () => {
  const token = route.query.token as string

  if (token) {
    // 1. Save token to cookie/store
    authStore.token = token

    // 2. Fetch user profile to complete the login state
    await authStore.fetchMe()

    // 3. Redirect to profile or dashboard
    router.replace('/profile')
  } else {
    // If no token, kick back to login
    router.replace('/auth/login')
  }
})
</script>

<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-50">
    <div class="text-center">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-emerald-800 mx-auto mb-4"></div>
      <p class="text-emerald-900 font-medium">جاري تسجيل الدخول عبر جوجل...</p>
    </div>
  </div>
</template>
<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4" dir="rtl">
    <div class="w-full max-w-md rounded-2xl border border-emerald-100 bg-white p-6 text-center shadow-lg sm:p-8">

      <!-- Loading State -->
      <div v-if="status === 'loading'">
        <div class="mx-auto mb-4 h-12 w-12 animate-spin rounded-full border-4 border-emerald-200 border-t-emerald-800"></div>
        <h2 class="text-xl font-bold text-emerald-900" style="font-family: 'Amiri', serif;">جاري تفعيل الحساب...</h2>
        <p class="text-gray-500 mt-2">يرجى الانتظار لحظة.</p>
      </div>

      <!-- Success State -->
      <div v-else-if="status === 'success'">
        <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h2 class="text-2xl font-bold text-emerald-900 mb-2" style="font-family: 'Amiri', serif;">تم تفعيل الحساب بنجاح!</h2>
        <p class="text-gray-600 mb-6">تم تفعيل حسابك بنجاح. يمكنك الآن تسجيل الدخول.</p>
        <NuxtLink to="/auth/login" class="inline-block w-full rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white hover:from-emerald-900 hover:to-emerald-950 transition-colors">
          الذهاب لتسجيل الدخول
        </NuxtLink>
      </div>

      <!-- Error State -->
      <div v-else-if="status === 'error'">
        <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </div>
        <h2 class="text-2xl font-bold text-emerald-900 mb-2" style="font-family: 'Amiri', serif;">فشل التفعيل</h2>
        <p class="text-red-600 mb-6">{{ errorMessage }}</p>
        <NuxtLink to="/auth/register" class="inline-block w-full rounded-lg bg-gradient-to-l from-amber-400 to-amber-500 p-3 text-base font-semibold text-white hover:from-amber-500 hover:to-amber-600 transition-colors">
          محاولة التسجيل مجدداً
        </NuxtLink>
      </div>

    </div>
  </div>
</template>

<script setup>
const route = useRoute();
const status = ref('loading'); // 'loading' | 'success' | 'error'
const errorMessage = ref('');

onMounted(async () => {
  const { userId, token } = route.query;

  if (!userId || !token) {
    status.value = 'error';
    errorMessage.value = 'رابط التفعيل غير صالح. يرجى طلب رابط جديد.';
    return;
  }

  try {
    const config = useRuntimeConfig();
    await $fetch(`${config.public.apiBase}auth/confirm-email`, {
      method: 'GET',
      query: { userId, token }
    });
    status.value = 'success';
  } catch (error) {
    status.value = 'error';
    errorMessage.value = error.response?._data?.message || 'فشل التفعيل. قد يكون الرابط منتهي الصلاحية أو غير صالح.';
  }
});
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
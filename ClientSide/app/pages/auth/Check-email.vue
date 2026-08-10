<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4" dir="rtl">
    <div class="w-full max-w-md rounded-2xl bg-white p-8 shadow-lg border border-emerald-100 text-center">
      <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
      </div>

      <h2 class="text-2xl font-bold text-emerald-900 mb-2" style="font-family: 'Amiri', serif;">تحقق من بريدك الإلكتروني</h2>
      <p class="text-gray-600 mb-6">
        لقد أرسلنا رابط تفعيل إلى <strong class="text-emerald-700">{{ route.query.email }}</strong>.
        <br>يرجى النقر على الرابط في الرسالة لتفعيل حسابك.
      </p>

      <p class="text-sm text-gray-500 mb-6">
        لم تصلك الرسالة؟ تحقق من مجلد الرسائل غير المرغوب فيها (Spam) أو
        <a href="#" class="text-amber-600 hover:text-amber-700 hover:underline font-medium">أعد إرسال رابط التفعيل</a>.
      </p>

      <NuxtLink to="/auth/login" class="inline-block w-full rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-colors hover:from-emerald-900 hover:to-emerald-950">
        العودة لتسجيل الدخول
      </NuxtLink>
    </div>
  </div>
</template>

<script setup>
const route = useRoute();
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
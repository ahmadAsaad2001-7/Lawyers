<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8" dir="rtl">
    <form
        @submit.prevent="handleSubmit"
        class="w-full max-w-md space-y-6 rounded-2xl bg-white p-8 shadow-lg border border-emerald-100"
    >
      <!-- Header -->
      <div class="text-center">
        <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-100">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-amber-600" fill="none" viewBox="0 0 24 24"
               stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/>
          </svg>
        </div>
        <h2 class="text-3xl font-bold tracking-tight text-emerald-900" style="font-family: 'Amiri', serif;">
          نسيت كلمة المرور؟
        </h2>
        <p class="mt-2 text-sm text-gray-600">
          أدخل بريدك الإلكتروني المسجل، وسنرسل لك رابطاً لإعادة تعيين كلمة المرور.
        </p>
      </div>

      <!-- Email Input -->
      <div class="flex flex-col space-y-2">
        <label for="email" class="text-sm font-medium text-gray-700">البريد الإلكتروني</label>
        <input
            type="email"
            id="email"
            v-model="form.email"
            :disabled="isLoading || isSuccess"
            placeholder="name@example.com"
            class="rounded-lg border border-gray-300 p-3 text-base outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.email">{{ errors.email }}</span>
      </div>

      <!-- Submit Button -->
      <button
          type="submit"
          :disabled="isLoading || isSuccess"
          class="flex min-h-[48px] w-full items-center justify-center rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-all duration-200 hover:from-emerald-900 hover:to-emerald-950 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <span v-if="isLoading" class="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white"></span>
        <span v-else>إرسال رابط إعادة التعيين</span>
      </button>

      <!-- Success Message -->
      <div v-if="isSuccess" class="rounded-lg bg-green-50 border border-green-200 p-4 text-center">
        <p class="text-sm font-medium text-green-800">
          ✅ تم إرسال الرابط بنجاح!
        </p>
        <p class="text-xs text-green-700 mt-1">
          يرجى التحقق من بريدك الإلكتروني (ومجلد الرسائل غير المرغوب فيها).
        </p>
      </div>

      <!-- Error Message -->
      <div v-if="apiError"
           class="rounded-lg bg-red-50 border border-red-200 p-3 text-center text-sm font-medium text-red-700">
        {{ apiError }}
      </div>

      <!-- Back to Login -->
      <div class="text-center pt-2 border-t border-gray-100">
        <NuxtLink
            to="/auth/login"
            class="inline-flex items-center gap-2 text-sm font-medium text-emerald-700 hover:text-emerald-900 transition-colors"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
          </svg>
          العودة إلى تسجيل الدخول
        </NuxtLink>
      </div>
    </form>
  </div>
</template>

<script setup>
const form = ref({email: ''});
const errors = ref({});
const isLoading = ref(false);
const apiError = ref('');
const isSuccess = ref(false);

const validateForm = () => {
  errors.value = {};
  let isValid = true;
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  if (!form.value.email || !emailRegex.test(form.value.email)) {
    errors.value.email = 'يرجى إدخال بريد إلكتروني صحيح.';
    isValid = false;
  }

  return isValid;
};

const handleSubmit = async () => {
  if (!validateForm()) return;

  isLoading.value = true;
  apiError.value = '';
  const config = useRuntimeConfig();

  try {
    await $fetch(`${config.public.apiBase}auth/forgot-password`, {
      method: 'POST',
      body: {
        email: form.value.email
      }
    });

    // Show success message (we don't reveal if the email exists for security)
    isSuccess.value = true;

  } catch (error) {
    console.error('Forgot password failed:', error);
    // Even on error, we show a generic message to prevent email enumeration
    apiError.value = 'حدث خطأ أثناء معالجة الطلب. يرجى المحاولة مرة أخرى لاحقاً.';
  } finally {
    isLoading.value = false;
  }
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
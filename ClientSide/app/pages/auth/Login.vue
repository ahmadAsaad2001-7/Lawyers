<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8" dir="rtl">
    <form @submit.prevent="handleSubmit" class="w-full max-w-md space-y-6 rounded-2xl border border-emerald-100 bg-white p-6 shadow-lg sm:p-8">
      <h2 class="text-center text-2xl font-bold tracking-tight text-emerald-900 sm:text-3xl" style="font-family: 'Amiri', serif;">
        تسجيل الددخول
      </h2>

      <!-- Email -->
      <div class="flex flex-col space-y-2">
        <label for="email" class="text-sm font-medium text-gray-800">البريد الإلكتروني</label>
        <input
            type="email"
            id="email"
            v-model="form.email"
            :disabled="authStore.isLoading"
            placeholder="name@example.com"
            class="rounded-lg border border-gray-300 p-3 text-base text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.email">{{ errors.email }}</span>
      </div>

      <!-- Password -->
      <div class="flex flex-col space-y-2">
        <div class="flex items-center justify-between">
          <label for="password" class="text-sm font-medium text-gray-800">كلمة المرور</label>
          <NuxtLink
              to="/auth/forgot-password"
              class="text-xs text-amber-600 hover:text-amber-700 hover:underline font-medium"
          >
            نسيت كلمة المرور؟
          </NuxtLink>
        </div>
        <input
            type="password"
            id="password"
            v-model="form.password"
            :disabled="authStore.isLoading"
            placeholder="••••••••"
            class="rounded-lg border border-gray-300 p-3 text-base text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.password">{{ errors.password }}</span>
      </div>

      <!-- Submit Button -->
      <button
          type="submit"
          :disabled="authStore.isLoading"
          class="flex min-h-12 w-full items-center justify-center rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-all duration-200 hover:from-emerald-900 hover:to-emerald-950 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <span v-if="authStore.isLoading" class="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white"></span>
        <span v-else>دخول</span>
      </button>

      <!-- ✅ NEW: Divider -->
      <div class="relative my-6">
        <div class="absolute inset-0 flex items-center">
          <div class="w-full border-t border-gray-300"></div>
        </div>
        <div class="relative flex justify-center text-sm">
          <span class="bg-white px-3 text-gray-500">أو</span>
        </div>
      </div>

      <!-- ✅ NEW: Google Login Button -->
      <button
          type="button"
          @click="authStore.loginWithGoogle"
          :disabled="authStore.isLoading"
          class="flex w-full items-center justify-center gap-3 rounded-lg border border-gray-300 bg-white p-3 text-base font-semibold text-gray-700 transition-all duration-200 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <svg class="h-5 w-5" viewBox="0 0 24 24">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
        </svg>
        <span>تسجيل الدخول عبر جوجل</span>
      </button>

      <!-- Global API Error -->
      <div v-if="apiError" class="rounded-lg bg-red-50 border border-red-200 p-3 text-center text-sm font-medium text-red-700">
        {{ apiError }}
      </div>

      <p class="text-center text-sm text-gray-600">
        ليس لديك حساب؟
        <NuxtLink to="/auth/register" class="font-semibold text-emerald-700 hover:text-emerald-900 hover:underline">إنشاء حساب جديد</NuxtLink>
      </p>
    </form>
  </div>
</template>

<script setup>
import { useAuthStore } from '~/stores/auth';
definePageMeta({ middleware: 'guest' })
const authStore = useAuthStore();

const form = ref({ email: '', password: '' });
const errors = ref({});
const apiError = ref('');

const validateForm = () => {
  errors.value = {};
  let isValid = true;
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  if (!form.value.email || !emailRegex.test(form.value.email)) {
    errors.value.email = 'يرجى إدخال بريد إلكتروني صحيح.';
    isValid = false;
  }
  if (!form.value.password || form.value.password.length < 7) {
    errors.value.password = 'كلمة المرور يجب أن تكون 7 أحرف على الأقل.';
    isValid = false;
  }
  return isValid;
};

const handleSubmit = async () => {
  if (!validateForm()) return;

  apiError.value = '';

  try {
    const response = await authStore.login({
      email: form.value.email,
      password: form.value.password,
    });

    if (response?.token) {
      navigateTo('/');
    }
  } catch (error) {
    console.error('Login failed:', error);
    apiError.value = error.data?.message || 'فشل تسجيل الدخول. يرجى التحقق من بياناتك.';
  }
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
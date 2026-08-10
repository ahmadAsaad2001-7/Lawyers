<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8" dir="rtl">
    <form @submit.prevent="handleSubmit" class="w-full max-w-md space-y-6 rounded-2xl bg-white p-8 shadow-lg border border-emerald-100">
      <h2 class="text-center text-3xl font-bold tracking-tight text-emerald-900" style="font-family: 'Amiri', serif;">
        تسجيل الدخول
      </h2>

      <!-- Email -->
      <div class="flex flex-col space-y-2">
        <label for="email" class="text-sm font-medium text-gray-700">البريد الإلكتروني</label>
        <input
            type="email"
            id="email"
            v-model="form.email"
            :disabled="authStore.isLoading"
            placeholder="name@example.com"
            class="rounded-lg border border-gray-300 p-3 text-base outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.email">{{ errors.email }}</span>
      </div>

      <!-- Password -->
      <div class="flex flex-col space-y-2">
        <div class="flex items-center justify-between">
          <label for="password" class="text-sm font-medium text-gray-700">كلمة المرور</label>
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
            class="rounded-lg border border-gray-300 p-3 text-base outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.password">{{ errors.password }}</span>
      </div>

      <!-- Submit Button -->
      <button
          type="submit"
          :disabled="authStore.isLoading"
          class="flex min-h-[48px] w-full items-center justify-center rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-all duration-200 hover:from-emerald-900 hover:to-emerald-950 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <span v-if="authStore.isLoading" class="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white"></span>
        <span v-else>دخول</span>
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
    // We use the store's login action instead of raw $fetch
    // authStore.isLoading will handle the loading state for the UI
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
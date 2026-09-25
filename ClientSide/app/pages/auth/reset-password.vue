<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8" dir="rtl">
    <form
        @submit.prevent="handleSubmit"
        class="w-full max-w-md space-y-6 rounded-2xl border border-emerald-100 bg-white p-6 shadow-lg sm:p-8"
    >
      <div class="text-center">
        <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
          </svg>
        </div>
        <h2 class="text-2xl font-bold tracking-tight text-emerald-900 sm:text-3xl" style="font-family: 'Amiri', serif;">
          إعادة تعيين كلمة المرور
        </h2>
        <p class="mt-2 text-sm text-gray-600">
          يرجى إدخال كلمة المرور الجديدة لحسابك
        </p>
      </div>

      <!-- New Password -->
      <div class="flex flex-col space-y-2">
        <label for="newPassword" class="text-sm font-medium text-gray-700">كلمة المرور الجديدة</label>
        <input
            type="password"
            id="newPassword"
            v-model="form.newPassword"
            :disabled="isLoading"
            placeholder="••••••••"
            class="rounded-lg border border-gray-300 p-3 text-base outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.newPassword">{{ errors.newPassword }}</span>
      </div>

      <!-- Confirm Password -->
      <div class="flex flex-col space-y-2">
        <label for="confirmPassword" class="text-sm font-medium text-gray-700">تأكيد كلمة المرور</label>
        <input
            type="password"
            id="confirmPassword"
            v-model="form.confirmPassword"
            :disabled="isLoading"
            placeholder="••••••••"
            class="rounded-lg border border-gray-300 p-3 text-base outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.confirmPassword">{{ errors.confirmPassword }}</span>
      </div>

      <!-- Submit Button -->
      <button
          type="submit"
          :disabled="isLoading"
          class="flex min-h-[48px] w-full items-center justify-center rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-all duration-200 hover:from-emerald-900 hover:to-emerald-950 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <span v-if="isLoading" class="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white"></span>
        <span v-else>تغيير كلمة المرور</span>
      </button>

      <!-- Global API Error -->
      <div v-if="apiError" class="rounded-lg bg-red-50 border border-red-200 p-3 text-center text-sm font-medium text-red-700">
        {{ apiError }}
      </div>

      <!-- Success Message -->
      <div v-if="isSuccess" class="rounded-lg bg-green-50 border border-green-200 p-3 text-center text-sm font-medium text-green-700">
        تم تغيير كلمة المرور بنجاح! جاري تحويلك لتسجيل الدخول...
      </div>
    </form>
  </div>
</template>

<script setup>
const route = useRoute();
const router = useRouter();

const form = ref({
  newPassword: '',
  confirmPassword: ''
});
const errors = ref({});
const isLoading = ref(false);
const apiError = ref('');
const isSuccess = ref(false);

// Extract email and token from URL query parameters
const email = computed(() => route.query.email || '');
const token = computed(() => route.query.token || '');

const validateForm = () => {
  errors.value = {};
  let isValid = true;

  if (!form.value.newPassword || form.value.newPassword.length < 7) {
    errors.value.newPassword = 'كلمة المرور يجب أن تكون 7 أحرف على الأقل.';
    isValid = false;
  }

  if (form.value.newPassword !== form.value.confirmPassword) {
    errors.value.confirmPassword = 'كلمتا المرور غير متطابقتين.';
    isValid = false;
  }

  if (!email.value || !token.value) {
    apiError.value = 'رابط إعادة التعيين غير صالح أو منتهي الصلاحية.';
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
    const response = await $fetch(`${config.public.apiBase}auth/reset-password`, {
      method: 'POST',
      body: {
        email: email.value,
        token: token.value,
        newPassword: form.value.newPassword
      }
    });

    isSuccess.value = true;

    // Redirect to login after 2 seconds
    setTimeout(() => {
      navigateTo('/auth/login');
    }, 2000);

  } catch (error) {
    console.error('Password reset failed:', error);
    apiError.value = error.response?._data?.message || 'فشل تغيير كلمة المرور. يرجى التأكد من صحة الرابط أو طلب رابط جديد.';
  } finally {
    isLoading.value = false;
  }
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
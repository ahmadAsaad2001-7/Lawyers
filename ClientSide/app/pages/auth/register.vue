<template>
  <div class="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12 sm:px-6 lg:px-8" dir="rtl">
    <form @submit.prevent="handleSubmit" class="w-full max-w-md space-y-5 rounded-2xl border border-emerald-100 bg-white p-6 shadow-lg sm:p-8">
      <h2 class="text-center text-2xl font-bold tracking-tight text-emerald-900 sm:text-3xl" style="font-family: 'Amiri', serif;">
        إنشاء حساب جديد
      </h2>

      <!-- Full Name -->
      <div class="flex flex-col space-y-1.5">
        <label for="fullName" class="text-sm font-medium text-gray-800">الاسم الكامل</label>
        <input
            type="text"
            id="fullName"
            v-model="form.fullName"
            :disabled="authStore.isLoading"
            placeholder="أحمد أسعد"
            class="rounded-lg border border-gray-300 p-3 text-base text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.fullName">{{ errors.fullName }}</span>
      </div>

      <!-- Email -->
      <div class="flex flex-col space-y-1.5">
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

      <!-- Phone Number -->
      <div class="flex flex-col space-y-1.5">
        <label for="phoneNumber" class="text-sm font-medium text-gray-800">رقم الهاتف</label>
        <input
            type="tel"
            id="phoneNumber"
            v-model="form.phoneNumber"
            :disabled="authStore.isLoading"
            placeholder="01012345678"
            class="rounded-lg border border-gray-300 p-3 text-base text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.phoneNumber">{{ errors.phoneNumber }}</span>
      </div>

      <!-- Password -->
      <div class="flex flex-col space-y-1.5">
        <label for="password" class="text-sm font-medium text-gray-800">كلمة المرور</label>
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

      <!-- Role Selection -->
      <div class="flex flex-col space-y-1.5">
        <label class="text-sm font-medium text-gray-800">نوع الحساب</label>
        <div class="grid grid-cols-2 gap-3">
          <button
              type="button"
              @click="form.role = 'Client'"
              class="rounded-lg border py-2.5 text-sm font-medium transition-all"
              :class="form.role === 'Client' ? 'border-emerald-800 bg-emerald-800 text-white' : 'border-gray-300 bg-white text-gray-700 hover:border-emerald-400'"
          >
            عميل
          </button>
          <button
              type="button"
              @click="form.role = 'Lawyer'"
              class="rounded-lg border py-2.5 text-sm font-medium transition-all"
              :class="form.role === 'Lawyer' ? 'border-emerald-800 bg-emerald-800 text-white' : 'border-gray-300 bg-white text-gray-700 hover:border-emerald-400'"
          >
            محامي
          </button>
        </div>
      </div>

      <!-- Address Section -->
      <div class="border-t border-gray-200 pt-4 mt-4 space-y-3">
        <h3 class="text-sm font-bold text-emerald-900" style="font-family: 'Amiri', serif;">العنوان</h3>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <!-- Country -->
          <div class="flex flex-col space-y-1">
            <label for="country" class="text-xs font-medium text-gray-700">الدولة</label>
            <select
                id="country"
                v-model="form.address.country"
                :disabled="authStore.isLoading"
                class="rounded-lg border border-gray-300 p-2.5 text-sm text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-100"
            >
              <option v-for="country in EGYPT_COUNTRIES" :key="country" :value="country">{{ country }}</option>
            </select>
          </div>

          <!-- State -->
          <div class="flex flex-col space-y-1">
            <label for="state" class="text-xs font-medium text-gray-700">المحافظة</label>
            <select
                id="state"
                v-model="form.address.state"
                :disabled="authStore.isLoading"
                class="rounded-lg border border-gray-300 p-2.5 text-sm text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-100"
            >
              <option value="" disabled>اختر المحافظة</option>
              <option v-for="governorate in EGYPT_GOVERNORATES" :key="governorate" :value="governorate">
                {{ governorate }}
              </option>
            </select>
            <span class="text-xs text-red-500" v-if="errors.state">{{ errors.state }}</span>
          </div>

          <!-- City -->
          <div class="flex flex-col space-y-1">
            <label for="city" class="text-xs font-medium text-gray-700">المدينة</label>
            <input
                type="text"
                id="city"
                v-model="form.address.city"
                :disabled="authStore.isLoading"
                placeholder="مدينة نصر"
                class="rounded-lg border border-gray-300 p-2.5 text-sm text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-100"
            />
          </div>

          <!-- Postal Code -->
          <div class="flex flex-col space-y-1">
            <label for="postalCode" class="text-xs font-medium text-gray-700">الرمز البريدي</label>
            <input
                type="text"
                id="postalCode"
                v-model="form.address.postalCode"
                :disabled="authStore.isLoading"
                placeholder="11511"
                class="rounded-lg border border-gray-300 p-2.5 text-sm text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-100"
            />
          </div>

          <!-- Street -->
          <div class="flex flex-col space-y-1 sm:col-span-2">
            <label for="street" class="text-xs font-medium text-gray-700">الشارع / العنوان التفصيلي</label>
            <input
                type="text"
                id="street"
                v-model="form.address.street"
                :disabled="authStore.isLoading"
                placeholder="123 شارع التحرير"
                class="rounded-lg border border-gray-300 p-2.5 text-sm text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-100"
            />
          </div>
        </div>
      </div>

      <!-- Conditional Law Firm Name for Lawyers -->
      <div v-if="form.role === 'Lawyer'" class="flex flex-col space-y-1.5">
        <label for="lawFirmName" class="text-sm font-medium text-gray-800">اسم مكتب المحاماة / الشركة</label>
        <input
            type="text"
            id="lawFirmName"
            v-model="form.lawFirmName"
            :disabled="authStore.isLoading"
            placeholder="مكتب أسعد للمحاماة"
            class="rounded-lg border border-gray-300 p-3 text-base text-gray-800 outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:cursor-not-allowed disabled:bg-gray-100"
        />
        <span class="text-xs text-red-500" v-if="errors.lawFirmName">{{ errors.lawFirmName }}</span>
      </div>

      <!-- Submit Button -->
      <button
          type="submit"
          :disabled="authStore.isLoading"
          class="flex min-h-[48px] w-full items-center justify-center rounded-lg bg-gradient-to-l from-emerald-800 to-emerald-900 p-3 text-base font-semibold text-white transition-all duration-200 hover:from-emerald-900 hover:to-emerald-950 disabled:cursor-not-allowed disabled:opacity-70"
      >
        <span v-if="authStore.isLoading" class="h-5 w-5 animate-spin rounded-full border-2 border-white/30 border-t-white"></span>
        <span v-else>تسجيل الحساب</span>
      </button>

      <!-- Global API Error & Success -->
      <div v-if="apiError" class="rounded-lg bg-red-50 border border-red-200 p-3 text-center text-sm font-medium text-red-700">
        {{ apiError }}
      </div>
      <div v-if="successMessage" class="rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-center text-sm font-medium text-emerald-800">
        {{ successMessage }}
      </div>

      <p class="text-center text-sm text-gray-600">
        لديك حساب بالفعل؟
        <NuxtLink to="/auth/login" class="font-semibold text-emerald-700 hover:text-emerald-900 hover:underline">تسجيل الدخول</NuxtLink>
      </p>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { EGYPT_COUNTRIES, EGYPT_COUNTRY, EGYPT_GOVERNORATES } from '~/data/egyptLocations';

const authStore = useAuthStore();

const form = ref({
  fullName: '',
  email: '',
  phoneNumber: '',
  password: '',
  role: 'Client',
  lawFirmName: '',
  address: {
    street: '',
    city: '',
    state: '',
    country: EGYPT_COUNTRY,
    postalCode: ''
  }
});
const errors = ref<Record<string, string>>({});
const apiError = ref('');
const successMessage = ref('');

const validateForm = () => {
  errors.value = {};
  let isValid = true;
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  const phoneRegex = /^01[0125][0-9]{8}$/;

  if (!form.value.fullName.trim()) {
    errors.value.fullName = 'يرجى إدخال الاسم الكامل.';
    isValid = false;
  }
  if (!form.value.email || !emailRegex.test(form.value.email)) {
    errors.value.email = 'يرجى إدخال بريد إلكتروني صحيح.';
    isValid = false;
  }
  if (!form.value.phoneNumber || !phoneRegex.test(form.value.phoneNumber)) {
    errors.value.phoneNumber = 'يرجى إدخال رقم هاتف مصري صحيح (مثال: 01012345678).';
    isValid = false;
  }
  if (!form.value.password || form.value.password.length < 8) {
    errors.value.password = 'كلمة المرور يجب أن تكون 8 أحرف على الأقل، وتشمل حرفاً كبيراً ورقم ورمزاً.';
    isValid = false;
  } else if (!/[A-Z]/.test(form.value.password) || !/[a-z]/.test(form.value.password) || !/[0-9]/.test(form.value.password) || !/[^A-Za-z0-9]/.test(form.value.password)) {
    errors.value.password = 'كلمة المرور يجب أن تحتوي على حرف كبير وصغير ورقم ورمز (مثال: Test@123).';
    isValid = false;
  }
  if (form.value.role === 'Lawyer' && !form.value.lawFirmName.trim()) {
    errors.value.lawFirmName = 'يرجى إدخال اسم مكتب المحاماة.';
    isValid = false;
  }
  if (!form.value.address.state) {
    errors.value.state = 'يرجى اختيار المحافظة.';
    isValid = false;
  }

  return isValid;
};

const handleSubmit = async () => {
  if (!validateForm()) return;

  apiError.value = '';
  successMessage.value = '';

  try {
    const roleEnumMap: Record<string, number> = {
      Client: 0,
      Lawyer: 1,
    };

    const response = await authStore.register({
      fullName: form.value.fullName,
      email: form.value.email,
      phoneNumber: form.value.phoneNumber,
      password: form.value.password,
      role: roleEnumMap[form.value.role] ?? 0,
      lawFirmName: form.value.role === 'Lawyer' ? form.value.lawFirmName : undefined,
      address: {
        street: form.value.address.street,
        city: form.value.address.city,
        state: form.value.address.state,
        country: EGYPT_COUNTRY,
        postalCode: form.value.address.postalCode,
      },
    });

    if (response) {
      successMessage.value = 'تم إنشاء الحساب بنجاح. يرجى التحقق من بريدك الإلكتروني لتأكيد الحساب.';
      setTimeout(() => {
        navigateTo('/auth/login');
      }, 2000);
    }
  } catch (error: any) {
    console.error('Registration failed:', error);
    const validation = error.data?.errors
      ? Object.values(error.data.errors).flat().join(' ')
      : '';
    apiError.value = error.data?.message || validation || 'فشل إنشاء الحساب. يرجى التحقق من البيانات والمحاولة مرة أخرى.';
  }
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>
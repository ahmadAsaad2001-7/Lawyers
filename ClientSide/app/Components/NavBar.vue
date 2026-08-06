<template>
  <nav
      dir="rtl"
      class="relative w-full rounded-b-2xl bg-gradient-to-l from-emerald-900 via-emerald-800 to-teal-900 shadow-lg shadow-black/10"
  >
    <!-- Decorative Andalusian Pattern Border -->
    <div class="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-amber-400 via-yellow-300 to-amber-400 rounded-t-2xl"></div>

    <div class="mx-auto max-w-7xl px-6 py-4">
      <div class="flex items-center justify-between">

        <!-- Right Side: Logo & Brand -->
        <div class="flex items-center gap-3">
          <NuxtLink to="/" class="flex items-center gap-3">
            <div class="flex h-10 w-10 items-center justify-center rounded-full bg-amber-400/20 ring-2 ring-amber-400/50">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-amber-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
              </svg>
            </div>
            <div class="flex flex-col">
              <span class="font-bold text-xl text-amber-300" style="font-family: 'Amiri', serif;">البينة</span>
              <span class="text-xs text-emerald-200/70" style="font-family: 'Amiri', serif;">منصة المحاماة</span>
            </div>
          </NuxtLink>
        </div>

        <!-- Center: Navigation Links -->
        <div class="hidden md:flex items-center gap-8">
          <NuxtLink to="/" class="group relative text-emerald-100 hover:text-amber-300 transition-colors duration-300" style="font-family: 'Amiri', serif;">
            الصفحة الرئيسية
            <span class="absolute -bottom-1 right-0 w-0 h-0.5 bg-amber-400 transition-all duration-300 group-hover:w-full"></span>
          </NuxtLink>
          <NuxtLink to="/search" class="group relative text-emerald-100 hover:text-amber-300 transition-colors duration-300" style="font-family: 'Amiri', serif;">
            بحث
            <span class="absolute -bottom-1 right-0 w-0 h-0.5 bg-amber-400 transition-all duration-300 group-hover:w-full"></span>
          </NuxtLink>
          <NuxtLink to="/lawyers" class="group relative text-emerald-100 hover:text-amber-300 transition-colors duration-300" style="font-family: 'Amiri', serif;">
            المحامون
            <span class="absolute -bottom-1 right-0 w-0 h-0.5 bg-amber-400 transition-all duration-300 group-hover:w-full"></span>
          </NuxtLink>
        </div>

        <!-- Left Side: Auth Buttons -->
        <div class="flex items-center gap-3">

          <!-- LOGGED OUT STATE -->
          <template v-if="!isLoggedIn">
            <NuxtLink to="/auth/login" class="rounded-lg px-5 py-2 text-sm font-medium text-emerald-100 hover:text-amber-300 transition-colors duration-300" style="font-family: 'Amiri', serif;">
              تسجيل الدخول
            </NuxtLink>
            <NuxtLink to="/auth/register" class="rounded-lg bg-amber-400 px-5 py-2 text-sm font-medium text-emerald-900 shadow-md hover:bg-amber-300 hover:shadow-lg transition-all duration-300" style="font-family: 'Amiri', serif;">
              إنشاء حساب
            </NuxtLink>
          </template>

          <!-- LOGGED IN STATE -->
          <template v-else>
            <NuxtLink
                to="/consultations"
                class="relative flex h-10 w-10 items-center justify-center rounded-full bg-emerald-700/30 text-amber-300 hover:bg-emerald-700/50 hover:text-amber-200 transition-colors duration-300"
                title="الاستشارات والمحادثات"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
              <span class="absolute top-0 right-0 block h-2.5 w-2.5 rounded-full bg-red-500 ring-2 ring-emerald-900"></span>
            </NuxtLink>

            <!-- Profile Link (Shows User Name) -->
            <NuxtLink to="/profile" class="hidden sm:flex items-center gap-2 rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors duration-300">
              <div class="flex h-8 w-8 items-center justify-center rounded-full bg-amber-400/20 ring-2 ring-amber-400/50">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 text-amber-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <span class="text-sm" style="font-family: 'Amiri', serif;">
                {{ authStore.user?.userName || 'الملف الشخصي' }}
              </span>
            </NuxtLink>

            <!-- Logout Button -->
            <button @click="handleLogout" class="rounded-lg px-4 py-2 text-sm font-medium text-red-300 hover:bg-red-500/20 hover:text-red-200 transition-all duration-300" style="font-family: 'Amiri', serif;">
              خروج
            </button>
          </template>

          <!-- Mobile Menu Button -->
          <button @click="isMobileMenuOpen = !isMobileMenuOpen" class="md:hidden rounded-lg p-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path v-if="!isMobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
              <path v-else stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Mobile Menu -->
      <div v-if="isMobileMenuOpen" class="mt-4 border-t border-emerald-700/50 pt-4 md:hidden">
        <div class="flex flex-col gap-3">
          <NuxtLink to="/" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors" style="font-family: 'Amiri', serif;">الصفحة الرئيسية</NuxtLink>
          <NuxtLink to="/search" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors" style="font-family: 'Amiri', serif;">بحث</NuxtLink>
          <NuxtLink to="/lawyers" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors" style="font-family: 'Amiri', serif;">المحامون</NuxtLink>

          <!-- Mobile Logged Out State -->
          <template v-if="!isLoggedIn">
            <NuxtLink to="/auth/login" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors" style="font-family: 'Amiri', serif;">تسجيل الدخول</NuxtLink>
            <NuxtLink to="/auth/register" class="rounded-lg bg-amber-400 px-4 py-2 text-center font-medium text-emerald-900 hover:bg-amber-300 transition-colors" style="font-family: 'Amiri', serif;">إنشاء حساب</NuxtLink>
          </template>

          <!-- Mobile Logged In State -->
          <template v-else>
            <NuxtLink to="/consultations" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors flex items-center gap-2" style="font-family: 'Amiri', serif;">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
              الاستشارات
            </NuxtLink>

            <!-- Mobile Profile Link (Shows User Name) -->
            <NuxtLink to="/profile" class="rounded-lg px-4 py-2 text-emerald-100 hover:bg-emerald-700/50 transition-colors flex items-center gap-2" style="font-family: 'Amiri', serif;">
              <span>{{ authStore.user?.userName || 'الملف الشخصي' }}</span>
            </NuxtLink>

            <button @click="handleLogout" class="rounded-lg px-4 py-2 text-right text-red-300 hover:bg-red-500/20 transition-colors" style="font-family: 'Amiri', serif;">تسجيل الخروج</button>
          </template>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from "~/stores/Auth";

const authStore = useAuthStore();
const isMobileMenuOpen = ref(false);

onMounted(() => {
  authStore.initAuth();
});

const isLoggedIn = computed(() => authStore.isAuthenticated);

const handleLogout = () => {
  authStore.logout();
  isMobileMenuOpen.value = false;
};
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');

* {
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
}
</style>
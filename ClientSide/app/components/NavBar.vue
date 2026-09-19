<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';

const authStore = useAuthStore();
const chatStore = useChatStore();

const isMobileMenuOpen = ref(false);
const isProfileMenuOpen = ref(false);

const isLoggedIn = computed(() => authStore.isAuthenticated);
const isLawyer = computed(() => authStore.user?.role === 'Lawyer');

const chatUnread = computed(() =>
    Object.values(chatStore.unread).reduce((sum, n) => sum + n, 0)
);

const toggleMobileMenu = () => {
  isMobileMenuOpen.value = !isMobileMenuOpen.value;
  isProfileMenuOpen.value = false;
};

const closeMenus = () => {
  isMobileMenuOpen.value = false;
  isProfileMenuOpen.value = false;
};

const handleLogout = () => {
  authStore.logout();
  closeMenus();
};

// Close dropdowns when clicking outside
const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement;
  if (!target.closest('[data-menu-trigger]') && !target.closest('[data-menu-content]')) {
    isProfileMenuOpen.value = false;
  }
};

onMounted(() => document.addEventListener('click', handleClickOutside));
onBeforeUnmount(() => document.removeEventListener('click', handleClickOutside));

// Close mobile menu on route change
const route = useRoute();
watch(() => route.path, closeMenus);
</script>

<template>
  <nav
      dir="rtl"
      class="relative w-full rounded-b-2xl bg-gradient-to-l from-emerald-900 via-emerald-800 to-teal-900 shadow-lg shadow-black/10"
      style="font-family: 'Amiri', serif;"
  >
    <!-- Gold top accent bar -->
    <div class="absolute inset-x-0 top-0 h-1 rounded-t-2xl bg-gradient-to-r from-amber-400 via-yellow-300 to-amber-400"></div>

    <div class="mx-auto max-w-7xl px-6 py-4">
      <div class="flex items-center justify-between">

        <!-- Logo -->
        <NuxtLink to="/" class="flex items-center gap-3" @click="closeMenus">
          <div class="flex h-10 w-10 items-center justify-center rounded-full bg-amber-400/20 ring-2 ring-amber-400/50">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 text-amber-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
            </svg>
          </div>
          <div class="flex flex-col">
            <span class="text-xl font-bold text-amber-300">البينة</span>
            <span class="text-xs text-emerald-200/70">منصة المحاماة</span>
          </div>
        </NuxtLink>

        <!-- Desktop links -->
        <div class="hidden items-center gap-8 md:flex">
          <NuxtLink to="/" class="text-emerald-100 transition-colors hover:text-amber-300">الصفحة الرئيسية</NuxtLink>
         
        </div>

        <!-- Desktop auth / profile -->
        <div class="hidden items-center gap-3 md:flex">
          <template v-if="!isLoggedIn">
            <NuxtLink to="/auth/login" class="text-sm text-emerald-100 transition-colors hover:text-amber-300">تسجيل الدخول</NuxtLink>
            <NuxtLink to="/auth/register" class="rounded-lg bg-amber-400 px-5 py-2 text-sm font-semibold text-emerald-900 transition-colors hover:bg-amber-300">إنشاء حساب</NuxtLink>
          </template>

          <template v-else>
            <NotificationBell />

            <!-- Chat icon with unread badge -->
            <NuxtLink
                to="/chat"
                class="relative flex h-10 w-10 items-center justify-center rounded-full text-emerald-100 transition-colors hover:bg-white/10 hover:text-amber-300"
                aria-label="المحادثات"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
              <span
                  v-if="chatUnread > 0"
                  class="absolute -start-1 -top-1 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-amber-500 px-1 text-[10px] font-bold text-white"
              >
                {{ chatUnread }}
              </span>
            </NuxtLink>

            <!-- Profile dropdown trigger -->
            <button
                data-menu-trigger
                class="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-emerald-100 transition-colors hover:bg-white/10 hover:text-amber-300"
                @click.stop="isProfileMenuOpen = !isProfileMenuOpen"
            >
              <div class="flex h-8 w-8 items-center justify-center rounded-full bg-amber-400/30 text-sm font-bold text-amber-200">
                {{ (authStore.user?.userName || authStore.user?.email || 'م').charAt(0) }}
              </div>
              <span>{{ authStore.user?.userName || authStore.user?.email }}</span>
              <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 transition-transform" :class="{ 'rotate-180': isProfileMenuOpen }" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            <!-- Profile dropdown -->
            <div
                v-if="isProfileMenuOpen"
                data-menu-content
                class="absolute end-6 top-full z-30 mt-2 w-56 overflow-hidden rounded-xl border border-gray-100 bg-white text-sm shadow-xl"
            >
              <NuxtLink to="/profile" class="flex items-center gap-2 px-4 py-2.5 text-gray-700 hover:bg-emerald-50" @click="closeMenus">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                الملف الشخصي
              </NuxtLink>
              <NuxtLink
                  v-if="authStore.user?.role.toLowerCase() == 'admin'"
                  to="/admin"
                  class="text-gray-700 hover:text-emerald-600 font-medium transition-colors"
              >
الادمن بانل
              </NuxtLink>
              
              <NuxtLink to="/chat" class="flex items-center gap-2 px-4 py-2.5 text-gray-700 hover:bg-emerald-50" @click="closeMenus">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" /></svg>
                المحادثات
                <span v-if="chatUnread" class="me-auto rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white">{{ chatUnread }}</span>
              </NuxtLink>
<!--              <NuxtLink v-if="isLawyer" to="/consultations/free-messages" class="flex items-center gap-2 px-4 py-2.5 text-gray-700 hover:bg-emerald-50" @click="closeMenus">-->
<!--                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>-->
<!--                الاستشارات المجانية-->
<!--              </NuxtLink>-->
              <div class="h-px bg-gray-100"></div>
              <button
                  class="flex w-full items-center gap-2 px-4 py-2.5 text-start text-red-600 hover:bg-red-50"
                  @click="handleLogout"
              >
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" /></svg>
                تسجيل الخروج
              </button>
            </div>
          </template>
        </div>

        <!-- Mobile: hamburger + chat icon -->
        <div class="flex items-center gap-2 md:hidden">
          <NotificationBell v-if="isLoggedIn" compact />
          <NuxtLink
              v-if="isLoggedIn"
              to="/chat"
              class="relative flex h-10 w-10 items-center justify-center rounded-full text-emerald-100 hover:bg-white/10"
              aria-label="المحادثات"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
            <span
                v-if="chatUnread > 0"
                class="absolute -start-1 -top-1 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-amber-500 px-1 text-[10px] font-bold text-white"
            >
              {{ chatUnread }}
            </span>
          </NuxtLink>

          <button
              class="flex h-10 w-10 items-center justify-center rounded-full text-emerald-100 hover:bg-white/10"
              aria-label="القائمة"
              @click="toggleMobileMenu"
          >
            <svg v-if="!isMobileMenuOpen" xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Mobile menu -->
      <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="opacity-0 -translate-y-2"
          enter-to-class="opacity-100 translate-y-0"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="opacity-100 translate-y-0"
          leave-to-class="opacity-0 -translate-y-2"
      >
        <div v-if="isMobileMenuOpen" class="mt-4 flex flex-col gap-1 border-t border-white/10 pt-4 md:hidden">
          <NuxtLink to="/" class="rounded-lg px-4 py-2.5 text-emerald-100 hover:bg-white/10" @click="closeMenus">الصفحة الرئيسية</NuxtLink>
          <NuxtLink to="/search" class="rounded-lg px-4 py-2.5 text-emerald-100 hover:bg-white/10" @click="closeMenus">بحث</NuxtLink>
          <NuxtLink v-if="isLoggedIn" to="/profile" class="rounded-lg px-4 py-2.5 text-emerald-100 hover:bg-white/10" @click="closeMenus">الملف الشخصي</NuxtLink>
          <NuxtLink v-if="isLawyer" to="/consultations/free-messages" class="rounded-lg px-4 py-2.5 text-emerald-100 hover:bg-white/10" @click="closeMenus">الاستشارات المجانية</NuxtLink>

          <div class="my-2 h-px bg-white/10"></div>

          <template v-if="!isLoggedIn">
            <NuxtLink to="/auth/login" class="rounded-lg px-4 py-2.5 text-emerald-100 hover:bg-white/10" @click="closeMenus">تسجيل الدخول</NuxtLink>
            <NuxtLink to="/auth/register" class="rounded-lg bg-amber-400 px-4 py-2.5 text-center font-semibold text-emerald-900" @click="closeMenus">إنشاء حساب</NuxtLink>
          </template>
          <template v-else>
            <div class="rounded-lg px-4 py-2.5 text-sm text-emerald-200/80">
              {{ authStore.user?.userName || authStore.user?.email }}
            </div>
            <button
                class="rounded-lg px-4 py-2.5 text-start text-red-300 hover:bg-red-500/10"
                @click="handleLogout"
            >
              تسجيل الخروج
            </button>
          </template>
        </div>
      </Transition>
    </div>
  </nav>
</template>
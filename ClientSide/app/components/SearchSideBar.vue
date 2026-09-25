<!-- components/SearchSideBar.vue -->
<script setup lang="ts">
import { useLawyerSearchStore } from '~/stores/lawyerSearch';
import { watchDebounced } from '@vueuse/core';
import { EGYPT_GOVERNORATES } from '~/data/egyptLocations';

const store = useLawyerSearchStore();

watchDebounced(
    () => ({ ...store.filters }),
    () => {
      store.fetchLawyers(1);
    },
    { debounce: 500, maxWait: 1500, deep: true }
);

const applyFilters = () => {
  store.fetchLawyers(1);
};

const isStackedLayout = ref(false);
const isScrolled = ref(false);
const isManuallyOpen = ref(false);

const isCompact = computed(
    () => isStackedLayout.value && isScrolled.value && !isManuallyOpen.value
);

const filterHint = computed(() => {
  const { search, state, specialization, city } = store.filters;
  const parts = [search, state, specialization, city].filter(Boolean);
  return parts.length ? parts.join(' · ') : 'اضغط لفتح الفلاتر';
});

const syncLayout = () => {
  if (typeof window === 'undefined') return;
  isStackedLayout.value = window.innerWidth < 1024;
  if (!isStackedLayout.value) {
    isScrolled.value = false;
    isManuallyOpen.value = false;
  }
};

const onScroll = () => {
  if (typeof window === 'undefined' || !isStackedLayout.value) return;
  const scrolled = window.scrollY > 48;
  if (scrolled && !isScrolled.value) {
    isManuallyOpen.value = false;
  }
  isScrolled.value = scrolled;
};

const expandFilters = () => {
  isManuallyOpen.value = true;
};

onMounted(() => {
  syncLayout();
  onScroll();
  window.addEventListener('resize', syncLayout);
  window.addEventListener('scroll', onScroll, { passive: true });
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', syncLayout);
  window.removeEventListener('scroll', onScroll);
});
</script>

<template>
  <div
      class="border border-emerald-900/10 bg-white text-right shadow-sm"
      :class="isCompact ? 'rounded-xl px-3 py-2' : 'space-y-4 rounded-2xl p-5'"
      dir="rtl"
  >
    <button
        v-if="isCompact"
        type="button"
        class="flex min-h-11 w-full items-center justify-between gap-3"
        @click="expandFilters"
    >
      <span class="font-bold text-emerald-900" style="font-family: 'Amiri', serif;">تصفية البحث</span>
      <span class="flex min-w-0 items-center gap-2 text-xs text-gray-500">
        <span class="truncate">{{ filterHint }}</span>
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 shrink-0" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.17l3.71-3.94a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
        </svg>
      </span>
    </button>

    <template v-else>
      <h2 class="border-b border-gray-100 pb-3 text-lg font-bold text-emerald-900" style="font-family: 'Amiri', serif;">
        تصفية البحث
      </h2>

      <div class="space-y-1">
        <label class="text-xs font-medium text-gray-800">بحث عام</label>
        <input
            v-model="store.filters.search"
            type="text"
            placeholder="اسم المحامي أو الكلمة المفتاحية..."
            class="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
        />
      </div>

      <div class="space-y-1">
        <label class="text-xs font-medium text-gray-800">المحافظة</label>
        <select
            v-model="store.filters.state"
            class="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
        >
          <option value="">جميع المحافظات</option>
          <option v-for="governorate in EGYPT_GOVERNORATES" :key="governorate" :value="governorate">
            {{ governorate }}
          </option>
        </select>
      </div>

      <div class="space-y-1">
        <label class="text-xs font-medium text-gray-800">التخصص القانوني</label>
        <select
            v-model="store.filters.specialization"
            class="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
        >
          <option value="">جميع التخصصات</option>
          <option value="Real Estate">عقارات (Real Estate)</option>
          <option value="Commercial">قانون تجاري (Commercial)</option>
          <option value="Labor">قضايا عمالية (Labor)</option>
        </select>
      </div>

      <div class="space-y-1">
        <label class="text-xs font-medium text-gray-800">المدينة</label>
        <input
            v-model="store.filters.city"
            type="text"
            placeholder="حدد المدينة..."
            class="w-full rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
        />
      </div>

      <button
          type="button"
          class="mt-2 min-h-11 w-full rounded-lg bg-emerald-800 py-3 text-sm font-medium text-white transition-colors hover:bg-emerald-900"
          @click="applyFilters"
      >
        تطبيق الفلتر
      </button>
    </template>
  </div>
</template>

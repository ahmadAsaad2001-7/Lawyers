<!-- components/SearchSideBar.vue -->
<script setup lang="ts">
import { useLawyerSearchStore } from '~/stores/lawyerSearch';
import { watchDebounced } from '@vueuse/core';
import { EGYPT_GOVERNORATES } from '~/data/egyptLocations';

const store = useLawyerSearchStore();

watchDebounced(
    () => ({ ...store.filters }),
    (newFilters, oldFilters) => {
      // Resets to page 1 on fresh search trigger
      store.fetchLawyers(1);
    },
    { debounce: 500, maxWait: 1500, deep: true }
);

const applyFilters = () => {
  store.fetchLawyers(1);
};
</script>

<template>
  <div class="bg-white rounded-2xl p-5 shadow-sm border border-emerald-900/10 space-y-4 text-right" dir="rtl">
    <h2 class="font-bold text-emerald-900 text-lg border-b border-gray-100 pb-3" style="font-family: 'Amiri', serif;">
      تصفية البحث
    </h2>

    <div class="space-y-1">
      <label class="text-xs text-gray-800 font-medium">بحث عام</label>
      <input
          v-model="store.filters.search"
          type="text"
          placeholder="اسم المحامي أو الكلمة المفتاحية..."
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
      />
    </div>

    <div class="space-y-1">
      <label class="text-xs text-gray-800 font-medium">المحافظة</label>
      <select
          v-model="store.filters.state"
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
      >
        <option value="">جميع المحافظات</option>
        <option v-for="governorate in EGYPT_GOVERNORATES" :key="governorate" :value="governorate">
          {{ governorate }}
        </option>
      </select>
    </div>

    <div class="space-y-1">
      <label class="text-xs text-gray-800 font-medium">التخصص القانوني</label>
      <select
          v-model="store.filters.specialization"
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
      >
        <option value="">جميع التخصصات</option>
        <option value="Real Estate">عقارات (Real Estate)</option>
        <option value="Commercial">قانون تجاري (Commercial)</option>
        <option value="Labor">قضايا عمالية (Labor)</option>
      </select>
    </div>

    <div class="space-y-1">
      <label class="text-xs text-gray-800 font-medium">المدينة</label>
      <input
          v-model="store.filters.city"
          type="text"
          placeholder="حدد المدينة..."
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-amber-400"
      />
    </div>

    <button
        @click="applyFilters"
        class="mt-2 min-h-11 w-full rounded-lg bg-emerald-800 py-3 text-sm font-medium text-white transition-colors hover:bg-emerald-900"
    >
      تطبيق الفلتر
    </button>
  </div>
</template>
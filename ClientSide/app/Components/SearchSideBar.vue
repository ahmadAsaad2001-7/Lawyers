<!-- components/SearchSideBar.vue -->
<script setup lang="ts">
import { useLawyerSearchStore } from '~/stores/lawyerSearch';
import { watchDebounced } from '@vueuse/core'; // Optional: npx i @vueuse/core for easy debouncing

const store = useLawyerSearchStore();

// Trigger fetch when specific filters change
watchDebounced(
    () => [store.filters.search, store.filters.state, store.filters.specialization, store.filters.city],
    () => {
      store.fetchLawyers(1); // Reset to page 1 on filter change
    },
    { debounce: 500, maxWait: 1000 }
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

    <!-- General Search -->
    <div class="space-y-1">
      <label class="text-xs text-gray-600 font-medium">بحث عام</label>
      <input
          v-model="store.filters.search"
          type="text"
          placeholder="اسم المحامي أو الكلمة المفتاحية..."
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
      />
    </div>

    <!-- By State/Region -->
    <div class="space-y-1">
      <label class="text-xs text-gray-600 font-medium">المنطقة / المحافظة</label>
      <select
          v-model="store.filters.state"
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
      >
        <option value="">جميع المناطق</option>
        <option value="الرياض">منطقة الرياض</option>
        <option value="مكة">مكة المكرمة</option>
      </select>
    </div>

    <!-- Section of Law -->
    <div class="space-y-1">
      <label class="text-xs text-gray-600 font-medium">التخصص القانوني</label>
      <select
          v-model="store.filters.specialization"
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
      >
        <option value="">جميع التخصصات</option>
        <option value="تجاري">قانون تجاري</option>
        <option value="عمالي">قضايا عمالية</option>
      </select>
    </div>

    <!-- City -->
    <div class="space-y-1">
      <label class="text-xs text-gray-600 font-medium">المدينة</label>
      <input
          v-model="store.filters.city"
          type="text"
          placeholder="حدد المدينة..."
          class="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
      />
    </div>

    <button
        @click="applyFilters"
        class="w-full bg-emerald-800 text-white font-medium py-2 rounded-lg hover:bg-emerald-900 transition-colors text-sm"
    >
      تطبيق الفلتر
    </button>
  </div>
</template>

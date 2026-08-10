<!-- components/CardList.vue -->
<script setup lang="ts">
import { onMounted } from 'vue';
import { useLawyerSearchStore } from '~/stores/lawyerSearch';
// Assuming your card component is saved as LawyerCard.vue
import Card from './Card.vue';

const store = useLawyerSearchStore();

// Fetch the first page of lawyers as soon as the component loads
onMounted(() => {
  store.fetchLawyers(1);
});
</script>

<template>
  <div class="w-full space-y-6" dir="rtl">

    <!-- Loading State -->
    <div v-if="store.isLoading" class="flex justify-center items-center py-24">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-emerald-800"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="!store.lawyers?.items.length" class="bg-white rounded-2xl p-10 text-center border border-emerald-900/10 shadow-sm">
      <h3 class="text-lg font-bold text-emerald-900 mb-2">لا يوجد نتائج</h3>
      <p class="text-gray-500 text-sm">لم نتمكن من العثور على محامين يتطابقون مع معايير البحث الخاصة بك.</p>
      <button @click="store.fetchLawyers(1)" class="mt-4 text-amber-500 hover:text-amber-600 text-sm font-medium">
        إعادة ضبط البحث
      </button>
    </div>

    <!-- Cards Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      <Card
          v-for="lawyer in store.lawyers.items"
          :key="lawyer.id"
          :lawyer="lawyer"
      />
    </div>

    <!-- Pagination -->
    <div v-if="store.totalPages > 1 && !store.isLoading" class="flex items-center justify-center gap-4 pt-6 border-t border-gray-100">
      <button
          :disabled="store.currentPage === 1"
          @click="store.fetchLawyers(store.currentPage - 1)"
          class="px-4 py-2 text-sm font-medium rounded-lg border border-gray-200 hover:bg-emerald-50 text-emerald-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        السابق
      </button>

      <span class="text-sm text-gray-600 font-medium">
        صفحة {{ store.currentPage }} من {{ store.totalPages }}
      </span>

      <button
          :disabled="store.currentPage === store.totalPages"
          @click="store.fetchLawyers(store.currentPage + 1)"
          class="px-4 py-2 text-sm font-medium rounded-lg border border-gray-200 hover:bg-emerald-50 text-emerald-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        التالي
      </button>
    </div>

  </div>
</template>
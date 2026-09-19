<script setup lang="ts">
const stats = ref({
  totalConsultations: 0,
  totalEarnings: 0,
  averageRating: 0,
  completedConsultations: 0,
  cancelledConsultations: 0
})
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 shadow-sm p-6">
    <h2 class="text-xl font-bold text-emerald-950 mb-6" style="font-family: 'Amiri', serif;">
      الإحصائيات
    </h2>

    <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
      <div class="bg-emerald-50 rounded-xl p-4 text-center">
        <div class="text-2xl font-bold text-emerald-900">{{ stats.totalConsultations }}</div>
        <div class="text-xs text-gray-600 mt-1">إجمالي الاستشارات</div>
      </div>
      <div class="bg-amber-50 rounded-xl p-4 text-center">
        <div class="text-2xl font-bold text-amber-900">{{ stats.totalEarnings }} ج.م</div>
        <div class="text-xs text-gray-600 mt-1">إجمالي الأرباح</div>
      </div>
      <div class="bg-blue-50 rounded-xl p-4 text-center">
        <div class="text-2xl font-bold text-blue-900">{{ stats.averageRating }} ★</div>
        <div class="text-xs text-gray-600 mt-1">متوسط التقييم</div>
      </div>
      <div class="bg-gray-50 rounded-xl p-4 text-center">
        <div class="text-2xl font-bold text-gray-900">{{ stats.completedConsultations }}</div>
        <div class="text-xs text-gray-600 mt-1">مكتملة</div>
      </div>
      <div class="bg-red-50 rounded-xl p-4 text-center">
        <div class="text-2xl font-bold text-red-900">{{ stats.cancelledConsultations }}</div>
        <div class="text-xs text-gray-600 mt-1">ملغية</div>
      </div>
    </div>
  </div>
</template>
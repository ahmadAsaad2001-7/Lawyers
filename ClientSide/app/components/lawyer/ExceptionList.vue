<script setup lang="ts">
enum ExceptionType {
  Closed = 0,
  OpenEarly = 1,
  OpenLate = 2,
  ModifiedHours = 3
}

// ✅ 2. Now the interface knows what ExceptionType is
interface Exception {
  id: number;
  date: string;
  type: ExceptionType;
  reason?: string;
  startTime?: string;
  endTime?: string;
}

const props = defineProps<{
  exceptions: Exception[];
}>();

const emit = defineEmits<{
  add: [];
  delete: [id: number];
}>();

const getTypeLabel = (type: ExceptionType) => {
  const labels: Record<ExceptionType, string> = {
    [ExceptionType.Closed]: 'مغلق',
    [ExceptionType.OpenEarly]: 'مفتوح (ساعات إضافية)',
    [ExceptionType.OpenLate]: 'مفتوح (ساعات إضافية)',
    [ExceptionType.ModifiedHours]: 'ساعات معدلة',
  };
  return labels[type];
};

const getTypeColor = (type: ExceptionType) => {
  const colors: Record<ExceptionType, string> = {
    [ExceptionType.Closed]: 'bg-red-100 text-red-800',
    [ExceptionType.OpenEarly]: 'bg-emerald-100 text-emerald-800',
    [ExceptionType.OpenLate]: 'bg-emerald-100 text-emerald-800',
    [ExceptionType.ModifiedHours]: 'bg-amber-100 text-amber-800',
  };
  return colors[type];
};
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-lg font-bold text-emerald-950">الاستثناءات والعطلات</h2>
      <button
          @click="emit('add')"
          class="px-4 py-2 bg-emerald-800 text-white rounded-xl text-sm font-medium hover:bg-emerald-900 transition-colors"
      >
        + إضافة استثناء
      </button>
    </div>

    <div v-if="!exceptions.length" class="text-center py-8 text-gray-400 text-sm">
      لا توجد استثناءات محددة
    </div>

    <div v-else class="space-y-2">
      <div
          v-for="exc in exceptions"
          :key="exc.id"
          class="flex items-center justify-between p-4 rounded-xl border border-gray-100 hover:border-emerald-200 transition-colors"
      >
        <div class="flex items-center gap-4">
          <div class="w-24 text-sm font-medium text-gray-700">
            {{ new Date(exc.date).toLocaleDateString('ar-EG', { weekday: 'short', day: 'numeric', month: 'short' }) }}
          </div>

          <span
              class="px-3 py-1 rounded-full text-xs font-semibold"
              :class="getTypeColor(exc.type)"
          >
            {{ getTypeLabel(exc.type) }}
          </span>

          <span v-if="exc.reason" class="text-sm text-gray-500">
            {{ exc.reason }}
          </span>

          <span v-if="exc.startTime && exc.endTime" class="text-sm text-gray-600">
            {{ exc.startTime }} - {{ exc.endTime }}
          </span>
        </div>

        <button
            @click="emit('delete', exc.id)"
            class="text-gray-400 hover:text-red-600 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>
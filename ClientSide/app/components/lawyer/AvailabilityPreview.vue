<script setup lang="ts">
interface DayPreview {
  date: string;
  dayName: string;
  availableHours: number[];
  isFullyAvailable: boolean;
  isFullyClosed: boolean;
}

const props = defineProps<{
  nextSevenDays: DayPreview[];
}>();

const formatHours = (hours: number[]) => {
  if (hours.length === 0) return 'مغلق';
  if (hours.length === 24) return '24 ساعة';

  // Group consecutive hours
  const ranges: string[] = [];
  let start = hours[0];
  let end = hours[0];

  for (let i = 1; i < hours.length; i++) {
    if (hours[i] === end + 1) {
      end = hours[i];
    } else {
      ranges.push(`${start}:00 - ${end + 1}:00`);
      start = hours[i];
      end = hours[i];
    }
  }
  ranges.push(`${start}:00 - ${end + 1}:00`);

  return ranges.join(', ');
};
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
    <h2 class="text-lg font-bold text-emerald-950 mb-6">
      معاينة: ما يراه العملاء خلال الأيام السبعة القادمة
    </h2>

    <div class="space-y-3">
      <div
          v-for="day in nextSevenDays"
          :key="day.date"
          class="flex items-center justify-between p-4 rounded-xl border"
          :class="day.isFullyClosed ? 'bg-gray-50 border-gray-200' : 'bg-emerald-50/30 border-emerald-200'"
      >
        <div class="flex items-center gap-4">
          <div class="w-32 text-sm font-medium text-gray-700">
            {{ day.dayName }}
          </div>
          <div class="text-xs text-gray-400">
            {{ new Date(day.date).toLocaleDateString('ar-EG', { day: 'numeric', month: 'short' }) }}
          </div>
        </div>

        <div class="flex-1 px-4">
          <span
              class="text-sm"
              :class="day.isFullyClosed ? 'text-gray-500' : 'text-emerald-800 font-medium'"
          >
            {{ formatHours(day.availableHours) }}
          </span>
        </div>

        <div
            class="w-3 h-3 rounded-full"
            :class="day.isFullyClosed ? 'bg-gray-300' : day.isFullyAvailable ? 'bg-emerald-500' : 'bg-amber-500'"
        />
      </div>
    </div>

    <div class="mt-4 flex items-center gap-4 text-xs text-gray-500">
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded-full bg-emerald-500" />
        <span>متاح بالكامل</span>
      </div>
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded-full bg-amber-500" />
        <span>متاح جزئياً</span>
      </div>
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded-full bg-gray-300" />
        <span>مغلق</span>
      </div>
    </div>
  </div>
</template>
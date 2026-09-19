<script setup lang="ts">
enum DayOfWeek {
  Sunday = 0,
  Monday = 1,
  Tuesday = 2,
  Wednesday = 3,
  Thursday = 4,
  Friday = 5,
  Saturday = 6
}
interface DaySchedule {
  day: DayOfWeek;
  dayName: string;
  startTime: string | null;  // "09:00"
  endTime: string | null;    // "17:00"
  isEnabled: boolean;
}

const daysOfWeek = [
  { day: DayOfWeek.Saturday, dayName: 'السبت' },
  { day: DayOfWeek.Sunday, dayName: 'الأحد' },
  { day: DayOfWeek.Monday, dayName: 'الإثنين' },
  { day: DayOfWeek.Tuesday, dayName: 'الثلاثاء' },
  { day: DayOfWeek.Wednesday, dayName: 'الأربعاء' },
  { day: DayOfWeek.Thursday, dayName: 'الخميس' },
  { day: DayOfWeek.Friday, dayName: 'الجمعة' },
];

const schedule = ref<DaySchedule[]>(
    daysOfWeek.map(d => ({
      day: d.day,
      dayName: d.dayName,
      startTime: null,
      endTime: null,
      isEnabled: false,
    }))
);

const emit = defineEmits<{
  update: [schedule: DaySchedule[]];
}>();

const save = async () => {
  await $fetch('/api/lawyer-schedule/update-weekly', {
    method: 'POST',
    body: schedule.value
  });
  emit('update', schedule.value);
};

const copyFromPrevious = () => {
  // Copy Sunday-Saturday from previous week's schedule
};
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-lg font-bold text-emerald-950">الجدول الأسبوعي المتكرر</h2>
      <button
          @click="copyFromPrevious"
          class="text-xs text-emerald-700 hover:text-emerald-800 font-medium"
      >
        نسخ من الأسبوع السابق
      </button>
    </div>

    <div class="space-y-3">
      <div
          v-for="day in schedule"
          :key="day.day"
          class="flex items-center gap-4 p-3 rounded-xl border border-gray-100 hover:border-emerald-200 transition-colors"
      >
        <!-- Day Name + Checkbox -->
        <div class="flex items-center gap-3 w-40">
          <input
              v-model="day.isEnabled"
              type="checkbox"
              class="w-4 h-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
          />
          <span class="text-sm font-medium text-gray-700">{{ day.dayName }}</span>
        </div>

        <!-- Time Inputs -->
        <div class="flex items-center gap-2 flex-1" v-if="day.isEnabled">
          <input
              v-model="day.startTime"
              type="time"
              class="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
          <span class="text-gray-400">-</span>
          <input
              v-model="day.endTime"
              type="time"
              class="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
          />
        </div>

        <!-- OFF Badge -->
        <span v-else class="text-xs text-gray-400 font-medium">يوم عطلة</span>

        <!-- Quick Actions -->
        <button
            v-if="day.isEnabled"
            @click="day.isEnabled = false; day.startTime = null; day.endTime = null"
            class="text-xs text-red-600 hover:text-red-700"
        >
          إزالة
        </button>
      </div>
    </div>

    <div class="mt-6 flex justify-end">
      <button
          @click="save"
          class="px-6 py-2.5 bg-emerald-800 text-white rounded-xl text-sm font-medium hover:bg-emerald-900 transition-colors"
      >
        حفظ الجدول
      </button>
    </div>
  </div>
</template>
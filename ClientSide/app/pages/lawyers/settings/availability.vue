<script setup lang="ts">
definePageMeta({
  layout: 'default',
  middleware: ['lawyer-only']
})

const { data: schedule, refresh: refreshSchedule } = await useFetch<any[]>('/api/lawyer-schedule/my-weekly', {
  method: 'GET'
})

const { data: exceptions, refresh: refreshExceptions } = await useFetch<any[]>('/api/lawyer-schedule/my-exceptions', {
  method: 'GET'
})

const { data: preview } = await useFetch<any[]>('/api/lawyer-schedule/next-7-days', {
  method: 'GET'
})

// ✅ Added missing functions
const handleScheduleUpdate = async () => {
  // Refresh data after successful update
  await refreshSchedule()
}

const openExceptionModal = () => {
  // TODO: Implement modal logic (e.g., set a ref to true to show a modal component)
  console.log('Open exception modal')
}

const handleExceptionDelete = async (id: number) => {
  if (confirm('هل أنت متأكد من حذف هذا الاستثناء؟')) {
    try {
      await $fetch(`/api/lawyer-schedule/exceptions/${id}`, { method: 'DELETE' })
      await refreshExceptions()
    } catch (error) {
      console.error('Failed to delete exception:', error)
    }
  }
}
</script>

<template>
  <div class="max-w-5xl mx-auto p-6 space-y-8">
    <div>
      <h1 class="text-2xl font-bold text-emerald-950" style="font-family: 'Amiri', serif;">
        إعدادات التوفر
      </h1>
      <p class="text-sm text-gray-500 mt-1">
        حدد ساعات عملك الأسبوعية وأيام العطلة
      </p>
    </div>

    <!-- Section 1: Weekly Schedule -->
    <WeeklyScheduleEditor
        :schedule="schedule"
        @update="handleScheduleUpdate"
    />

    <!-- Section 2: Exceptions -->
    <ExceptionList
        :exceptions="exceptions"
        @add="openExceptionModal"
        @delete="handleExceptionDelete"
    />

    <!-- Section 3: Preview -->
    <AvailabilityPreview
        :next-seven-days="preview"
    />
  </div>
</template>
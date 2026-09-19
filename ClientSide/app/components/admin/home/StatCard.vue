<script setup lang="ts">
defineProps<{
  title: string
  value: string | number
  icon: string
  color?: 'emerald' | 'blue' | 'purple' | 'amber'
}>()

const colors: Record<string, string> = {
  emerald: 'bg-emerald-100 text-emerald-600',
  blue: 'bg-blue-100 text-blue-600',
  purple: 'bg-purple-100 text-purple-600',
  amber: 'bg-amber-100 text-amber-600',
}
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
    <div class="flex items-center justify-between">
      <p class="text-xs font-medium uppercase tracking-wide text-gray-500">{{ title }}</p>
      <span class="flex size-9 items-center justify-center rounded-lg" :class="colors[color ?? 'emerald']">
        <UIcon :name="icon" class="size-5" />
      </span>
    </div>
    <p class="mt-2 text-2xl font-bold text-gray-900">{{ value }}</p>
  </div>
</template>
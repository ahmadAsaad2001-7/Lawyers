<script setup lang="ts">
import type { Range } from '~/types'

const selected = defineModel<Range>({ required: true })

const toInput = (d: Date) => {
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

const start = computed({
  get: () => toInput(selected.value.start),
  set: (v) => { if (v) selected.value = { start: new Date(v), end: selected.value.end } },
})
const end = computed({
  get: () => toInput(selected.value.end),
  set: (v) => { if (v) selected.value = { start: selected.value.start, end: new Date(v) } },
})

const presets = [
  { label: '7d', days: 7 },
  { label: '30d', days: 30 },
  { label: '90d', days: 90 },
]
const applyPreset = (days: number) => {
  selected.value = { start: new Date(Date.now() - days * 864e5), end: new Date() }
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-2 rounded-lg border border-gray-200 bg-white px-3 py-2 shadow-sm">
    <UIcon name="i-lucide-calendar" class="size-4 text-gray-400" />
    <input v-model="start" type="date" class="text-sm text-gray-700 outline-none" />
    <span class="text-gray-400">→</span>
    <input v-model="end" type="date" class="text-sm text-gray-700 outline-none" />
    <div class="ml-1 flex gap-1 border-l border-gray-200 pl-2">
      <button
          v-for="p in presets"
          :key="p.days"
          type="button"
          class="rounded px-2 py-0.5 text-xs font-medium text-gray-500 hover:bg-emerald-50 hover:text-emerald-700"
          @click="applyPreset(p.days)"
      >
        {{ p.label }}
      </button>
    </div>
  </div>
</template>
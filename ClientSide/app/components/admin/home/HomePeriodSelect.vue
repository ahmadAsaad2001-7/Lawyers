<script setup lang="ts">
import type { Period } from '~/types'

const model = defineModel<Period>({ required: true })

const options: { label: string; value: Period }[] = [
  { label: 'Daily', value: 'daily' },
  { label: 'Weekly', value: 'weekly' },
  { label: 'Monthly', value: 'monthly' },
]
</script>

<template>
  <div class="flex rounded-lg border border-gray-200 bg-white p-1 shadow-sm">
    <button
        v-for="o in options"
        :key="o.value"
        type="button"
        class="rounded-md px-3 py-1.5 text-sm font-medium transition-colors"
        :class="model === o.value ? 'bg-emerald-600 text-white' : 'text-gray-600 hover:bg-gray-100'"
        @click="model = o.value"
    >
      {{ o.label }}
    </button>
  </div>
</template>
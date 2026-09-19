<script setup lang="ts">
import type { TopLawyer } from '~/composables/useAdmin'

defineProps<{ lawyers: TopLawyer[] }>()

const money = (v: number) =>
    v.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
    <p class="text-xs font-medium uppercase tracking-wide text-gray-500">Lawyers Leader Board</p>

    <div v-if="!lawyers.length" class="py-8 text-center text-sm text-gray-400">
      No completed consultations in this period
    </div>

    <div v-else class="mt-4 space-y-3">
      <div v-for="(l, i) in lawyers" :key="l.userId" class="flex items-center gap-3">
        <span
            class="w-7 text-center text-sm font-bold"
            :class="i === 0 ? 'text-amber-500' : i === 1 ? 'text-gray-400' : i === 2 ? 'text-amber-700' : 'text-gray-300'"
        >
          {{ i + 1 }}
        </span>
        <div class="min-w-0 flex-1">
          <p class="truncate text-sm font-medium text-gray-800">{{ l.fullName }}</p>
          <p class="text-xs text-gray-400">{{ l.consultations }} consultations · {{ l.averageRating }} ★</p>
        </div>
        <span class="text-sm font-semibold text-emerald-600">{{ money(l.revenue) }}</span>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import type { SuspendedUser } from '~/composables/useAdmin'

defineProps<{ s: SuspendedUser }>()
const emit = defineEmits<{
  extend: [s: SuspendedUser]; decrease: [s: SuspendedUser]; unsuspend: [s: SuspendedUser]
}>()

const fmt = (d: string) => new Date(d).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
    <div class="flex items-start justify-between gap-4">
      <div class="flex items-center gap-3">
        <img v-if="s.profileImageUrl" :src="s.profileImageUrl" class="size-10 rounded-full object-cover" alt="" />
        <div v-else class="flex size-10 items-center justify-center rounded-full bg-red-100 text-sm font-bold text-red-600">
          {{ s.displayName.slice(0, 1).toUpperCase() }}
        </div>
        <div>
          <p class="font-medium text-gray-800">{{ s.displayName }}</p>
          <p class="text-xs text-gray-500">{{ s.email }}</p>
        </div>
      </div>
      <span class="rounded-full bg-red-100 px-2.5 py-1 text-xs font-semibold text-red-700">{{ s.remainingDays }} days left</span>
    </div>

    <div class="mt-4 rounded-lg bg-gray-50 px-4 py-3 text-sm text-gray-600">
      <span class="font-medium text-gray-700">Reason:</span> {{ s.reason }}
    </div>

    <div class="mt-3 flex gap-6 text-xs text-gray-500">
      <p>Started: <span class="font-medium text-gray-700">{{ fmt(s.startedAt) }}</span></p>
      <p>Ends: <span class="font-medium text-gray-700">{{ fmt(s.endsAt) }}</span></p>
    </div>

    <div class="mt-4 flex gap-2">
      <button class="flex-1 rounded-lg border border-gray-200 px-3 py-2 text-xs font-medium text-gray-600 hover:bg-gray-100" @click="emit('decrease', s)">− Decrease 1 day</button>
      <button class="flex-1 rounded-lg border border-gray-200 px-3 py-2 text-xs font-medium text-gray-600 hover:bg-gray-100" @click="emit('extend', s)">+ Extend 1 day</button>
      <button class="flex-1 rounded-lg bg-emerald-600 px-3 py-2 text-xs font-medium text-white hover:bg-emerald-700" @click="emit('unsuspend', s)">Unsuspend</button>
    </div>
  </div>
</template>
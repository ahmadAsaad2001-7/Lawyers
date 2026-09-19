<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
    <div class="flex items-center justify-between">
      <p class="text-xs font-medium uppercase tracking-wide text-gray-500">Platform Visits</p>
      <span class="rounded bg-gray-100 px-2 py-0.5 text-xs text-gray-500">coming soon</span>
    </div>
    <div class="mt-4 flex h-48 items-end gap-3 px-2">
      <div
          v-for="(h, i) in [40, 65, 50, 80, 60, 90, 70]"
          :key="i"
          class="flex-1 rounded-t bg-blue-200"
          :style="{ height: `${h}%` }"
      />
    </div>
    <p class="mt-3 text-center text-xs text-gray-400">Visit tracking endpoint will be added later</p>
  </div>
</template>
<script setup lang="ts">
defineProps<{ title: string }>()
const open = defineModel<boolean>({ default: false })
</script>

<template>
  <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center p-4">
    <div class="absolute inset-0 bg-black/40" @click="open = false" />
    <div class="relative w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
      <div class="mb-4 flex items-center justify-between">
        <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
        <button class="text-gray-400 hover:text-gray-600" @click="open = false">✕</button>
      </div>
      <slot />
    </div>
  </div>
</template>
<script setup lang="ts">
// Default layout
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <NavBar />
    <main class="container mx-auto px-6 py-8">
      <slot />
    </main>
  </div>
</template>
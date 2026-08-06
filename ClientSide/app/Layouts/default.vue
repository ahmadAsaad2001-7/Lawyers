<!-- layouts/default.vue -->
<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Notice the capital B to match the filename NavBar.vue -->
    <NavBar />

    <main class="container mx-auto px-6 py-8">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">

</script>

<style scoped>
</style>
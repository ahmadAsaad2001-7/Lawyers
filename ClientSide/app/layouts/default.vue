<script setup lang="ts">
// Call overlay, ringtones, and audio-unlock logic now live once in
// app.vue (see useCallRingtones composable) instead of being
// duplicated in every layout — this layout only owns its own chrome.
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <NavBar />
    <main class="container mx-auto px-6 py-8">
      <slot />
    </main>
  </div>
</template>
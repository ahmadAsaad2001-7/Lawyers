<script setup lang="ts">
definePageMeta({
  alias: ['/index']
});
</script>

<template>
  <div dir="rtl" class="flex flex-col lg:flex-row gap-6 items-start container mx-auto p-4">
    <aside class="w-full lg:sticky lg:top-6 lg:w-1/4">
      <SearchSideBar />
    </aside>

    <main class="w-full lg:w-3/4">
      <CardList />
    </main>
  </div>
  
</template>
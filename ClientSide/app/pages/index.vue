<script setup lang="ts">
definePageMeta({
  alias: ['/index']
});
</script>

<template>
  <div dir="rtl" class="flex flex-col lg:flex-row gap-6 items-start container mx-auto p-4">
    <aside class="w-full lg:w-1/4 sticky top-6">
      <SearchSideBar />
    </aside>

    <main class="w-full lg:w-3/4">
      <CardList />
    </main>
  </div>
  
</template>
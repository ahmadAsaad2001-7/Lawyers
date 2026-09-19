<script setup lang="ts">
// useRoute is a Nuxt auto-import — no explicit import needed.

// Call overlay, ringtones, and audio-unlock logic now live once in
// app.vue (see useCallRingtones composable) instead of being
// duplicated in every layout — this layout only owns the admin
// content-traversal sidebar.

const route = useRoute();

const links = [
  { label: 'Analytics', icon: 'i-lucide-chart-line', to: '/admin' },
  { label: 'Users', icon: 'i-lucide-users', to: '/admin/users' },
  { label: 'Suspended', icon: 'i-lucide-user-x', to: '/admin/suspended' },
  { label: 'Lawyers', icon: 'i-lucide-briefcase', to: '/admin/lawyers' },
  { label: 'Votes', icon: 'i-lucide-vote', to: '/admin/votes' },
];

const isActive = (to: string) =>
    to === '/admin' ? route.path === '/admin' : route.path.startsWith(to);
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <NavBar />

    <div class="mx-auto flex w-full max-w-7xl gap-6 px-4 py-6">
      <aside class="w-56 shrink-0">
        <nav class="sticky top-20 space-y-1 rounded-xl border border-gray-200 bg-white p-2 shadow-sm">
          <NuxtLink
              v-for="link in links" :key="link.to" :to="link.to"
              class="flex items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
              :class="isActive(link.to) ? 'bg-emerald-600 text-white shadow-sm' : 'text-gray-600 hover:bg-gray-100'"
          >
            <UIcon :name="link.icon" class="size-4" />
            {{ link.label }}
          </NuxtLink>
        </nav>
      </aside>

      <main class="min-w-0 flex-1">
        <slot />
      </main>
    </div>
  </div>
</template>
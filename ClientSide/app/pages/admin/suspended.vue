<script setup lang="ts">
import type { SuspendedUser } from '~/composables/useAdmin'
import type { PagedResult } from '~/types/Lawyer'
import SuspendedUserCard from '~/components/admin/users/SuspendedUserCard.vue'

definePageMeta({ layout: 'admin', middleware: ['admin-only'] })

const { getSuspendedUsers, extendSuspend, decreaseSuspend, unsuspendUser } = useAdmin()
const toast = useToast()

const page = ref(1)
const loading = ref(false)
const result = ref<PagedResult<SuspendedUser> | null>(null)

const load = async () => {
  loading.value = true
  try { result.value = await getSuspendedUsers(page.value, 12) } finally { loading.value = false }
}
watch(page, load)
onMounted(load)

const totalPages = computed(() => Math.ceil((result.value?.totalCount ?? 0) / 12))

const act = async (fn: () => Promise<unknown>, success: string) => {
  try {
    await fn()
    toast.add({ title: success, color: 'success' })
    await load()
  } catch (e: any) {
    toast.add({ title: 'Failed', description: e?.data?.message ?? e.message, color: 'error' })
  }
}

const extend = (s: SuspendedUser) => act(() => extendSuspend(s.userId, 1), 'Suspension extended by 1 day')
const decrease = (s: SuspendedUser) => act(() => decreaseSuspend(s.userId, 1), 'Suspension decreased by 1 day')
const unsuspend = (s: SuspendedUser) => act(() => unsuspendUser(s.userId), 'User unsuspended')
</script>

<template>
  <div class="space-y-4">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">Suspended Users</h1>
      <p class="text-sm text-gray-500">{{ result?.totalCount ?? 0 }} active suspensions</p>
    </div>

    <div v-if="loading" class="py-16 text-center text-gray-400">Loading…</div>
    <div v-else-if="!result?.items.length" class="rounded-xl border border-dashed border-gray-300 py-16 text-center text-gray-400">
      No suspended users 🎉
    </div>
    <div v-else class="grid gap-4 md:grid-cols-2">
      <SuspendedUserCard v-for="s in result.items" :key="s.userId" :s="s" @extend="extend" @decrease="decrease" @unsuspend="unsuspend" />
    </div>

    <div v-if="totalPages > 1" class="flex items-center justify-center gap-3">
      <button :disabled="page === 1" class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm disabled:opacity-40" @click="page--">Prev</button>
      <span class="text-sm text-gray-600">{{ page }} / {{ totalPages }}</span>
      <button :disabled="page === totalPages" class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm disabled:opacity-40" @click="page++">Next</button>
    </div>
  </div>
</template>
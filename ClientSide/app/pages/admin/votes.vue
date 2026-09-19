<script setup lang="ts">
import type { AdminVote } from '~/composables/useAdmin'
import type { PagedResult } from '~/types/Lawyer'
import VoteCard from '~/components/admin/votes/voteCard.vue'

definePageMeta({ layout: 'admin', middleware: ['admin-only'] })

const { getVotes, castVote } = useAdmin()
const toast = useToast()

const includeResolved = ref(false)
const page = ref(1)
const loading = ref(false)
const result = ref<PagedResult<AdminVote> | null>(null)

const load = async () => {
  loading.value = true
  try { result.value = await getVotes(includeResolved.value, page.value, 10) } finally { loading.value = false }
}
watch(includeResolved, () => { page.value = 1; load() })
watch(page, load)
onMounted(load)

const totalPages = computed(() => Math.ceil((result.value?.totalCount ?? 0) / 10))

const decide = async (vote: AdminVote, approve: boolean) => {
  try {
    const res = await castVote(vote.id, approve) as { isFinalized?: boolean; message?: string }
    toast.add({ title: approve ? 'Approved' : 'Disapproved', description: res.message ?? '', color: res.isFinalized ? 'success' : 'info' })
    await load()
  } catch (e: any) {
    toast.add({ title: 'Failed', description: e?.data?.message ?? e.message, color: 'error' })
  }
}
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Votes</h1>
        <p class="text-sm text-gray-500">Two-admin approval queue for bans and verifications</p>
      </div>
      <label class="flex items-center gap-2 text-sm text-gray-600">
        <input v-model="includeResolved" type="checkbox" class="size-4 accent-emerald-600" /> Show resolved
      </label>
    </div>

    <div v-if="loading" class="py-16 text-center text-gray-400">Loading…</div>
    <div v-else-if="!result?.items.length" class="rounded-xl border border-dashed border-gray-300 py-16 text-center text-gray-400">
      No pending votes
    </div>
    <div v-else class="grid gap-4 xl:grid-cols-2">
      <VoteCard v-for="vote in result.items" :key="vote.id" :vote="vote" @decide="decide" />
    </div>

    <div v-if="totalPages > 1" class="flex items-center justify-center gap-3">
      <button :disabled="page === 1" class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm disabled:opacity-40" @click="page--">Prev</button>
      <span class="text-sm text-gray-600">{{ page }} / {{ totalPages }}</span>
      <button :disabled="page === totalPages" class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm disabled:opacity-40" @click="page++">Next</button>
    </div>
  </div>
</template>
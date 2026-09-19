<script setup lang="ts">
import type { AdminVote } from '~/composables/useAdmin'

const props = defineProps<{ vote: AdminVote }>()
const emit = defineEmits<{ decide: [vote: AdminVote, approve: boolean] }>()

const intent = computed(() =>
    ({
      BanUser: { label: 'Ban User', cls: 'bg-red-100 text-red-700' },
      VerifyLawyer: { label: 'Verify Lawyer', cls: 'bg-emerald-100 text-emerald-700' },
      UnverifyLawyer: { label: 'Revoke Verification', cls: 'bg-amber-100 text-amber-700' },
    })[props.vote.actionType] ?? { label: props.vote.actionType, cls: 'bg-gray-100 text-gray-700' })

const profileLink = computed(() =>
    props.vote.actionType === 'BanUser'
        ? `/admin/users/${props.vote.targetUserId}`
        : `/admin/lawyers/${props.vote.targetUserId}`)
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm" :class="{ 'opacity-60': vote.isResolved }">
    <div class="flex items-start gap-4">
      <!-- SHOW PROFILE -->
      <NuxtLink :to="profileLink" class="shrink-0" title="Show profile">
        <img v-if="vote.targetProfileImageUrl" :src="vote.targetProfileImageUrl" class="size-12 rounded-lg object-cover" alt="" />
        <div v-else class="flex size-12 items-center justify-center rounded-lg bg-gray-100 text-sm font-bold text-gray-500">
          {{ vote.targetUserName.slice(0, 1).toUpperCase() }}
        </div>
      </NuxtLink>

      <div class="min-w-0 flex-1">
        <div class="flex items-center justify-between gap-2">
          <div class="min-w-0">
            <p class="truncate font-medium text-gray-800">{{ vote.targetUserName }}</p>
            <p class="truncate text-xs text-gray-500">{{ vote.targetEmail }}</p>
          </div>
          <!-- Stats of the vote (green = pending, gray = resolved) -->
          <span class="size-3 shrink-0 rounded-full" :class="vote.isResolved ? 'bg-gray-300' : 'bg-emerald-500'" :title="vote.isResolved ? 'Resolved' : 'Pending'" />
        </div>

        <div class="mt-2 flex flex-wrap gap-2">
          <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold" :class="intent.cls">{{ intent.label }}</span>
          <span class="rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-700">✔ {{ vote.approvalCount }} approved</span>
          <span class="rounded-full bg-red-50 px-2.5 py-0.5 text-xs font-medium text-red-600">✘ {{ vote.disapprovalCount }} disapproved</span>
        </div>
      </div>
    </div>

    <!-- deScription -->
    <div class="mt-4 rounded-lg bg-gray-50 px-4 py-3 text-sm text-gray-600">
      <span class="font-medium text-gray-700">Reason:</span> {{ vote.reason }}
      <span class="ml-2 text-xs text-gray-400">— proposed by {{ vote.initiatorName }}</span>
    </div>

    <div class="mt-4 flex justify-between">
      <button :disabled="vote.isResolved || vote.hasCurrentAdminVoted" class="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 disabled:opacity-40" @click="emit('decide', vote, false)">
        Disapprove
      </button>
      <button :disabled="vote.isResolved || vote.hasCurrentAdminVoted" class="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-40" @click="emit('decide', vote, true)">
        Approve
      </button>
    </div>
    <p v-if="vote.hasCurrentAdminVoted && !vote.isResolved" class="mt-2 text-right text-xs text-gray-400">
      You voted — waiting for the second admin
    </p>
  </div>
</template>
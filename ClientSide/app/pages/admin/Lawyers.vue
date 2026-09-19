<script setup lang="ts">
import type { AdminUserDto, PendingLawyer } from '~/composables/useAdmin'

definePageMeta({ layout: 'admin', middleware: ['admin-only'] })

const { getPendingLawyers, getUsers, proposeVerification, proposeUnverification } = useAdmin()
const toast = useToast()

const pending = ref<PendingLawyer[]>([])
const lawyers = ref<AdminUserDto[]>([])
const loading = ref(true)

const load = async () => {
  loading.value = true
  try {
    const [p, l] = await Promise.all([
      getPendingLawyers(1).then(r => r.items),
      getUsers({ roleFilter: 'Lawyer', pageSize: 50 }).then(r => r.items),
    ])
    pending.value = p
    lawyers.value = l
  } finally { loading.value = false }
}
onMounted(load)

// ── Verify / unverify modal ──
const modalOpen = ref(false)
const modalMode = ref<'verify' | 'unverify'>('verify')
const target = ref<{ id: number; name: string } | null>(null)
const reason = ref('')
const busy = ref(false)

const openModal = (mode: 'verify' | 'unverify', id: number, name: string) => {
  modalMode.value = mode
  target.value = { id, name }
  reason.value = ''
  modalOpen.value = true
}

const confirm = async () => {
  if (!target.value || reason.value.trim().length < 5) return
  busy.value = true
  try {
    if (modalMode.value === 'verify') {
      await proposeVerification(target.value.id, reason.value.trim())
      toast.add({ title: 'Verification proposed', description: 'Waiting for second admin approval.', color: 'success' })
    } else {
      await proposeUnverification(target.value.id, reason.value.trim())
      toast.add({ title: 'Unverification proposed', description: 'Waiting for second admin approval.', color: 'success' })
    }
    modalOpen.value = false
    await load()
  } catch (e: any) {
    toast.add({ title: 'Failed', description: e?.data?.message ?? e.message, color: 'error' })
  } finally { busy.value = false }
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">Lawyers</h1>
      <p class="text-sm text-gray-500">Review pending verifications and manage verified lawyers</p>
    </div>

    <!-- Pending verifications -->
    <section class="space-y-3">
      <h2 class="text-lg font-semibold text-gray-800">Pending Verification ({{ pending.length }})</h2>
      <div v-if="!pending.length" class="rounded-xl border border-dashed border-gray-300 py-10 text-center text-gray-400">
        No lawyers waiting for verification
      </div>
      <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <div v-for="l in pending" :key="l.userId" class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="font-semibold text-gray-800">{{ l.fullName }}</p>
          <p class="text-sm text-gray-500">{{ l.specialization }} · {{ l.lawFirmName || 'Independent' }}</p>
          <p class="mt-2 text-xs text-gray-500">Bar License: <span class="font-mono">{{ l.barLicenseNumber }}</span></p>
          <p class="text-xs text-gray-400">Registered {{ new Date(l.registeredAt).toLocaleDateString('en-GB') }}</p>
          <button
              class="mt-4 w-full rounded-lg bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-700"
              @click="openModal('verify', l.userId, l.fullName)"
          >
            Review & Propose Verification
          </button>
        </div>
      </div>
    </section>

    <!-- All lawyers -->
    <section class="space-y-3">
      <h2 class="text-lg font-semibold text-gray-800">All Lawyers</h2>
      <div class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 text-xs uppercase text-gray-500">
          <tr>
            <th class="px-4 py-3 text-left">Lawyer</th>
            <th class="px-4 py-3 text-left">Specialization</th>
            <th class="px-4 py-3 text-left">Rate</th>
            <th class="px-4 py-3 text-left">Rating</th>
            <th class="px-4 py-3 text-left">Status</th>
            <th class="px-4 py-3 text-right">Actions</th>
          </tr>
          </thead>
          <tbody>
          <tr v-if="loading"><td colspan="6" class="py-10 text-center text-gray-400">Loading…</td></tr>
          <tr v-else-if="!lawyers.length"><td colspan="6" class="py-10 text-center text-gray-400">No lawyers</td></tr>
          <tr v-for="l in lawyers" :key="l.id" class="border-t border-gray-100 hover:bg-gray-50">
            <td class="px-4 py-3">
              <p class="font-medium text-gray-800">{{ l.displayName }}</p>
              <p class="text-xs text-gray-500">{{ l.email }}</p>
            </td>
            <td class="px-4 py-3 text-gray-600">{{ l.specialization ?? '—' }}</td>
            <td class="px-4 py-3 text-gray-600">${{ l.hourlyRate ?? 0 }}/h</td>
            <td class="px-4 py-3 text-gray-600">{{ l.averageRating ?? 0 }} ★</td>
            <td class="px-4 py-3">
              <span v-if="l.isVerified" class="rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-700">Verified</span>
              <span v-else class="rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-700">Not verified</span>
            </td>
            <td class="px-4 py-3 text-right">
              <button
                  v-if="l.isVerified"
                  class="rounded-lg bg-amber-50 px-2.5 py-1.5 text-xs font-medium text-amber-600 hover:bg-amber-100"
                  @click="openModal('unverify', l.id, l.displayName)"
              >
                Propose Unverification
              </button>
              <button
                  v-else
                  class="rounded-lg bg-emerald-50 px-2.5 py-1.5 text-xs font-medium text-emerald-600 hover:bg-emerald-100"
                  @click="openModal('verify', l.id, l.displayName)"
              >
                Propose Verification
              </button>
            </td>
          </tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Reason modal -->
    <UModal
        v-model:open="modalOpen"
        :title="modalMode === 'verify' ? `Verify ${target?.name ?? ''}` : `Unverify ${target?.name ?? ''}`"
        description="A second admin must approve this action."
    >
      <template #body>
        <UFormField label="Reason" required>
          <UTextarea v-model="reason" :rows="3" placeholder="e.g. Bar license verified with the syndicate" />
        </UFormField>
      </template>
      <template #footer>
        <div class="flex w-full justify-end gap-2">
          <UButton color="neutral" variant="outline" @click="modalOpen = false">Cancel</UButton>
          <UButton
              :color="modalMode === 'verify' ? 'success' : 'warning'"
              :loading="busy"
              :disabled="reason.trim().length < 5"
              @click="confirm"
          >
            {{ modalMode === 'verify' ? 'Propose Verification' : 'Propose Unverification' }}
          </UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
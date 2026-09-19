<script setup lang="ts">
import type { AdminUserDto } from '~/composables/useAdmin'
import type { PagedResult } from '~/types/Lawyer'
import UsersTable from '~/components/admin/users/UsersTable.vue'
import ModalShell from '~/components/admin/ModalShell.vue'

definePageMeta({ layout: 'admin', middleware: ['admin-only'] })

const { getUsers, suspendUser } = useAdmin()
const toast = useToast()

const search = ref('')
const roleFilter = ref('')
const includeDeleted = ref(false)
const page = ref(1)
const pageSize = 10
const loading = ref(false)
const result = ref<PagedResult<AdminUserDto> | null>(null)

const load = async () => {
  loading.value = true
  try {
    result.value = await getUsers({
      search: search.value || undefined,
      roleFilter: roleFilter.value || undefined,
      includeDeleted: includeDeleted.value,
      page: page.value,
      pageSize,
    })
  } catch (e: any) {
    toast.add({ title: 'Failed to load users', description: e?.data?.message ?? e.message, color: 'error' })
  } finally {
    loading.value = false
  }
}

let debounce: ReturnType<typeof setTimeout>
watch(search, () => {
  clearTimeout(debounce)
  debounce = setTimeout(() => {
    page.value = 1
    load()
  }, 300)
})
watch([roleFilter, includeDeleted], () => {
  page.value = 1
  load()
})
watch(page, load)
onMounted(load)

const totalPages = computed(() => Math.ceil((result.value?.totalCount ?? 0) / pageSize))

// ── Suspend flow ──
const suspendTarget = ref<AdminUserDto | null>(null)
const suspendOpen = ref(false)
const reason = ref('')
const days = ref(7)
const busy = ref(false)

const openSuspend = (u: AdminUserDto) => {
  suspendTarget.value = u
  reason.value = ''
  days.value = 7
  suspendOpen.value = true
}

const confirmSuspend = async () => {
  if (!suspendTarget.value || reason.value.trim().length < 5) return
  busy.value = true
  try {
    await suspendUser(suspendTarget.value.id, reason.value.trim(), days.value)
    toast.add({
      title: 'User suspended',
      description: `${suspendTarget.value.displayName} suspended for ${days.value} days.`,
      color: 'success',
    })
    suspendOpen.value = false
    await load()
  } catch (e: any) {
    toast.add({ title: 'Failed', description: e?.data?.message ?? e.message, color: 'error' })
  } finally {
    busy.value = false
  }
}

// ── Detail modal ──
const detailTarget = ref<AdminUserDto | null>(null)
const detailOpen = ref(false)
const viewUser = (u: AdminUserDto) => {
  detailTarget.value = u
  detailOpen.value = true
}
</script>

<template>
  <div class="space-y-4">
    <div>
      <h1 class="text-2xl font-bold text-gray-900">Users</h1>
      <p class="text-sm text-gray-600">Suspend, unsuspend and inspect platform users</p>
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap items-center gap-3">
      <input
          v-model="search"
          type="text"
          placeholder="Search name, email, phone…"
          class="w-72 rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-gray-800 shadow-sm outline-none focus:border-emerald-500"
      />
      <select
          v-model="roleFilter"
          class="rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-gray-800 shadow-sm outline-none focus:border-emerald-500"
      >
        <option value="">All roles</option>
        <option value="Admin">Admin</option>
        <option value="Lawyer">Lawyer</option>
        <option value="Client">Client</option>
      </select>
      <label class="flex items-center gap-2 text-sm text-gray-700">
        <input v-model="includeDeleted" type="checkbox" class="size-4 accent-emerald-600" />
        Include deleted
      </label>
    </div>

    <UsersTable :users="result?.items ?? []" :loading="loading" @suspend="openSuspend" @view="viewUser" />

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-center gap-3">
      <button
          :disabled="page === 1"
          class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-800 disabled:opacity-40 hover:bg-gray-50 disabled:hover:bg-white"
          @click="page--"
      >
        Prev
      </button>
      <span class="text-sm text-gray-800">{{ page }} / {{ totalPages }}</span>
      <button
          :disabled="page === totalPages"
          class="rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-800 disabled:opacity-40 hover:bg-gray-50 disabled:hover:bg-white"
          @click="page++"
      >
        Next
      </button>
    </div>

    <!-- Suspend modal -->
    <ModalShell v-model="suspendOpen" :title="`Suspend ${suspendTarget?.displayName ?? ''}`">
      <label class="mb-1 block text-sm font-medium text-gray-800">Reason (required)</label>
      <textarea
          v-model="reason"
          rows="3"
          placeholder="e.g. Abusive language during consultation"
          class="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-red-500"
      />
      <label class="mb-1 mt-3 block text-sm font-medium text-gray-800">Duration (days)</label>
      <input
          v-model.number="days"
          type="number"
          min="1"
          max="365"
          class="w-28 rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-red-500"
      />
      <div class="mt-4 flex justify-end gap-2">
        <button
            class="rounded-lg border border-gray-200 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
            @click="suspendOpen = false"
        >
          Cancel
        </button>
        <button
            :disabled="busy || reason.trim().length < 5"
            class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-40"
            @click="confirmSuspend"
        >
          {{ busy ? 'Suspending…' : 'Suspend User' }}
        </button>
      </div>
    </ModalShell>

    <!-- Detail modal -->
    <ModalShell v-model="detailOpen" :title="detailTarget?.displayName ?? 'User'">
      <div v-if="detailTarget" class="space-y-2 text-sm">
        <p><span class="font-medium text-gray-700">Email:</span> {{ detailTarget.email }}</p>
        <p><span class="font-medium text-gray-700">Role:</span> {{ detailTarget.role }}</p>
        <p><span class="font-medium text-gray-700">Phone:</span> {{ detailTarget.phoneNumber || '—' }}</p>
        <p><span class="font-medium text-gray-700">Registered:</span> {{ new Date(detailTarget.createdAt).toLocaleDateString('en-GB') }}</p>
        <p><span class="font-medium text-gray-700">Consultations:</span> {{ detailTarget.consultationCount }}</p>
        <template v-if="detailTarget.role === 'Lawyer'">
          <p><span class="font-medium text-gray-700">Specialization:</span> {{ detailTarget.specialization ?? '—' }}</p>
          <p><span class="font-medium text-gray-700">Rate:</span> ${{ detailTarget.hourlyRate ?? 0 }}/h</p>
          <p><span class="font-medium text-gray-700">Rating:</span> {{ detailTarget.averageRating ?? 0 }} ★</p>
          <p><span class="font-medium text-gray-700">Verified:</span> {{ detailTarget.isVerified ? 'Yes' : 'No' }}</p>
        </template>
      </div>
      <div class="mt-4 flex justify-end">
        <button
            class="rounded-lg border border-gray-200 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
            @click="detailOpen = false"
        >
          Close
        </button>
      </div>
    </ModalShell>
  </div>
</template>
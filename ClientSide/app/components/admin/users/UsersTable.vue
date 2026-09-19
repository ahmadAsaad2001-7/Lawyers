<script setup lang="ts">
import type { AdminUserDto } from '~/composables/useAdmin'

defineProps<{ users: AdminUserDto[]; loading?: boolean }>()
const emit = defineEmits<{ suspend: [user: AdminUserDto]; view: [user: AdminUserDto] }>()

const roleClass = (role: string) =>
    role === 'Admin' ? 'bg-red-100 text-red-700'
        : role === 'Lawyer' ? 'bg-emerald-100 text-emerald-700'
            : 'bg-blue-100 text-blue-700'

const initials = (name?: string | null) => {
  const safeName = (name || '').trim()
  if (!safeName) return '?'
  return safeName
      .split(' ')
      .map(part => part[0] || '')
      .join('')
      .slice(0, 2)
      .toUpperCase()
}
</script>

<template>
  <div class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
    <table class="w-full text-sm">
      <thead class="bg-gray-50 text-xs uppercase text-gray-700">
      <tr>
        <th class="px-4 py-3 text-left">User</th>
        <th class="px-4 py-3 text-left">Role</th>
        <th class="px-4 py-3 text-left">Phone</th>
        <th class="px-4 py-3 text-left">Consultations</th>
        <th class="px-4 py-3 text-left">Registered</th>
        <th class="px-4 py-3 text-left">Status</th>
        <th class="px-4 py-3 text-right">Actions</th>
      </tr>
      </thead>
      <tbody>
      <tr v-if="loading"><td colspan="7" class="py-10 text-center text-gray-400">Loading…</td></tr>
      <tr v-else-if="!users.length"><td colspan="7" class="py-10 text-center text-gray-400">No users found</td></tr>
      <tr v-for="u in users" :key="u.id" class="border-t border-gray-100 hover:bg-gray-50">
        <td class="px-4 py-3">
          <div class="flex items-center gap-3">
            <img v-if="u.profileImageUrl" :src="u.profileImageUrl" class="size-9 rounded-full object-cover" alt="" />
            <div v-else class="flex size-9 items-center justify-center rounded-full bg-emerald-100 text-xs font-bold text-emerald-700">
              {{ initials(u.displayName) }}
            </div>
            <div>
              <p class="font-medium text-gray-800">{{ u.displayName }}</p>
              <p class="text-xs text-gray-600">{{ u.email }}</p>
            </div>
          </div>
        </td>
        <td class="px-4 py-3"><span class="rounded-full px-2.5 py-0.5 text-xs font-semibold" :class="roleClass(u.role)">{{ u.role }}</span></td>
        <td class="px-4 py-3 text-gray-800" dir="ltr">{{ u.phoneNumber || '—' }}</td>
        <td class="px-4 py-3 text-gray-800">{{ u.consultationCount }}</td>
        <td class="px-4 py-3 text-gray-800">{{ new Date(u.createdAt).toLocaleDateString('en-GB') }}</td>
        <td class="px-4 py-3">
          <span v-if="u.isDeleted" class="text-xs font-medium text-red-600">Deleted</span>
          <span v-else class="text-xs font-medium text-emerald-600">Active</span>
        </td>
        <td class="px-4 py-3">
          <div class="flex justify-end gap-2">
            <button class="rounded-lg border border-gray-200 px-2.5 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-100" @click="emit('view', u)">View</button>
            <button v-if="u.role !== 'Admin' && !u.isDeleted" class="rounded-lg bg-red-50 px-2.5 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100" @click="emit('suspend', u)">Suspend</button>
          </div>
        </td>
      </tr>
      </tbody>
    </table>
  </div>
</template>
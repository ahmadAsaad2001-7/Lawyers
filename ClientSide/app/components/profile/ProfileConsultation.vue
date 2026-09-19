<script setup lang="ts">
interface Consultation {
  id: number
  otherUserName: string
  scheduledAt: string
  durationMinutes: number
  status: string
}

const { data: consultations, pending } = await useFetch<Consultation[]>('/api/consultations/my-consultations', {
  server: false
})

const getStatusColor = (status: string) => {
  const map: Record<string, string> = {
    'Pending': 'bg-amber-100 text-amber-800',
    'Confirmed': 'bg-emerald-100 text-emerald-800',
    'InProgress': 'bg-blue-100 text-blue-800',
    'Completed': 'bg-gray-100 text-gray-800',
    'Cancelled': 'bg-red-100 text-red-800'
  }
  return map[status] || 'bg-gray-100 text-gray-800'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = {
    'Pending': 'قيد الانتظار',
    'Confirmed': 'مؤكد',
    'InProgress': 'جاري',
    'Completed': 'مكتمل',
    'Cancelled': 'ملغي'
  }
  return map[status] || status
}
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 shadow-sm overflow-hidden">
    <div class="p-6 border-b border-gray-100">
      <h2 class="text-lg font-bold text-emerald-950">سجل الاستشارات</h2>
    </div>

    <div v-if="pending" class="p-8 text-center text-gray-400">جاري التحميل...</div>
    <div v-else-if="!consultations?.length" class="p-8 text-center text-gray-400">لا توجد استشارات</div>

    <div v-else class="overflow-x-auto">
      <table class="w-full text-sm text-right">
        <thead class="bg-gray-50 text-gray-500">
        <tr>
          <th class="px-6 py-3 font-medium">العميل</th>
          <th class="px-6 py-3 font-medium">التاريخ والوقت</th>
          <th class="px-6 py-3 font-medium">المدة</th>
          <th class="px-6 py-3 font-medium">الحالة</th>
          <th class="px-6 py-3 font-medium">إجراء</th>
        </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
        <tr v-for="c in consultations" :key="c.id" class="hover:bg-gray-50/50">
          <td class="px-6 py-4 font-medium text-gray-800">{{ c.otherUserName }}</td>
          <td class="px-6 py-4 text-gray-600">
            {{ new Date(c.scheduledAt).toLocaleDateString('ar-EG') }}
            <span class="text-xs text-gray-400 block">{{ new Date(c.scheduledAt).toLocaleTimeString('ar-EG', {hour:'2-digit', minute:'2-digit'}) }}</span>
          </td>
          <td class="px-6 py-4 text-gray-600">{{ c.durationMinutes }} دقيقة</td>
          <td class="px-6 py-4">
              <span class="px-2.5 py-1 rounded-full text-xs font-semibold" :class="getStatusColor(c.status)">
                {{ getStatusText(c.status) }}
              </span>
          </td>
          <td class="px-6 py-4">
            <NuxtLink
                v-if="['Confirmed', 'InProgress'].includes(c.status)"
                :to="`/consultations/${c.id}`"
                class="text-emerald-700 hover:text-emerald-800 font-medium text-xs"
            >
              دخول الغرفة
            </NuxtLink>
            <span v-else class="text-gray-400 text-xs">—</span>
          </td>
        </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
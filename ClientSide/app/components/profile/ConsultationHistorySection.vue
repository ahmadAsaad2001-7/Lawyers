<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

const config = useRuntimeConfig()
const authStore = useAuthStore()

interface Consultation {
  id: number
  otherUserName: string // Changed from clientName to match standard backend response
  scheduledAt: string
  durationMinutes: number
  status: string
  totalCost?: number
}

const consultations = ref<Consultation[]>([])
const isLoading = ref(true)

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

onMounted(async () => {
  try {
    // ✅ Fixed: Removed invalid 'server: false', added Authorization header
    const data = await $fetch<Consultation[]>(`${config.public.apiBase}/consultations/my-consultations`, {
      headers: {
        Authorization: authStore.token ? `Bearer ${authStore.token}` : ''
      }
    })
    consultations.value = data || []
  } catch (err) {
    console.error('Failed to fetch consultations:', err)
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div class="bg-white rounded-2xl border border-emerald-900/10 shadow-sm p-6">
    <h2 class="text-xl font-bold text-emerald-950 mb-6" style="font-family: 'Amiri', serif;">
      سجل الاستشارات
    </h2>

    <div v-if="isLoading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800"></div>
    </div>

    <div v-else-if="!consultations.length" class="text-center py-12">
      <div class="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4 text-2xl">
        💬
      </div>
      <h3 class="text-lg font-bold text-gray-900 mb-1">لا توجد استشارات</h3>
      <p class="text-sm text-gray-500">لم تقم بأي استشارات بعد</p>
    </div>

    <div v-else class="space-y-3">
      <div
          v-for="c in consultations"
          :key="c.id"
          class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-100 p-4 hover:shadow-sm transition-shadow"
      >
        <div>
          <p class="font-medium text-gray-900">{{ c.otherUserName }}</p>
          <p class="text-xs text-gray-500 mt-1">
            {{ new Date(c.scheduledAt).toLocaleDateString('ar-EG') }}
            • {{ c.durationMinutes }} دقيقة
          </p>
        </div>
        <div class="text-left">
          <span class="px-3 py-1 rounded-full text-xs font-semibold" :class="getStatusColor(c.status)">
            {{ getStatusText(c.status) }}
          </span>
          <p v-if="c.totalCost" class="text-sm font-bold text-emerald-900 mt-1">{{ c.totalCost }} ج.م</p>
        </div>
      </div>
    </div>
  </div>
</template>
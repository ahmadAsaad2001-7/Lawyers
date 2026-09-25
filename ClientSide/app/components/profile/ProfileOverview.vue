<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

interface OverviewData {
  totalConsultations: number
  totalEarned: number
  averageRating: number
  memberSince: string
  recentActivities: Array<{ description: string; timestamp: string }>
  upcomingConsultations: Array<{
    id: number
    otherPartyName: string
    scheduledAt: string
    durationMinutes: number
    status: string
  }>
}

const authStore = useAuthStore()
const { data: overview, pending } = await useFetch<OverviewData>('/api/profile/overview', {
  server: false
})

const formatCurrency = (val: number) => val.toLocaleString('en-US', { style: 'currency', currency: 'EGP' })
const formatDate = (dateStr: string) => new Date(dateStr).toLocaleDateString('ar-EG', { year: 'numeric', month: 'long', day: 'numeric' })
</script>

<template>
  <div class="space-y-6">
    <!-- Quick Stats -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="bg-white rounded-xl border border-emerald-900/10 p-4 text-center shadow-sm">
        <div class="text-2xl font-bold text-emerald-900">{{ overview?.totalConsultations || 0 }}</div>
        <div class="text-xs text-gray-500 mt-1">إجمالي الاستشارات</div>
      </div>
      <div class="bg-white rounded-xl border border-emerald-900/10 p-4 text-center shadow-sm" v-if="authStore.user?.role === 'Lawyer'">
        <div class="text-2xl font-bold text-emerald-900">{{ formatCurrency(overview?.totalEarned || 0) }}</div>
        <div class="text-xs text-gray-500 mt-1">إجمالي الأرباح</div>
      </div>
      <div class="bg-white rounded-xl border border-emerald-900/10 p-4 text-center shadow-sm" v-if="authStore.user?.role === 'Lawyer'">
        <div class="text-2xl font-bold text-amber-600">{{ overview?.averageRating || '0.0' }} ★</div>
        <div class="text-xs text-gray-500 mt-1">متوسط التقييم</div>
      </div>
      <div class="bg-white rounded-xl border border-emerald-900/10 p-4 text-center shadow-sm">
        <div class="text-lg font-bold text-emerald-900">{{ overview ? formatDate(overview.memberSince) : '...' }}</div>
        <div class="text-xs text-gray-500 mt-1">تاريخ الانضمام</div>
      </div>
    </div>

    <div class="grid md:grid-cols-2 gap-6">
      <!-- Recent Activity -->
      <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
        <h2 class="text-lg font-bold text-emerald-950 mb-4">النشاط الأخير</h2>
        <div v-if="pending" class="text-center py-4 text-gray-400">جاري التحميل...</div>
        <div v-else-if="!overview?.recentActivities?.length" class="text-center py-4 text-gray-400 text-sm">لا يوجد نشاط حديث</div>
        <ul v-else class="space-y-3">
          <li v-for="(activity, idx) in overview.recentActivities" :key="idx" class="flex items-start gap-3 text-sm">
            <span class="mt-1.5 w-2 h-2 rounded-full bg-emerald-500 shrink-0"></span>
            <div>
              <p class="text-gray-800">{{ activity.description }}</p>
              <p class="text-xs text-gray-400 mt-0.5">{{ new Date(activity.timestamp).toLocaleDateString('ar-EG') }}</p>
            </div>
          </li>
        </ul>
      </div>

      <!-- Upcoming Consultations -->
      <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
        <h2 class="text-lg font-bold text-emerald-950 mb-4">الاستشارات القادمة</h2>
        <div v-if="pending" class="text-center py-4 text-gray-400">جاري التحميل...</div>
        <div v-else-if="!overview?.upcomingConsultations?.length" class="text-center py-4 text-gray-400 text-sm">لا توجد استشارات قادمة</div>
        <ul v-else class="space-y-3">
          <li v-for="c in overview.upcomingConsultations" :key="c.id" class="flex flex-col gap-2 rounded-xl bg-gray-50 p-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p class="font-medium text-gray-800 text-sm">{{ c.otherPartyName }}</p>
              <p class="text-xs text-gray-500 mt-0.5">
                {{ new Date(c.scheduledAt).toLocaleDateString('ar-EG', { weekday: 'short', day: 'numeric', month: 'short' }) }}
                • {{ new Date(c.scheduledAt).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' }) }}
                ({{ c.durationMinutes }} دقيقة)
              </p>
            </div>
            <NuxtLink
                :to="`/consultations/${c.id}`"
                class="min-h-10 shrink-0 rounded-lg bg-emerald-800 px-3 py-2 text-center text-xs font-medium text-white transition-colors hover:bg-emerald-900"
            >
              دخول
            </NuxtLink>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>
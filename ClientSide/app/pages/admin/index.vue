<script setup lang="ts">
import { sub } from 'date-fns'
import type { Period, Range } from '~/types'
import type { PeriodAnalytics } from '~/composables/useAdmin'
import StatCard from '~/components/admin/home/StatCard.vue'
import AnalyticsChart from '~/components/admin/home/AnalyticsChart.vue'
import VisitsChart from '~/components/admin/home/VisitsChart.vue'
import LeaderBoard from '~/components/admin/home/LeaderBoard.vue'
import HomeDateRangePicker from '~/components/admin/home/HomeDateRangePicker.vue'
import HomePeriodSelect from '~/components/admin/home/HomePeriodSelect.vue'

definePageMeta({ layout: 'admin', middleware: ['admin-only'] })

const { getPeriodAnalytics } = useAdmin()

const range = shallowRef<Range>({
  start: sub(new Date(), { days: 30 }),
  end: new Date(),
})
const period = ref<Period>('daily')

const { data: analytics } = await useAsyncData<PeriodAnalytics | null>(
    'admin-period-analytics',
    () => getPeriodAnalytics(range.value.start.toISOString(), range.value.end.toISOString()),
    { watch: [range], default: () => null },
)

const money = (v: number) =>
    v.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
</script>

<template>
  <div class="space-y-6">
    <!-- Header + period controls -->
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Platform Analytics</h1>
        <p class="text-sm text-gray-500">Overview of platform performance metrics</p>
      </div>
      <div class="flex flex-wrap items-center gap-3">
        <HomeDateRangePicker v-model="range" />
        <HomePeriodSelect v-model="period" />
      </div>
    </div>

    <!-- Stat cards -->
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard title="Period Revenue" :value="money(analytics?.periodRevenue ?? 0)" icon="i-lucide-dollar-sign" color="emerald" />
      <StatCard title="Quarter Revenue" :value="money(analytics?.quarterRevenue ?? 0)" icon="i-lucide-trending-up" color="blue" />
      <StatCard title="Total Consultations" :value="String(analytics?.consultationsCount ?? 0)" icon="i-lucide-calendar" color="purple" />
      <StatCard title="Completion Rate" :value="`${(analytics?.completionRate ?? 0).toFixed(0)}%`" icon="i-lucide-check-circle" color="amber" />
    </div>

    <!-- Charts -->
    <div class="grid gap-6 lg:grid-cols-2">
      <AnalyticsChart
          endpoint="/admin/dashboard/chart"
          title="Platform Revenue"
          metric="revenue"
          :period="period"
          :range="range"
      />
      <VisitsChart />
    </div>

    <!-- Leader board -->
    <LeaderBoard :lawyers="analytics?.topLawyers ?? []" />
  </div>
</template>
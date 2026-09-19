<script setup lang="ts">
import type { Period, Range } from '~/types'
import type { ChartPoint } from '~/composables/useAdmin'
import { useAuthStore } from '~/stores/auth'

const props = withDefaults(defineProps<{
  endpoint: string
  title?: string
  metric?: 'revenue' | 'consultations'
  period: Period
  range: Range
}>(), { title: 'Analytics', metric: 'revenue' })

const config = useRuntimeConfig()
const authStore = useAuthStore()

const { data } = await useAsyncData<ChartPoint[]>(
    `chart-${props.endpoint}`,
    () => $fetch<ChartPoint[]>(`${config.public.apiBase}${props.endpoint}`, {
      query: {
        start: props.range.start.toISOString(),
        end: props.range.end.toISOString(),
        period: props.period,
      },
      headers: { Authorization: `Bearer ${authStore.token}` },
    }),
    { watch: [() => props.period, () => props.range], default: () => [] },
)

const points = computed(() =>
    (data.value ?? []).map(p => (props.metric === 'revenue' ? p.revenue : p.consultations)))

const total = computed(() => points.value.reduce((a, b) => a + b, 0))

// ── SVG geometry ──
const W = 600, H = 200, PAD = 12
const max = computed(() => Math.max(...points.value, 1))
const coords = computed(() => points.value.map((v, i) => ({
  x: points.value.length === 1 ? W / 2 : (i / (points.value.length - 1)) * (W - PAD * 2) + PAD,
  y: H - PAD - (v / max.value) * (H - PAD * 2),
})))
const line = computed(() => coords.value.map(c => `${c.x},${c.y}`).join(' '))
const area = computed(() => {
  const c = coords.value
  if (!c.length) return ''
  return `${c[0]!.x},${H - PAD} ${line.value} ${c[c.length - 1]!.x},${H - PAD}`
})

const money = (v: number) =>
    v.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
const formattedTotal = computed(() =>
    props.metric === 'revenue' ? money(total.value) : total.value.toLocaleString())
</script>

<template>
  <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
    <p class="text-xs font-medium uppercase tracking-wide text-gray-500">{{ title }}</p>
    <p class="mt-1 text-2xl font-bold text-gray-900">{{ formattedTotal }}</p>

    <div v-if="!points.length" class="mt-4 flex h-48 items-center justify-center text-sm text-gray-400">
      No data for this period
    </div>
    <svg v-else viewBox="0 0 600 200" class="mt-4 h-48 w-full">
      <polygon :points="area" fill="rgb(16 185 129 / 0.15)" />
      <polyline
          :points="line"
          fill="none"
          stroke="rgb(16 185 129)"
          stroke-width="3"
          stroke-linecap="round"
          stroke-linejoin="round"
      />
    </svg>
  </div>
</template>
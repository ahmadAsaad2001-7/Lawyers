<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAuthStore } from '~/stores/auth'

const config = useRuntimeConfig()
const authStore = useAuthStore()

const getHeaders = () => ({
  Authorization: authStore.token ? `Bearer ${authStore.token}` : ''
})

// --- Interfaces ---
interface WeeklySchedule {
  id?: number
  day: number
  dayName: string
  hours: number[]
  isEnabled: boolean
}

interface Exception {
  id: number
  date: string
  type: number
  reason?: string
  startTime?: string
  endTime?: string
}

interface DayPreview {
  date: string
  dayName: string
  availableHours: number[]
  isFullyAvailable: boolean
  isFullyClosed: boolean
}

// --- State ---
const mode = ref<'recurring' | 'advanced'>('recurring')
const loading = ref(false)
const saving = ref(false)
const workHours = Array.from({ length: 24 }, (_, i) => i)

const schedule = ref<WeeklySchedule[]>([
  { day: 0, dayName: 'الأحد', hours: [], isEnabled: false },
  { day: 1, dayName: 'الإثنين', hours: [], isEnabled: false },
  { day: 2, dayName: 'الثلاثاء', hours: [], isEnabled: false },
  { day: 3, dayName: 'الأربعاء', hours: [], isEnabled: false },
  { day: 4, dayName: 'الخميس', hours: [], isEnabled: false },
  { day: 5, dayName: 'الجمعة', hours: [], isEnabled: false },
  { day: 6, dayName: 'السبت', hours: [], isEnabled: false },
])

const exceptions = ref<Exception[]>([])
const preview = ref<DayPreview[]>([])

// Advanced Mode State
const currentMonth = ref(new Date())
const selectedDays = ref<number[]>([])
const selectedDayHours = ref<Record<number, number[]>>({})

// Modal state
const showDayModal = ref(false)
const dayModalDay = ref<number | null>(null)
const tempIsUnavailable = ref(false)
const tempHours = ref<number[]>([])

// --- Computed ---
const daysInMonth = computed(() => {
  const year = currentMonth.value.getFullYear()
  const month = currentMonth.value.getMonth()
  return new Date(year, month + 1, 0).getDate()
})

const monthLabel = computed(() => {
  return currentMonth.value.toLocaleDateString('ar-EG', { month: 'long', year: 'numeric' })
})

const getDayStatus = (day: number): 'available' | 'unavailable' | 'none' => {
  if (!selectedDays.value.includes(day)) return 'none'
  const hours = selectedDayHours.value[day] || []
  return hours.length > 0 ? 'available' : 'unavailable'
}

// --- Helper: Toggle hours in recurring mode ---
const toggleDayHour = (dayIndex: number, hour: number) => {
  const day = schedule.value[dayIndex]
  const idx = day.hours.indexOf(hour)
  if (idx > -1) day.hours.splice(idx, 1)
  else day.hours.push(hour)
}

// ✅ NEW: Apply recurring schedule to advanced calendar
const applyRecurringToAdvanced = () => {
  selectedDays.value = []
  selectedDayHours.value = {}

  const year = currentMonth.value.getFullYear()
  const month = currentMonth.value.getMonth()
  const daysInCurrentMonth = daysInMonth.value

  // For each day of the month, check if it matches a recurring day
  for (let day = 1; day <= daysInCurrentMonth; day++) {
    const date = new Date(year, month, day)
    const dayOfWeek = date.getDay() // 0 = Sunday, 1 = Monday, etc.

    // Find matching recurring schedule
    const recurringDay = schedule.value.find(s => s.day === dayOfWeek && s.isEnabled)

    if (recurringDay) {
      selectedDays.value.push(day)
      selectedDayHours.value[day] = [...recurringDay.hours]
    }
  }
}

// --- Advanced Day Modal ---
const openDayModal = (day: number) => {
  dayModalDay.value = day
  const hours = selectedDayHours.value[day] || []
  const isSelected = selectedDays.value.includes(day)

  if (isSelected && hours.length === 0) {
    tempIsUnavailable.value = true
    tempHours.value = []
  } else if (isSelected && hours.length > 0) {
    tempIsUnavailable.value = false
    tempHours.value = [...hours]
  } else {
    tempIsUnavailable.value = false
    tempHours.value = []
  }
  showDayModal.value = true
}

const closeDayModal = () => {
  showDayModal.value = false
  dayModalDay.value = null
}

const setUnavailable = () => {
  if (dayModalDay.value === null) return
  const day = dayModalDay.value
  if (!selectedDays.value.includes(day)) selectedDays.value.push(day)
  selectedDayHours.value[day] = []
  closeDayModal()
}

const applyAvailable = () => {
  if (dayModalDay.value === null) return
  const day = dayModalDay.value

  if (tempHours.value.length === 0) {
    alert('يرجى اختيار ساعة عمل واحدة على الأقل')
    return
  }

  if (!selectedDays.value.includes(day)) selectedDays.value.push(day)
  selectedDayHours.value[day] = [...tempHours.value]
  closeDayModal()
}

const toggleTempHour = (hour: number) => {
  const idx = tempHours.value.indexOf(hour)
  if (idx > -1) tempHours.value.splice(idx, 1)
  else tempHours.value.push(hour)
}

const prevMonth = () => {
  currentMonth.value = new Date(currentMonth.value.getFullYear(), currentMonth.value.getMonth() - 1, 1)
}

const nextMonth = () => {
  currentMonth.value = new Date(currentMonth.value.getFullYear(), currentMonth.value.getMonth() + 1, 1)
}

// --- API Methods ---
const fetchSchedule = async () => {
  loading.value = true
  try {
    const data = await $fetch<any[]>(`${config.public.apiBase}/lawyer-schedule/my-weekly`, {
      headers: getHeaders()
    })
    data.forEach((item) => {
      const existing = schedule.value.find(s => s.day === item.day)
      if (existing) {
        existing.id = item.id
        existing.isEnabled = item.isEnabled

        if (item.isEnabled && item.startTime && item.endTime) {
          const startHour = parseInt(item.startTime.split(':')[0], 10)
          const endHour = parseInt(item.endTime.split(':')[0], 10)
          const hoursArr = []
          for (let h = startHour; h < endHour; h++) {
            hoursArr.push(h)
          }
          existing.hours = hoursArr
        } else {
          existing.hours = []
        }
      }
    })

    // ✅ If we're in advanced mode, apply recurring schedule
    if (mode.value === 'advanced') {
      applyRecurringToAdvanced()
    }
  } catch (error) {
    console.error('Failed to fetch schedule:', error)
  } finally {
    loading.value = false
  }
}

const fetchExceptions = async () => {
  try {
    const data = await $fetch<Exception[]>(`${config.public.apiBase}/lawyer-schedule/my-exceptions`, {
      headers: getHeaders()
    })

    exceptions.value = data

    // ✅ Apply exceptions on top of recurring schedule
    data.forEach(exc => {
      const excDate = new Date(exc.date)

      if (excDate.getMonth() === currentMonth.value.getMonth() &&
          excDate.getFullYear() === currentMonth.value.getFullYear()) {

        const day = excDate.getDate()

        if (!selectedDays.value.includes(day)) {
          selectedDays.value.push(day)
        }

        if (exc.type === 0) {
          selectedDayHours.value[day] = []
        } else if (exc.startTime && exc.endTime) {
          const startHour = parseInt(exc.startTime.split(':')[0], 10)
          const endHour = parseInt(exc.endTime.split(':')[0], 10)
          const hoursArr = []
          for (let h = startHour; h < endHour; h++) {
            hoursArr.push(h)
          }
          selectedDayHours.value[day] = hoursArr
        } else {
          selectedDayHours.value[day] = []
        }
      }
    })

  } catch (error) {
    console.error('Failed to fetch exceptions:', error)
  }
}

const fetchPreview = async () => {
  try {
    preview.value = await $fetch<DayPreview[]>(`${config.public.apiBase}/lawyer-schedule/next-7-days`, {
      headers: getHeaders()
    })
  } catch (error) {
    console.error('Failed to fetch preview:', error)
  }
}

// ✅ UPDATED with tracing for advanced mode save
const saveSchedule = async () => {
  saving.value = true
  try {
    if (mode.value === 'recurring') {
      const pad = (n: number) => String(n).padStart(2, '0')
      const payload = schedule.value.map((d) => {
        const sorted = [...d.hours].filter((h) => h >= 0 && h <= 23).sort((a, b) => a - b)
        const enabled = d.isEnabled && sorted.length > 0
        return {
          day: d.day,
          isEnabled: enabled,
          startTime: enabled ? `${pad(sorted[0])}:00:00` : null,
          endTime: enabled ? `${pad(sorted[sorted.length - 1] + 1)}:00:00` : null
        }
      })
      await $fetch(`${config.public.apiBase}/lawyer-schedule/update-weekly`, {
        method: 'POST',
        headers: getHeaders(),
        body: payload
      })
    } else {
      const year = currentMonth.value.getFullYear()
      const month = currentMonth.value.getMonth() + 1

      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
      console.log('💾 FRONTEND: Saving advanced schedule');
      console.log('   Selected days:', selectedDays.value);
      console.log('   Selected day hours:', selectedDayHours.value);

      for (const day of selectedDays.value) {
        const dateStr = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
        const hours = selectedDayHours.value[day] || []

        let type = 0
        let startTime = null
        let endTime = null

        if (hours.length > 0) {
          type = 3
          const sorted = [...hours].sort((a, b) => a - b)
          startTime = `${String(sorted[0]).padStart(2, '0')}:00:00`
          endTime = `${String(sorted[sorted.length - 1] + 1).padStart(2, '0')}:00:00`
        }

        console.log('   📤 Sending exception for day', day);
        console.log('      Date:', dateStr);
        console.log('      Type:', type);
        console.log('      StartTime:', startTime);
        console.log('      EndTime:', endTime);

        const response = await $fetch(`${config.public.apiBase}/lawyer-schedule/add-exception`, {
          method: 'POST',
          headers: getHeaders(),
          body: {
            date: dateStr,
            type: type,
            startTime: startTime,
            endTime: endTime,
            reason: 'تحديث من الجدول المتقدم'
          }
        } as any);

        console.log('      ✅ Response:', response);
      }

      console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    }

    alert('تم حفظ الجدول بنجاح')
    await fetchSchedule()
    await fetchExceptions()
    await fetchPreview()

  } catch (error) {
    console.error('❌ Failed to save schedule:', error);
    alert('فشل حفظ الجدول')
  } finally {
    saving.value = false
  }
}

// ✅ Watch for mode changes
watch(mode, (newMode) => {
  if (newMode === 'advanced') {
    applyRecurringToAdvanced()
    fetchExceptions() // Fetch exceptions to overlay on top
  }
})

// ✅ Watch for month changes in advanced mode
watch([currentMonth, mode], () => {
  if (mode.value === 'advanced') {
    applyRecurringToAdvanced()
    fetchExceptions()
  }
})

onMounted(() => {
  fetchSchedule()
  fetchExceptions()
  fetchPreview()
})
</script>
<template>
  <div class="space-y-6" dir="rtl">

    <div class="bg-white rounded-2xl border border-emerald-900/10 p-6 shadow-sm">
      <h2 class="text-xl font-bold text-emerald-950 mb-6" style="font-family: 'Amiri', serif;">
        إعداد الجدول الزمني
      </h2>

      <div class="flex gap-2 mb-6">
        <button
            @click="mode = 'recurring'"
            class="flex-1 py-2.5 rounded-xl text-sm font-medium transition-colors"
            :class="mode === 'recurring' ? 'bg-emerald-800 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'"
        >
          أيام متكررة
        </button>
        <button
            @click="mode = 'advanced'"
            class="flex-1 py-2.5 rounded-xl text-sm font-medium transition-colors"
            :class="mode === 'advanced' ? 'bg-emerald-800 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'"
        >
          اختيار متقدم
        </button>
      </div>

      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800"></div>
      </div>

      <!-- RECURRING MODE -->
      <div v-else-if="mode === 'recurring'" class="space-y-4">
        <div
            v-for="(day, index) in schedule"
            :key="day.day"
            class="flex flex-col p-4 rounded-xl border transition-colors"
            :class="day.isEnabled ? 'border-emerald-200 bg-emerald-50/30' : 'border-gray-100 bg-white'"
        >
          <!-- Day Toggle -->
          <div class="flex items-center gap-3 cursor-pointer" @click="day.isEnabled = !day.isEnabled">
            <input
                v-model="day.isEnabled"
                type="checkbox"
                class="w-4 h-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500 pointer-events-none"
            />
            <span class="text-sm font-bold" :class="day.isEnabled ? 'text-emerald-900' : 'text-gray-600'">
              {{ day.dayName }}
            </span>
            <span v-if="!day.isEnabled" class="text-xs text-gray-400 ms-auto">يوم عطلة</span>
          </div>

          <!-- 24-Hour Grid (Shows only when day is enabled) -->
          <div v-if="day.isEnabled" class="mt-4 pt-4 border-t border-emerald-100">
            <p class="text-xs text-emerald-800 font-medium mb-3">اختر ساعات العمل المتاحة في هذا اليوم:</p>
            <div class="grid grid-cols-6 sm:grid-cols-8 md:grid-cols-12 gap-2">
              <div
                  v-for="hour in workHours"
                  :key="hour"
                  @click="toggleDayHour(index, hour)"
                  class="aspect-square rounded-lg border-2 flex items-center justify-center text-xs font-medium cursor-pointer transition-all"
                  :class="day.hours.includes(hour)
                  ? 'bg-emerald-500 border-emerald-600 text-white shadow-sm'
                  : 'bg-white border-gray-200 text-gray-700 hover:border-emerald-300'"
              >
                {{ hour }}
              </div>
            </div>
            <p class="text-xs text-gray-500 mt-2 text-end">
              محدد: {{ day.hours.length }} ساعات
            </p>
          </div>
        </div>
      </div>

      <!-- ADVANCED MODE -->
      <div v-else class="space-y-6">
        <div>
          <div class="flex items-center justify-between mb-4">
            <button @click="prevMonth" class="p-2 bg-blue-900 rounded-lg hover:bg-blue-600">←</button>
            <h3 class="font-bold text-emerald-900">{{ monthLabel }}</h3>
            <button @click="nextMonth" class="p-2 bg-blue-900 rounded-lg hover:bg-blue-600">→</button>
          </div>

          <div class="grid grid-cols-7 sm:grid-cols-8 gap-2">
            <div
                v-for="day in daysInMonth"
                :key="day"
                @click="openDayModal(day)"
                class="relative aspect-square rounded-lg border-2 flex items-center justify-center text-sm font-medium cursor-pointer transition-all"
                :class="{
                  'bg-emerald-100 border-emerald-500 text-emerald-800': getDayStatus(day) === 'available',
                  'bg-red-100 border-red-500 text-red-800': getDayStatus(day) === 'unavailable',
                  'bg-white border-gray-200 text-gray-700 hover:border-emerald-300': getDayStatus(day) === 'none'
                }"
            >
              {{ day }}
              <!-- Small indicator dot -->
              <div v-if="getDayStatus(day) === 'available'"
                   class="absolute -bottom-1 -right-1 w-3 h-3 bg-emerald-500 rounded-full border-2 border-white">
              </div>
              <div v-if="getDayStatus(day) === 'unavailable'"
                   class="absolute -bottom-1 -right-1 w-3 h-3 bg-red-500 rounded-full border-2 border-white">
              </div>
            </div>
          </div>
          <p class="text-xs text-gray-500 mt-2 text-center">
            اضغط على أي يوم لتعيين حالة اليوم (متاح / غير متاح)
          </p>
        </div>
      </div>

      <div class="mt-6 flex justify-end pt-4 border-t border-gray-100">
        <button
            @click="saveSchedule"
            :disabled="saving || loading"
            class="px-6 py-2.5 bg-emerald-800 text-white rounded-xl text-sm font-medium hover:bg-emerald-900 transition-colors disabled:opacity-50"
        >
          {{ saving ? 'جاري الحفظ...' : 'حفظ الجدول' }}
        </button>
      </div>
    </div>

    <!-- ========== NEW ADVANCED DAY MODAL ========== -->
    <div v-if="showDayModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div class="bg-white rounded-2xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <!-- Header -->
        <div class="p-6 border-b border-gray-100 flex items-center justify-between">
          <h3 class="text-lg font-bold text-emerald-950">
            تحديد حالة يوم {{ dayModalDay }} {{ monthLabel }}
          </h3>
          <button @click="closeDayModal" class="text-gray-400 hover:text-gray-600">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- Body -->
        <div class="p-6 space-y-4">
          <!-- Two buttons: unavailable / available -->
          <div class="flex gap-4 justify-center">
            <button
                @click="setUnavailable"
                class="px-6 py-3 bg-red-600 text-white rounded-xl font-semibold hover:bg-red-700 transition-colors flex-1 max-w-xs"
            >
              غير متاح
            </button>
            <button
                @click="tempIsUnavailable = false"
                class="px-6 py-3 bg-emerald-600 text-white rounded-xl font-semibold hover:bg-emerald-700 transition-colors flex-1 max-w-xs"
                :class="{ 'ring-2 ring-emerald-400': !tempIsUnavailable }"
            >
              متاح
            </button>
          </div>

          <!-- Hours grid – only shown when "متاح" is selected -->
          <div v-if="!tempIsUnavailable" class="mt-4 pt-4 border-t border-gray-100">
            <p class="text-sm text-gray-600 mb-3">اختر ساعات العمل المتاحة لهذا اليوم:</p>
            <div class="grid grid-cols-6 sm:grid-cols-8 gap-2">
              <div
                  v-for="hour in workHours"
                  :key="hour"
                  @click="toggleTempHour(hour)"
                  class="aspect-square rounded-lg border-2 flex items-center justify-center text-xs font-medium cursor-pointer transition-all"
                  :class="tempHours.includes(hour)
                  ? 'bg-emerald-500 border-emerald-600 text-white shadow-sm'
                  : 'bg-white border-gray-200 text-gray-700 hover:border-emerald-300'"
              >
                {{ hour }}:00
              </div>
            </div>
            <p class="text-sm text-gray-600 mt-4 text-center">
              عدد الساعات المحددة: {{ tempHours.length }}
            </p>
          </div>
          <div v-else class="text-center text-red-600 font-medium mt-4">
            هذا اليوم مغلق بالكامل (غير متاح)
          </div>
        </div>

        <!-- Footer -->
        <div class="p-6 border-t border-gray-100 flex justify-end gap-2">
          <button
              @click="closeDayModal"
              class="px-4 py-2 border border-gray-200 text-gray-700 rounded-xl hover:bg-gray-50 transition-colors"
          >
            إلغاء
          </button>
          <button
              v-if="!tempIsUnavailable"
              @click="applyAvailable"
              class="px-4 py-2 bg-emerald-800 text-white rounded-xl hover:bg-emerald-900 transition-colors"
          >
            تم
          </button>
          <button
              v-else
              @click="setUnavailable"
              class="px-4 py-2 bg-red-600 text-white rounded-xl hover:bg-red-700 transition-colors"
          >
            تأكيد الإغلاق
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
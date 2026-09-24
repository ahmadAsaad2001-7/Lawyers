<script setup lang="ts">
interface BookingResponseDto {
  consultationId: number;
  paymentClientSecret: string;
  totalCost: number;
  scheduledAt: string;
  status: string;
}

interface Lawyer {
  id: number;
  name: string;
  avatar: string;
  hourlyRate: number;
}

type PaymentChannel = 'Card' | 'MobileWallet' | 'InstaPay';

const props = defineProps<{ lawyer: Lawyer; isOpen: boolean }>();
const emit = defineEmits<{
  close: [];
  booked: [response: BookingResponseDto]
}>();

const { isSubmitting, bookingError, submitBooking } = useBooking();
const chatStore = useChatStore();
const config = useRuntimeConfig();

//  TRACING: Log lawyer info when modal opens
watch(() => props.isOpen, (open) => {
  if (open) {
    console.log('🔍 [TRACE] Modal opened for lawyer:', {
      id: props.lawyer.id,
      name: props.lawyer.name,
      apiBase: config.public.apiBase
    });
  }
});

const now = new Date();
const viewMonth = ref(new Date(now.getFullYear(), now.getMonth(), 1));
const selectedDate = ref<string | null>(null);
const availableHours = ref<number[]>([]);
const selectedHours = ref<number[]>([]);
const loadingHours = ref(false);
const availableDays = ref<number[]>([]);
const loadingDays = ref(false);

const paymentChannels: { id: PaymentChannel; label: string; icon: string }[] = [
  { id: 'Card', label: 'بطاقة ائتمان', icon: '💳' },
  { id: 'MobileWallet', label: 'محفظة إلكترونية', icon: '📱' },
  { id: 'InstaPay', label: 'انستا باي', icon: '⚡' },
];

const selectedChannel = ref<PaymentChannel>('Card');
const walletPhoneNumber = ref('');
const formError = ref('');

const pad = (n: number) => String(n).padStart(2, '0');

const monthLabel = computed(() =>
    viewMonth.value.toLocaleDateString('ar-EG', { month: 'long', year: 'numeric' })
);

const daysInMonth = computed(() =>
    new Date(viewMonth.value.getFullYear(), viewMonth.value.getMonth() + 1, 0).getDate()
);

// 🔍 TRACING: Enhanced fetchAvailableDays with detailed logging
const fetchAvailableDays = async () => {
  loadingDays.value = true;
  availableDays.value = [];

  const year = viewMonth.value.getFullYear();
  const month = viewMonth.value.getMonth() + 1;
  const url = `${config.public.apiBase}/lawyer-availability/${props.lawyer.id}/days`;

  console.log('🔍 [TRACE] Fetching available days:', {
    url,
    lawyerId: props.lawyer.id,
    year,
    month
  });

  try {
    const response = await $fetch<number[]>(url, {
      query: { year, month }
    });

    console.log('✅ [TRACE] Available days response:', response);
    console.log('✅ [TRACE] Number of available days:', response.length);
    console.log('✅ [TRACE] Available days:', response);

    availableDays.value = response || [];
  } catch (error: any) {
    console.error('❌ [TRACE] Failed to fetch available days:', {
      error: error.message,
      statusCode: error.statusCode,
      statusMessage: error.statusMessage,
      data: error.data,
      url: error.url
    });
    availableDays.value = [];
  } finally {
    loadingDays.value = false;
  }
};

// 🔍 TRACING: Enhanced selectDate with detailed logging
const selectDate = async (day: number) => {
  if (!availableDays.value.includes(day)) {
    console.log('⚠️ [TRACE] Day not available:', day);
    return;
  }

  const year = viewMonth.value.getFullYear();
  const month = viewMonth.value.getMonth() + 1;
  selectedDate.value = `${year}-${pad(month)}-${pad(day)}`;

  selectedHours.value = [];
  availableHours.value = [];
  loadingHours.value = true;

  const url = `${config.public.apiBase}/lawyer-availability/${props.lawyer.id}/hours`;

  console.log('🔍 [TRACE] Fetching hours for date:', {
    url,
    date: selectedDate.value,
    lawyerId: props.lawyer.id
  });

  try {
    const response = await $fetch<number[]>(url, {
      query: { date: selectedDate.value }
    });

    console.log('✅ [TRACE] Available hours response:', response);
    console.log('✅ [TRACE] Number of available hours:', response.length);

    availableHours.value = response;
  } catch (error: any) {
    console.error('❌ [TRACE] Failed to fetch hours:', {
      error: error.message,
      statusCode: error.statusCode,
      data: error.data
    });
    availableHours.value = [];
  } finally {
    loadingHours.value = false;
  }
};

const prevMonth = () => {
  console.log('🔍 [TRACE] Navigating to previous month');
  viewMonth.value = new Date(viewMonth.value.getFullYear(), viewMonth.value.getMonth() - 1, 1);
  selectedDate.value = null;
  selectedHours.value = [];
  availableDays.value = [];
  fetchAvailableDays();
};

const nextMonth = () => {
  console.log('🔍 [TRACE] Navigating to next month');
  viewMonth.value = new Date(viewMonth.value.getFullYear(), viewMonth.value.getMonth() + 1, 1);
  selectedDate.value = null;
  selectedHours.value = [];
  availableDays.value = [];
  fetchAvailableDays();
};

const toggleHour = (h: number) => {
  if (!availableHours.value.includes(h)) return;
  selectedHours.value = selectedHours.value.includes(h)
      ? selectedHours.value.filter((x) => x !== h)
      : [...selectedHours.value, h].sort((a, b) => a - b);

  console.log('🔍 [TRACE] Hour toggled:', h, 'Selected hours:', selectedHours.value);
};

const estimatedCost = computed(() =>
    (selectedHours.value.length * props.lawyer.hourlyRate).toFixed(2)
);

const contiguityError = computed(() => {
  for (let i = 1; i < selectedHours.value.length; i++) {
    if (selectedHours.value[i] !== selectedHours.value[i - 1] + 1) {
      return 'يرجى اختيار ساعات متتالية';
    }
  }
  return '';
});

const selectionSummary = computed(() => {
  if (!selectedHours.value.length) return '';
  const first = selectedHours.value[0];
  const last = selectedHours.value[selectedHours.value.length - 1];
  return `${first}:00 – ${last + 1}:00 (${selectedHours.value.length} ساعة)`;
});

watch(
    () => props.isOpen,
    (open) => {
      if (open) {
        viewMonth.value = new Date(now.getFullYear(), now.getMonth(), 1);
        selectedDate.value = null;
        selectedHours.value = [];
        availableHours.value = [];
        availableDays.value = [];
        selectedChannel.value = 'Card';
        walletPhoneNumber.value = '';
        formError.value = '';
        bookingError.value = '';
        fetchAvailableDays();
      }
    },
    { immediate: true }
);

const validate = (): boolean => {
  formError.value = '';
  if (!selectedDate.value || !selectedHours.value.length) {
    formError.value = 'يرجى اختيار اليوم وساعة واحدة على الأقل';
    return false;
  }
  if (contiguityError.value) {
    formError.value = contiguityError.value;
    return false;
  }
  if (selectedChannel.value === 'MobileWallet') {
    if (!/^01[0125][0-9]{8}$/.test(walletPhoneNumber.value)) {
      formError.value = 'يرجى إدخال رقم محفظة إلكترونية صحيح (مثل 01012345678)';
      return false;
    }
  }
  return true;
};

const handleSubmit = async () => {
  if (!validate() || !selectedDate.value || isSubmitting.value) return;

  // ✅ تحقق إضافي قبل الإرسال
  const unavailableSelected = selectedHours.value.filter(h => !availableHours.value.includes(h));
  if (unavailableSelected.length > 0) {
    formError.value = `بعض الساعات المختارة لم تعد متاحة: ${unavailableSelected.map(h => `${h}:00`).join(', ')}`;
    return;
  }

  const firstHour = selectedHours.value[0];

  // ✅ 1. إنشاء كائن Date بالتوقيت المحلي للمتصفح (مصر)
  const localDate = new Date(`${selectedDate.value}T${pad(firstHour)}:00:00`);

  // ✅ 2. التحويل التلقائي والصحيح إلى UTC (بدون تلاعب يدومي بـ getTimezoneOffset)
  // هذه الدالة تأخذ الوقت المحلي (14:00) وتحوله تلقائياً إلى ما يماثله بـ UTC (12:00Z)
  const scheduledAtStr = localDate.toISOString();

  console.log('🔍 [TRACE] Submitting booking:', {
    lawyerId: props.lawyer.id,
    scheduledAtUTC: scheduledAtStr,
    durationMinutes: selectedHours.value.length * 60,
    channel: selectedChannel.value,
    localTimeSelected: `${selectedDate.value}T${pad(firstHour)}:00:00`
  });

  const result = await submitBooking({
    lawyerId: props.lawyer.id,
    scheduledAt: scheduledAtStr, // إرسال بصيغة UTC الصحيحة
    durationMinutes: selectedHours.value.length * 60,
    channel: selectedChannel.value,
    walletPhoneNumber:
        selectedChannel.value === 'MobileWallet' ? walletPhoneNumber.value : undefined,
  });

  if (result) {
    await chatStore.ensureConsultation({
      id: result.consultationId,
      status: result.status,
      scheduledAt: result.scheduledAt,
      otherUserName: props.lawyer.name,
      otherUserImageUrl: props.lawyer.avatar || null,
      otherUserRole: 'Lawyer',
    });
    emit('booked', result as BookingResponseDto);
  }
};

const handleClose = () => {
  if (isSubmitting.value) return;
  emit('close');
};
</script>

<template>
  <Teleport to="body">
    <Transition name="model-fade">
      <div
          v-if="isOpen"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-emerald-950/40 backdrop-blur-sm"
          @click.self="handleClose"
      >
        <div
            class="bg-white rounded-2xl shadow-xl w-full max-w-md border border-emerald-900/10 overflow-hidden max-h-[90vh] overflow-y-auto"
            dir="rtl"
        >
          <!-- Header -->
          <div class="p-5 border-b border-gray-100 bg-emerald-50/40 flex items-center gap-3">
            <img :src="lawyer.avatar" :alt="lawyer.name" class="w-12 h-12 rounded-full object-cover border-2 border-amber-400" />
            <div class="flex-1">
              <h2 class="font-bold text-emerald-950 text-base" style="font-family: 'Amiri', serif;">حجز استشارة</h2>
              <p class="text-xs text-gray-500 mt-0.5">مع {{ lawyer.name }} — {{ lawyer.hourlyRate }} ج.م/ساعة</p>
            </div>
            <button
                @click="handleClose"
                :disabled="isSubmitting"
                class="text-gray-400 hover:text-gray-600 p-1 disabled:opacity-30"
                aria-label="إغلاق"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <fieldset :disabled="isSubmitting" class="p-5 space-y-4">
            <!-- ══ STEP 1: Calendar with Available/Unavailable Days ═══ -->
            <div>
              <div class="flex items-center justify-between mb-3">
                <button type="button" @click="prevMonth" class="p-1 rounded-lg text-gray-500 hover:bg-gray-100">
                  <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" /></svg>
                </button>
                <span class="text-sm font-bold text-emerald-900">{{ monthLabel }}</span>
                <button type="button" @click="nextMonth" class="p-1 rounded-lg text-gray-500 hover:bg-gray-100">
                  <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>
                </button>
              </div>

              <!-- Loading state for days -->
              <div v-if="loadingDays" class="flex justify-center py-8">
                <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800"></div>
              </div>

              <!-- Calendar Grid -->
              <div v-else class="grid grid-cols-7 gap-1.5">
                <button
                    v-for="day in daysInMonth"
                    :key="day"
                    type="button"
                    @click="selectDate(day)"
                    :disabled="!availableDays.includes(day)"
                    class="py-2 rounded-lg text-xs font-medium border transition-all"
                    :class="
                    selectedDate === `${viewMonth.getFullYear()}-${pad(viewMonth.getMonth() + 1)}-${pad(day)}`
                      ? 'bg-sky-600 text-white border-sky-600 shadow-md'
                      : availableDays.includes(day)
                        ? 'bg-emerald-50 text-emerald-700 border-emerald-300 hover:bg-emerald-100 hover:shadow-sm cursor-pointer'
                        : 'bg-red-50 text-red-300 border-red-100 cursor-not-allowed opacity-60'
                  "
                >
                  {{ day }}
                </button>
              </div>

              <!-- Legend -->
              <div class="flex items-center gap-4 mt-3 text-xs">
                <div class="flex items-center gap-1.5">
                  <div class="w-3 h-3 rounded bg-emerald-50 border border-emerald-300"></div>
                  <span class="text-gray-600">متاح</span>
                </div>
                <div class="flex items-center gap-1.5">
                  <div class="w-3 h-3 rounded bg-red-50 border border-red-100"></div>
                  <span class="text-gray-600">غير متاح</span>
                </div>
              </div>
            </div>

            <!-- ═══ STEP 2: Hours Grid ═ -->
            <div v-if="selectedDate">
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">
                الساعات المتاحة ليوم {{ new Date(`${selectedDate}T00:00:00`).toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' }) }}
              </label>

              <div v-if="loadingHours" class="flex justify-center py-6">
                <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-emerald-800"></div>
              </div>

              <div v-else-if="!availableHours.length" class="rounded-xl bg-red-50 py-4 text-center text-xs text-red-600 border border-red-100">
                لا توجد ساعات متاحة لهذا اليوم
              </div>

              <div v-else class="grid grid-cols-6 gap-1.5">
                <button
                    v-for="h in 24"
                    :key="h"
                    type="button"
                    :disabled="!availableHours.includes(h - 1)"
                    @click="toggleHour(h - 1)"
                    class="py-2 rounded-lg text-xs font-medium border transition-colors"
                    :class="[
                    selectedHours.includes(h - 1)
                      ? 'bg-emerald-700 text-white border-emerald-700 shadow-md'
                      : availableHours.includes(h - 1)
                        ? 'bg-emerald-100 text-emerald-800 border-emerald-300 hover:bg-emerald-200'
                        : 'bg-gray-50 text-gray-300 border-gray-100 cursor-not-allowed',
                  ]"
                >
                  {{ h - 1 }}:00
                </button>
              </div>

              <p v-if="selectionSummary" class="mt-3 rounded-lg bg-sky-50 border border-sky-200 px-3 py-2 text-xs font-medium text-sky-800">
                المختار: {{ selectionSummary }}
              </p>
              <p v-if="contiguityError" class="mt-1 text-[10px] text-red-500">{{ contiguityError }}</p>
            </div>

            <!-- ═══ Payment ═══ -->
            <div>
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">طريقة الدفع</label>
              <div class="grid grid-cols-3 gap-2">
                <button
                    v-for="ch in paymentChannels"
                    :key="ch.id"
                    type="button"
                    @click="selectedChannel = ch.id"
                    class="text-xs py-2 px-1 rounded-xl border flex flex-col items-center gap-1 transition-colors disabled:opacity-50"
                    :class="selectedChannel === ch.id
                    ? 'bg-emerald-800 text-white border-emerald-800'
                    : 'bg-white text-gray-700 border-gray-200 hover:border-emerald-300'"
                >
                  <span class="text-base">{{ ch.icon }}</span>
                  <span>{{ ch.label }}</span>
                </button>
              </div>
            </div>

            <div v-if="selectedChannel === 'MobileWallet'">
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">رقم المحفظة الإلكترونية</label>
              <input
                  v-model="walletPhoneNumber"
                  type="tel"
                  placeholder="010XXXXXXXX"
                  class="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <!-- ═══ Price ═══ -->
            <div class="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 flex items-center justify-between">
              <span class="text-xs font-medium text-amber-900">
                التكلفة ({{ selectedHours.length }} ساعة × {{ lawyer.hourlyRate }})
              </span>
              <span class="font-bold text-amber-900 text-sm">{{ estimatedCost }} ج.م</span>
            </div>

            <p v-if="formError" class="text-xs text-red-600 text-center">{{ formError }}</p>
            <p v-if="bookingError" class="text-xs text-red-600 text-center bg-red-50 border border-red-100 rounded-lg py-2 px-3">
              {{ bookingError }}
            </p>
          </fieldset>

          <!-- Footer -->
          <div class="p-5 pt-0 flex gap-2">
            <button
                @click="handleClose"
                :disabled="isSubmitting"
                class="flex-1 border border-gray-200 text-gray-600 py-3 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              إلغاء
            </button>
            <button
                @click="handleSubmit"
                :disabled="isSubmitting || !selectedHours.length"
                class="flex-[2] bg-amber-400 text-emerald-950 font-semibold py-3 rounded-xl text-sm hover:bg-amber-300 transition-colors disabled:opacity-60 flex items-center justify-center gap-2"
            >
              <div v-if="isSubmitting" class="animate-spin rounded-full h-4 w-4 border-b-2 border-emerald-950"></div>
              {{ isSubmitting ? 'جاري الحجز...' : 'تأكيد الحجز' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.model-fade-enter-active,
.model-fade-leave-active {
  transition: opacity 0.2s ease;
}
.model-fade-enter-from,
.model-fade-leave-to {
  opacity: 0;
}
</style>
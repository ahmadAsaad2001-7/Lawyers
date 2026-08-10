<script setup lang="ts">
import { ref, computed, watch } from 'vue';


interface Lawyer {
  id: number;
  name: string;
  avatar: string;
  hourlyRate: number;
}

const props = defineProps<{
  lawyer: Lawyer;
  isOpen: boolean;
}>();

const emit = defineEmits<{
  close: [];
  booked: [response: BookingResponseDto];
}>();

const { isSubmitting, bookingError, submitBooking } = useBooking();

const durationOptions = [
  { minutes: 30, label: '30 دقيقة' },
  { minutes: 60, label: 'ساعة واحدة' },
  { minutes: 90, label: 'ساعة ونصف' },
  { minutes: 120, label: 'ساعتان' },
];

const paymentChannels: { id: PaymentChannel; label: string; icon: string }[] = [
  { id: 'Card', label: 'بطاقة ائتمان', icon: '💳' },
  { id: 'MobileWallet', label: 'محفظة إلكترونية', icon: '📱' },
  { id: 'InstaPay', label: 'انستا باي', icon: '⚡' },
];

const selectedDuration = ref(60);
const selectedDate = ref('');
const selectedTime = ref('');
const selectedChannel = ref<PaymentChannel>('Card');
const walletPhoneNumber = ref('');
const formError = ref('');

const todayIso = computed(() => new Date().toISOString().split('T')[0]);

const estimatedCost = computed(() => {
  return ((props.lawyer.hourlyRate * selectedDuration.value) / 60).toFixed(2);
});

const combinedDateTime = computed(() => {
  if (!selectedDate.value || !selectedTime.value) return null;
  return new Date(`${selectedDate.value}T${selectedTime.value}`);
});

watch(
    () => props.isOpen,
    (open) => {
      if (open) {
        selectedDate.value = '';
        selectedTime.value = '';
        selectedDuration.value = 60;
        selectedChannel.value = 'Card';
        walletPhoneNumber.value = '';
        formError.value = '';
        bookingError.value = '';
      }
    }
);

const validate = (): boolean => {
  formError.value = '';

  if (!selectedDate.value || !selectedTime.value) {
    formError.value = 'يرجى اختيار التاريخ والوقت';
    return false;
  }

  const dt = combinedDateTime.value;
  if (!dt || dt.getTime() <= Date.now()) {
    formError.value = 'يرجى اختيار موعد في المستقبل';
    return false;
  }

  if (selectedChannel.value === 'MobileWallet') {
    const egyptPhoneRegex = /^01[0125][0-9]{8}$/;
    if (!walletPhoneNumber.value || !egyptPhoneRegex.test(walletPhoneNumber.value)) {
      formError.value = 'يرجى إدخال رقم محفظة إلكترونية صحيح (مثل 01012345678)';
      return false;
    }
  }

  return true;
};

const handleSubmit = async () => {
  if (!validate() || !combinedDateTime.value || isSubmitting.value) return;

  const result = await submitBooking({
    lawyerId: props.lawyer.id,
    scheduledAt: combinedDateTime.value.toISOString(),
    durationMinutes: selectedDuration.value,
    channel: selectedChannel.value,
    walletPhoneNumber: selectedChannel.value === 'MobileWallet' ? walletPhoneNumber.value : undefined,
  });

  if (result) {
    emit('booked', result);
  }
};

const handleClose = () => {
  if (isSubmitting.value) return;
  emit('close');
};
</script>

<template>
  <Teleport to="body">
    <Transition name="modal-fade">
      <div
          v-if="isOpen"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-emerald-950/40 backdrop-blur-sm"
          @click.self="handleClose"
      >
        <div
            class="bg-white rounded-2xl shadow-xl w-full max-w-md border border-emerald-900/10 overflow-hidden"
            dir="rtl"
        >
          <!-- Header -->
          <div class="p-5 border-b border-gray-100 bg-emerald-50/40 flex items-center gap-3">
            <img
                :src="lawyer.avatar"
                :alt="lawyer.name"
                class="w-12 h-12 rounded-full object-cover border-2 border-amber-400"
            />
            <div class="flex-1">
              <h2 class="font-bold text-emerald-950 text-base" style="font-family: 'Amiri', serif;">
                حجز استشارة
              </h2>
              <p class="text-xs text-gray-500 mt-0.5">مع {{ lawyer.name }}</p>
            </div>
            <button
                @click="handleClose"
                :disabled="isSubmitting"
                class="text-gray-400 hover:text-gray-600 transition-colors p-1 disabled:opacity-30"
                aria-label="إغلاق"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <!-- Body -->
          <fieldset :disabled="isSubmitting" class="p-5 space-y-4">
            <div>
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">التاريخ</label>
              <input
                  v-model="selectedDate"
                  type="date"
                  :min="todayIso"
                  class="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 disabled:opacity-50"
              />
            </div>

            <div>
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">الوقت</label>
              <input
                  v-model="selectedTime"
                  type="time"
                  class="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 disabled:opacity-50"
              />
            </div>

            <div>
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">مدة الاستشارة</label>
              <div class="grid grid-cols-2 gap-2">
                <button
                    v-for="opt in durationOptions"
                    :key="opt.minutes"
                    type="button"
                    @click="selectedDuration = opt.minutes"
                    class="text-sm py-2 rounded-xl border transition-colors disabled:opacity-50"
                    :class="selectedDuration === opt.minutes
                    ? 'bg-emerald-800 text-white border-emerald-800'
                    : 'bg-white text-gray-700 border-gray-200 hover:border-emerald-300'"
                >
                  {{ opt.label }}
                </button>
              </div>
            </div>

            <!-- 🟢 PAYMENT CHANNEL SELECTION -->
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

            <!-- Dynamic Input for Mobile Wallet -->
            <div v-if="selectedChannel === 'MobileWallet'">
              <label class="block text-xs font-bold text-emerald-900 mb-1.5">رقم المحفظة الإلكترونية</label>
              <input
                  v-model="walletPhoneNumber"
                  type="tel"
                  placeholder="010XXXXXXXX"
                  class="w-full bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <div class="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 flex items-center justify-between">
              <span class="text-xs font-medium text-amber-900">التكلفة المقدرة</span>
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
                :disabled="isSubmitting"
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
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
</style>
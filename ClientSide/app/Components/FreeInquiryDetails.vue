<template>
  <div class="flex-1 flex flex-col bg-gray-50 overflow-y-auto p-6 space-y-6" dir="rtl">
    <!-- Header: Inquiry Info Card -->
    <div class="bg-white rounded-2xl p-5 border border-gray-200 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-gray-100 pb-3">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-full bg-amber-100 text-amber-800 flex items-center justify-center font-bold text-base">
            ?
          </div>
          <div>
            <h2 class="text-base font-bold text-gray-900">{{ inquiry.senderName || 'زائر' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(inquiry.createdAt) }}</span>
          </div>
        </div>
        <span
            class="text-xs px-2.5 py-1 rounded-full font-medium"
            :class="inquiry.isRepliedTo ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'"
        >
          {{ inquiry.isRepliedTo ? 'تم الرد' : 'بانتظار الرد' }}
        </span>
      </div>

      <!-- Metadata Details -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
        <div class="flex items-center gap-2 text-gray-600 bg-gray-50 p-2.5 rounded-lg">
          <span class="font-semibold text-gray-900">البريد الإلكتروني:</span>
          <span>{{ inquiry.senderEmail || 'غير متوفر' }}</span>
        </div>
        <div class="flex items-center gap-2 text-gray-600 bg-gray-50 p-2.5 rounded-lg">
          <span class="font-semibold text-gray-900">رقم الهاتف:</span>
          <span dir="ltr">{{ inquiry.senderPhone || 'غير متوفر' }}</span>
        </div>
      </div>
    </div>

    <!-- Inquiry Body -->
    <div class="bg-white rounded-2xl p-5 border border-gray-200 shadow-sm flex-1 flex flex-col">
      <h3 class="text-xs font-bold text-gray-400 uppercase tracking-wider mb-3">تفاصيل الاستفسار</h3>
      <div class="bg-amber-50/50 border border-amber-100 text-gray-800 p-4 rounded-xl text-sm leading-relaxed flex-1">
        {{ inquiry.content }}
      </div>
    </div>

    <!-- Response Editor -->
    <div class="bg-white rounded-2xl p-5 border border-gray-200 shadow-sm space-y-3">
      <h3 class="text-xs font-bold text-gray-400 uppercase tracking-wider">الرد على الاستفسار</h3>
      <textarea
          v-model="replyText"
          rows="4"
          placeholder="اكتب ردك المستفيض هنا ليتم إرساله للعميل..."
          class="w-full bg-gray-50 border border-gray-200 rounded-xl p-3 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 resize-none"
          :disabled="sending"
      ></textarea>

      <div class="flex justify-end gap-3">
        <button
            @click="submitReply"
            :disabled="!replyText.trim() || sending"
            class="px-6 py-2.5 bg-emerald-600 text-white rounded-xl text-sm font-medium hover:bg-emerald-700 disabled:opacity-50 transition-colors shadow-sm flex items-center gap-2"
        >
          <span v-if="sending">جاري الإرسال...</span>
          <span v-else>إرسال الرد</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

export interface FreeConsultation {
  id: number;
  senderName: string;
  senderPhone: string;
  senderEmail: string;
  content: string;
  createdAt: string;
  isRepliedTo: boolean;
}

const props = defineProps<{
  inquiry: FreeConsultation;
}>();

const emit = defineEmits<{
  (e: 'replySent', inquiryId: number, content: string): void;
}>();

const replyText = ref('');
const sending = ref(false);

const submitReply = async () => {
  if (!replyText.value.trim()) return;
  sending.value = true;

  try {
    const config = useRuntimeConfig();
    const token = useCookie('auth_token').value;

    await $fetch(`${config.public.apiBase}consultations/free-messages/${props.inquiry.id}/reply`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: { content: replyText.value }
    });

    emit('replySent', props.inquiry.id, replyText.value);
    replyText.value = '';
  } catch (err) {
    console.error('Failed to submit reply:', err);
  } finally {
    sending.value = false;
  }
};

const formatDate = (dateString: string) => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString('ar-EG', {
    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
  });
};
</script>
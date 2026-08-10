<script setup lang="ts">
import { ref } from 'vue';

export interface FreeInquiry {
  id: number;
  senderName: string;
  senderPhone: string;
  senderEmail: string;
  content: string;
  createdAt: string;
  isRepliedTo: boolean;
}

const props = defineProps<{
  inquiry: FreeInquiry;
}>();

const emit = defineEmits<{
  replySent: [inquiryId: number, content: string];
}>();

const replyText = ref('');
const sending = ref(false);

const submitReply = async () => {
  if (!replyText.value.trim()) return;
  sending.value = true;

  try {
    const config = useRuntimeConfig();
    const token = useCookie('auth_token').value;

    await $fetch(`${config.public.apiBase}/consultations/free-messages/${props.inquiry.id}/reply`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: { content: replyText.value }
    });

    emit('replySent', props.inquiry.id, replyText.value);
    replyText.value = '';
  } catch (err) {
    console.error('Failed to submit reply:', err);
    alert('فشل إرسال الرد');
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

<template>
  <div class="flex h-full flex-col overflow-y-auto bg-gray-50 p-6 space-y-6" dir="rtl">
    <!-- Header: Inquiry Info Card -->
    <div class="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-gray-100 pb-3">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-full bg-amber-100 text-base font-bold text-amber-800">
            ?
          </div>
          <div>
            <h2 class="text-base font-bold text-gray-900">{{ inquiry.senderName || 'زائر' }}</h2>
            <span class="text-xs text-gray-400">{{ formatDate(inquiry.createdAt) }}</span>
          </div>
        </div>
        <span
            class="rounded-full px-2.5 py-1 text-xs font-medium"
            :class="inquiry.isRepliedTo ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'"
        >
                    {{ inquiry.isRepliedTo ? 'تم الرد' : 'بانتظار الرد' }}
                </span>
      </div>

      <div class="grid grid-cols-1 gap-3 text-xs md:grid-cols-2">
        <div class="flex items-center gap-2 rounded-lg bg-gray-50 p-2.5 text-gray-600">
          <span class="font-semibold text-gray-900">البريد الإلكتروني:</span>
          <span>{{ inquiry.senderEmail || 'غير متوفر' }}</span>
        </div>
        <div class="flex items-center gap-2 rounded-lg bg-gray-50 p-2.5 text-gray-600">
          <span class="font-semibold text-gray-900">رقم الهاتف:</span>
          <span dir="ltr">{{ inquiry.senderPhone || 'غير متوفر' }}</span>
        </div>
      </div>
    </div>

    <!-- Inquiry Body -->
    <div class="flex flex-1 flex-col rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
      <h3 class="mb-3 text-xs font-bold uppercase tracking-wider text-gray-400">تفاصيل الاستفسار</h3>
      <div class="flex-1 rounded-xl border border-amber-100 bg-amber-50/50 p-4 text-sm leading-relaxed text-gray-800">
        {{ inquiry.content }}
      </div>
    </div>

    <!-- Response Editor -->
    <div class="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm space-y-3">
      <h3 class="text-xs font-bold uppercase tracking-wider text-gray-400">الرد على الاستفسار</h3>
      <textarea
          v-model="replyText"
          rows="4"
          placeholder="اكتب ردك المستفيض هنا ليتم إرساله للعميل..."
          class="w-full resize-none rounded-xl border border-gray-200 bg-gray-50 p-3 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
          :disabled="sending"
      ></textarea>

      <div class="flex justify-end gap-3">
        <button
            @click="submitReply"
            :disabled="!replyText.trim() || sending"
            class="flex items-center gap-2 rounded-xl bg-emerald-600 px-6 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
        >
          <span v-if="sending">جاري الإرسال...</span>
          <span v-else>إرسال الرد</span>
        </button>
      </div>
    </div>
  </div>
</template>
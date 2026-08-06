<template>
  <div class="flex-1 flex flex-col bg-[#e9edef] relative" style="background-image: url('https://www.transparenttextures.com/patterns/cubes.png');" dir="rtl">
    <!-- Header -->
    <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 shadow-sm z-10">
      <div class="flex items-center gap-3">
        <img
            v-if="consultation.otherUserImageUrl"
            :src="consultation.otherUserImageUrl"
            class="w-10 h-10 rounded-full object-cover border border-gray-200"
        />
        <div v-else class="w-10 h-10 rounded-full bg-emerald-200 flex items-center justify-center text-emerald-800 font-bold">
          {{ consultation.otherUserName.charAt(0) }}
        </div>
        <div>
          <h2 class="text-sm font-bold text-gray-900">{{ consultation.otherUserName }}</h2>
          <p class="text-xs text-gray-500">
            {{ consultation.otherUserRole === 'Lawyer' ? 'محامي' : 'عميل' }} • {{ getStatusText(consultation.status) }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <button
            v-if="canStartSession(consultation)"
            @click="$emit('startVideoCall', consultation.id)"
            class="px-4 py-1.5 bg-gradient-to-l from-amber-400 to-amber-500 text-white rounded-lg text-sm font-medium hover:from-amber-500 hover:to-amber-600 transition-all shadow-sm"
        >
          بدء الجلسة
        </button>
      </div>
    </header>

    <!-- Message Stream -->
    <div class="flex-1 overflow-y-auto p-4 space-y-4">
      <div class="flex justify-center my-2">
        <span class="bg-emerald-100/80 text-emerald-800 text-[11px] font-medium px-3 py-1 rounded-lg shadow-sm">
          {{ formatDate(consultation.scheduledAt) }}
        </span>
      </div>

      <div v-if="consultation.lastMessageContent" class="flex justify-start">
        <div class="max-w-[75%] bg-white text-gray-800 rounded-2xl rounded-tr-sm px-4 py-2 shadow-sm relative">
          <p class="text-sm pb-3">{{ consultation.lastMessageContent }}</p>
          <span class="text-[10px] text-gray-400 absolute bottom-1 left-3">{{ formatTime(consultation.lastMessageDate) }}</span>
        </div>
      </div>
    </div>

    <!-- Input Footer -->
    <footer class="bg-white p-3 flex items-end gap-2 z-10 border-t border-gray-100">
      <textarea
          v-model="messageText"
          rows="1"
          placeholder="اكتب رسالة..."
          class="flex-1 bg-gray-100 rounded-2xl px-4 py-3 text-sm focus:outline-none focus:ring-1 focus:ring-emerald-500 resize-none max-h-32"
          @keydown.enter.prevent="handleSend"
      ></textarea>

      <button
          @click="handleSend"
          :disabled="!messageText.trim()"
          class="p-3 bg-emerald-600 text-white rounded-full hover:bg-emerald-700 disabled:opacity-50 transition-colors shadow-sm transform rtl:-scale-x-100"
      >
        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
        </svg>
      </button>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

export interface Consultation {
  id: number;
  otherUserName: string;
  otherUserImageUrl: string | null;
  otherUserRole: string;
  status: string;
  scheduledAt: string;
  durationMinutes: number;
  lastMessageContent: string | null;
  lastMessageDate: string | null;
}

const props = defineProps<{
  consultation: Consultation;
  isAuthenticated: boolean;
}>();

const emit = defineEmits<{
  (e: 'sendMessage', content: string): void;
  (e: 'startVideoCall', consultationId: number): void;
}>();

const messageText = ref('');

const handleSend = () => {
  if (!messageText.value.trim()) return;
  emit('sendMessage', messageText.value);
  messageText.value = '';
};

const canStartSession = (c: Consultation): boolean => {
  if (!props.isAuthenticated) return false;
  if (!['Confirmed', 'InProgress'].includes(c.status)) return false;

  const scheduledTime = new Date(c.scheduledAt);
  const endTime = new Date(scheduledTime.getTime() + c.durationMinutes * 60000);
  const now = new Date();
  const windowStart = new Date(scheduledTime.getTime() - 15 * 60000);

  return now >= windowStart && now <= endTime;
};

const formatDate = (dateStr: string | null) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('ar-EG', { year: 'numeric', month: 'long', day: 'numeric' });
};

const formatTime = (dateStr: string | null) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
};

const getStatusText = (status: string) => {
  const texts: Record<string, string> = {
    'Pending': 'قيد الانتظار',
    'Confirmed': 'مؤكد',
    'InProgress': 'جاري',
    'Completed': 'مكتمل',
    'Cancelled': 'ملغي'
  };
  return texts[status] || status;
};
</script>
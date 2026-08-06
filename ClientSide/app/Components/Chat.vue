<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, computed } from 'vue';
import { useAuthStore } from "~/stores/Auth";
import { useChatStore } from "~/stores/Chat";

const props = defineProps<{
  consultationId: number;
}>();

const chatStore = useChatStore();
const authStore = useAuthStore();

const newMessage = ref('');
const isSending = ref(false);
const messagesContainer = ref<HTMLElement | null>(null);

interface ConsultationDetails {
  consultationId: number;
  otherUserName: string;
  otherUserImageUrl: string | null;
  otherUserRole: string;
  status: string;
  scheduledAt: string;
  durationMinutes: number;
  lastMessageContent: string | null;
  lastMessageDate: string | null;
  lastMessageSenderId: number | null;
  isOnline: boolean;
}

const consultationDetails = ref<ConsultationDetails | null>(null);

const currentUserId = computed(() => authStore.user?.userId || 0);
const token = computed(() => authStore.token || '');

const scrollToBottom = async () => {
  await nextTick();
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
};

if (import.meta.client) {
  window.addEventListener('scroll-chat-bottom', scrollToBottom);
}

onMounted(async () => {
  if (!import.meta.client) return;

  // 1. Fetch consultation details
  try {
    const config = useRuntimeConfig();
    const details = await $fetch<ConsultationDetails>(`${config.public.apiBase}consultations/${props.consultationId}/details`, {
      headers: {
        Authorization: `Bearer ${token.value}`
      }
    });
    consultationDetails.value = details;
  } catch (e) {
    console.error('Failed to load consultation details', e);
  }

  // 2. Connect to SignalR
  await chatStore.connect(props.consultationId, token.value, currentUserId.value);

  // 3. Fetch history once connected
  if (chatStore.connectionStatus === 'connected') {
    try {
      const history = await chatStore.fetchHistory(props.consultationId);
      chatStore.messages = history;
      await scrollToBottom();
    } catch (e) {
      console.error('Failed to load chat history', e);
    }
  }
});

onUnmounted(() => {
  chatStore.disconnect();
  if (import.meta.client) {
    window.removeEventListener('scroll-chat-bottom', scrollToBottom);
  }
});

const handleSend = async () => {
  const content = newMessage.value.trim();
  if (!content || isSending.value) return;

  isSending.value = true;
  try {
    await chatStore.sendMessage(props.consultationId, content);
    newMessage.value = '';
  } catch (error: any) {
    alert(error.message);
  } finally {
    isSending.value = false;
  }
};

const formatTime = (dateString: string) => {
  return new Date(dateString).toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit' });
};
</script>

<template>
  <!-- Added 'relative' to parent so the absolute overlay works correctly -->
  <div class="relative flex flex-col h-[600px] bg-white rounded-2xl shadow-sm border border-emerald-900/10 overflow-hidden">

    <!-- Chat Header -->
    <div class="p-4 border-b border-gray-100 flex items-center gap-3 bg-emerald-50/30">
      <div class="w-10 h-10 rounded-full bg-emerald-200 flex items-center justify-center text-emerald-800 font-bold">
        {{ consultationDetails?.otherUserName?.charAt(0) || 'م' }}
      </div>
      <div>
        <h3 class="font-bold text-emerald-950 text-sm">
          {{ consultationDetails?.otherUserName || 'جاري التحميل...' }}
        </h3>
        <span class="text-xs" :class="chatStore?.connectionStatus === 'connected' ? 'text-green-600' : 'text-gray-400'">
          {{ chatStore?.connectionStatus === 'connected' ? 'متصل الآن' : 'جاري الاتصال...' }}
        </span>
      </div>
    </div>

    <div
        ref="messagesContainer"
        class="flex-1 overflow-y-auto p-4 space-y-4 bg-gray-50/50"
    >
      <div v-if="chatStore?.connectionStatus === 'connecting'" class="flex justify-center py-8">
        <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-emerald-800"></div>
      </div>

      <div
          v-for="msg in chatStore?.messages || []"
          :key="msg.id"
          class="flex"
          :class="msg.senderId === currentUserId ? 'justify-end' : 'justify-start'"
      >
        <div
            class="max-w-[75%] px-4 py-3 rounded-2xl text-sm shadow-sm"
            :class="msg.senderId === currentUserId 
            ? 'bg-emerald-800 text-white rounded-br-none' 
            : 'bg-white text-gray-800 border border-gray-100 rounded-bl-none'"
        >
          <p class="whitespace-pre-wrap leading-relaxed">{{ msg.content }}</p>
          <span class="text-[10px] mt-1.5 block opacity-70 text-right">
            {{ formatTime(msg.createdAt) }}
          </span>
        </div>
      </div>
    </div>

    <!-- Input Area -->
    <div class="p-4 border-t border-gray-100 bg-white">
      <div class="flex gap-2 items-end">
        <textarea
            v-model="newMessage"
            @keydown.enter.exact.prevent="handleSend"
            :disabled="chatStore?.connectionStatus !== 'connected' || isSending"
            rows="1"
            placeholder="اكتب رسالتك هنا..."
            class="flex-1 bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 resize-none disabled:opacity-50"
            style="min-height: 48px; max-height: 120px;"
        ></textarea>

        <button
            @click="handleSend"
            :disabled="!newMessage.trim() || isSending || chatStore?.connectionStatus !== 'connected'"
            class="bg-emerald-800 text-white p-3 rounded-xl hover:bg-emerald-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
        >
          <svg v-if="!isSending" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 transform rotate-180" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10.894 2.553a1 1 0 00-1.788 0l-7 14a1 1 0 001.169 1.409l5-1.429A1 1 0 009 15.571V11a1 1 0 112 0v4.571a1 1 0 00.725.962l5 1.428a1 1 0 001.17-1.408l-7-14z" />
          </svg>
          <div v-else class="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
        </button>
      </div>

      <p class="text-[10px] text-gray-400 mt-2 text-center">
        اضغط على Enter للإرسال، أو Shift + Enter لسطر جديد
      </p>

      <div v-if="chatStore?.connectionStatus !== 'connected'" class="absolute inset-0 bg-white/80 backdrop-blur-sm flex items-center justify-center rounded-2xl z-10">
        <p class="text-emerald-800 font-medium">جاري إعادة الاتصال...</p>
      </div>
    </div>
  </div>
</template>
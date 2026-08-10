<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, computed, watch } from 'vue';
import { useAuthStore } from "~/stores/auth";
import { useChatStore } from "~/stores/Chat";

const props = defineProps<{
  consultationId: number;
}>();

const chatStore = useChatStore();
const authStore = useAuthStore();
const config = useRuntimeConfig();

const newMessage = ref('');
const isSending = ref(false);
const messagesContainer = ref<HTMLElement | null>(null);
const details = ref<any>(null);

const currentUserId = computed(() => Number(authStore.user?.userId ?? 0));
const token = computed(() => authStore.token || '');

const canChat = computed(() => {
  if (!details.value) return false;
  return ['Confirmed', 'InProgress'].includes(details.value.status);
});

const statusMessage = computed(() => {
  if (!details.value) return '';
  const status = details.value.status;
  if (status === 'Pending') return 'الدفع غير مكتمل — المحادثة تُفتح بعد تأكيد الحجز';
  if (status === 'Cancelled') return 'تم إلغاء الحجز';
  if (status === 'Completed') return 'انتهت الاستشارة';
  return '';
});

const scrollToBottom = async () => {
  await nextTick();
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
};

watch(() => chatStore.messages.length, scrollToBottom);

onMounted(async () => {
  if (!token.value) return;

  try {
    details.value = await $fetch(`${config.public.apiBase}/consultations/${props.consultationId}/details`, {
      headers: { Authorization: `Bearer ${token.value}` }
    });
  } catch (e) {
    console.error('Failed to load consultation details', e);
  }

  await chatStore.connect(props.consultationId, token.value, currentUserId.value);

  if (chatStore.connectionStatus === 'connected') {
    const history = await chatStore.fetchHistory(props.consultationId);
    chatStore.messages = history;
    await scrollToBottom();
  }
});

onUnmounted(() => {
  chatStore.disconnect();
});

const handleSend = async () => {
  const content = newMessage.value.trim();
  if (!content || isSending.value || !canChat.value) return;

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
  <div class="relative flex h-full flex-col bg-white">
    <!-- Header -->
    <div class="border-b border-gray-100 bg-emerald-50/30 p-4">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-full bg-emerald-200 flex items-center justify-center text-emerald-800 font-bold">
          {{ details?.otherUserName?.charAt(0) || 'م' }}
        </div>
        <div class="flex-1">
          <h3 class="font-bold text-emerald-950 text-sm">
            {{ details?.otherUserName || 'جاري التحميل...' }}
          </h3>
          <span
              class="text-xs"
              :class="chatStore.connectionStatus === 'connected' ? 'text-green-600' : 'text-gray-400'"
          >
                        {{ chatStore.connectionStatus === 'connected' ? 'متصل الآن' : 'جاري الاتصال...' }}
                    </span>
        </div>
      </div>
    </div>

    <!-- Status Banner -->
    <div
        v-if="!canChat && statusMessage"
        class="border-b border-red-200 bg-red-50 px-4 py-3 text-center text-sm text-red-800"
    >
      ⏳ {{ statusMessage }}
    </div>

    <!-- Messages -->
    <div
        ref="messagesContainer"
        class="flex-1 overflow-y-auto bg-gray-50/50 p-4 space-y-4"
    >
      <div v-if="chatStore.connectionStatus === 'connecting'" class="flex justify-center py-8">
        <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-emerald-800"></div>
      </div>

      <div
          v-for="msg in chatStore.messages"
          :key="msg.id"
          class="flex"
          :class="msg.senderId === currentUserId ? 'justify-end' : 'justify-start'"
      >
        <div
            class="max-w-[75%] rounded-2xl px-4 py-3 text-sm shadow-sm"
            :class="msg.senderId === currentUserId 
                        ? 'bg-emerald-800 text-white rounded-br-none' 
                        : 'bg-white text-gray-800 border border-gray-100 rounded-bl-none'"
        >
          <p class="whitespace-pre-wrap leading-relaxed">{{ msg.content }}</p>
          <span class="mt-1.5 block text-[10px] opacity-70 text-right">
                        {{ formatTime(msg.createdAt) }}
                    </span>
        </div>
      </div>
    </div>

    <!-- Input Area -->
    <div class="border-t border-gray-100 bg-white p-4">
      <template v-if="canChat">
        <div class="flex gap-2 items-end">
                    <textarea
                        v-model="newMessage"
                        @keydown.enter.exact.prevent="handleSend"
                        :disabled="isSending"
                        rows="1"
                        placeholder="اكتب رسالتك هنا..."
                        class="flex-1 resize-none rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 disabled:opacity-50"
                        style="min-height: 48px; max-height: 120px;"
                    ></textarea>

          <button
              @click="handleSend"
              :disabled="!newMessage.trim() || isSending"
              class="shrink-0 rounded-xl bg-emerald-800 p-3 text-white transition-colors hover:bg-emerald-900 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <svg v-if="!isSending" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 transform rotate-180" viewBox="0 0 20 20" fill="currentColor">
              <path d="M10.894 2.553a1 1 0 00-1.788 0l-7 14a1 1 0 001.169 1.409l5-1.429A1 1 0 009 15.571V11a1 1 0 112 0v4.571a1 1 0 00.725.962l5 1.428a1 1 0 001.17-1.408l-7-14z" />
            </svg>
            <div v-else class="h-5 w-5 animate-spin rounded-full border-b-2 border-white"></div>
          </button>
        </div>
        <p class="mt-2 text-center text-[10px] text-gray-400">
          اضغط على Enter للإرسال، أو Shift + Enter لسطر جديد
        </p>
      </template>
      <div v-else class="border border-gray-200 rounded-xl p-3 text-center text-sm text-gray-500">
        🔒 الكتابة مقفلة حتى تأكيد الحجز
      </div>
    </div>

    <!-- Reconnect Overlay -->
    <div
        v-if="chatStore.connectionStatus === 'connecting'"
        class="absolute inset-0 flex items-center justify-center rounded-2xl bg-white/80 backdrop-blur-sm z-10"
    >
      <p class="text-emerald-800 font-medium">جاري إعادة الاتصال...</p>
    </div>
  </div>
</template>
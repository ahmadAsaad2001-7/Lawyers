<template>
  <div
      dir="rtl"
      class="flex h-[600px] flex-col rounded-2xl bg-gradient-to-b from-emerald-50 to-white shadow-lg ring-1 ring-emerald-100"
  >
    <!-- Header -->
    <div class="flex items-center justify-between border-b border-emerald-100 bg-gradient-to-l from-emerald-800 to-emerald-900 px-6 py-4 rounded-t-2xl">
      <div class="flex items-center gap-3">
        <div class="relative">
          <div class="flex h-10 w-10 items-center justify-center rounded-full bg-amber-400/20 ring-2 ring-amber-400/50">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-amber-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
          </div>
          <span
              v-if="isConnected"
              class="absolute bottom-0 left-0 h-3 w-3 rounded-full bg-green-400 ring-2 ring-emerald-900"
          ></span>
        </div>
        <div>
          <h3 class="font-bold text-amber-300" style="font-family: 'Amiri', serif;">
            محادثة الاستشارة
          </h3>
          <p class="text-xs text-emerald-200/70">
            {{ isConnected ? 'متصل' : isConnecting ? 'جاري الاتصال...' : 'غير متصل' }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button
            @click="loadMessages"
            :disabled="!isConnected"
            class="rounded-lg p-2 text-emerald-200 hover:bg-emerald-700/50 transition-colors disabled:opacity-50"
            title="تحديث المحادثة"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Connection Error -->
    <div
        v-if="connectionError"
        class="mx-4 mt-3 rounded-lg bg-red-50 border border-red-200 p-3 text-sm text-red-700"
    >
      <div class="flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <span>{{ connectionError }}</span>
        <button @click="startConnection" class="mr-auto text-red-600 underline hover:text-red-800">
          إعادة المحاولة
        </button>
      </div>
    </div>

    <!-- Messages Area -->
    <div
        ref="messagesContainer"
        class="flex-1 overflow-y-auto px-6 py-4 space-y-3"
        style="scroll-behavior: smooth;"
    >
      <!-- Empty State -->
      <div v-if="messages.length === 0 && !isConnecting" class="flex h-full flex-col items-center justify-center text-center">
        <div class="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
        </div>
        <p class="mt-3 text-sm text-gray-500" style="font-family: 'Amiri', serif;">
          لا توجد رسائل بعد. ابدأ المحادثة!
        </p>
      </div>

      <!-- Messages -->
      <div
          v-for="message in messages"
          :key="message.id"
          class="flex"
          :class="message.isMine ? 'justify-start' : 'justify-end'"
      >
        <div
            class="max-w-[75%] rounded-2xl px-4 py-2.5 shadow-sm"
            :class="message.isMine
            ? 'bg-gradient-to-l from-emerald-700 to-emerald-800 text-white rounded-br-sm'
            : 'bg-white text-gray-800 ring-1 ring-emerald-100 rounded-bl-sm'"
        >
          <p class="whitespace-pre-wrap break-words text-sm leading-relaxed">
            {{ message.content }}
          </p>
          <p
              class="mt-1 text-xs"
              :class="message.isMine ? 'text-emerald-200' : 'text-gray-400'"
          >
            {{ formatTime(message.createdAt) }}
          </p>
        </div>
      </div>

      <!-- Loading indicator -->
      <div v-if="isConnecting" class="flex justify-center py-4">
        <div class="flex items-center gap-2 text-emerald-700">
          <div class="h-2 w-2 animate-bounce rounded-full bg-emerald-500 [animation-delay:-0.3s]"></div>
          <div class="h-2 w-2 animate-bounce rounded-full bg-emerald-500 [animation-delay:-0.15s]"></div>
          <div class="h-2 w-2 animate-bounce rounded-full bg-emerald-500"></div>
        </div>
      </div>
    </div>

    <!-- Input Area -->
    <div class="border-t border-emerald-100 bg-white p-4 rounded-b-2xl">
      <div class="flex items-end gap-3">
        <textarea
            v-model="newMessage"
            @keydown.enter.exact.prevent="handleSend"
            @keydown.enter.shift.exact="newMessage += '\n'"
            :disabled="!isConnected"
            :placeholder="isConnected ? 'اكتب رسالتك هنا...' : 'في انتظار الاتصال...'"
            rows="1"
            class="flex-1 resize-none rounded-xl border border-emerald-200 bg-emerald-50/50 px-4 py-3 text-sm text-gray-800 placeholder-gray-400 focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50"
            style="font-family: 'Amiri', serif; max-height: 120px;"
        ></textarea>

        <button
            @click="handleSend"
            :disabled="!isConnected || !newMessage.trim()"
            class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-l from-amber-400 to-amber-500 text-white shadow-md hover:from-amber-500 hover:to-amber-600 hover:shadow-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed"
            title="إرسال"
        >
          <svg
              v-if="!isSending"
              xmlns="http://www.w3.org/2000/svg"
              class="h-5 w-5 rotate-180"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              stroke-width="2"
          >
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
          </svg>
          <svg
              v-else
              class="h-5 w-5 animate-spin"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
          >
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </button>
      </div>
      <p class="mt-2 text-xs text-gray-400 text-center">
        اضغط Enter للإرسال • Shift+Enter لسطر جديد
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import {useSignalRChat} from "~/composables/useSignalRChat";

const props = defineProps<{
  consultationId: number;
  currentUserId: number;
}>();

const newMessage = ref("");
const isSending = ref(false);
const messagesContainer = ref<HTMLDivElement | null>(null);

const {
  messages,
  isConnected,
  isConnecting,
  connectionError,
  startConnection,
  sendMessage,
} = useSignalRChat(props.consultationId, props.currentUserId);

// Auto-scroll to bottom when new messages arrive
watch(
    () => messages.value.length,
    () => {
      nextTick(() => {
        if (messagesContainer.value) {
          messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
        }
      });
    }
);

const handleSend = async () => {
  const content = newMessage.value.trim();
  if (!content || !isConnected.value || isSending.value) return;

  isSending.value = true;
  try {
    await sendMessage(content);
    newMessage.value = "";
  } catch (err: any) {
    console.error("Failed to send message:", err);
    // Optionally show error toast
  } finally {
    isSending.value = false;
  }
};

const loadMessages = async () => {
  if (!isConnected.value) return;
  await startConnection(); // Re-fetches recent messages
};

const formatTime = (dateString: string) => {
  if (!dateString) return "";
  const date = new Date(dateString);
  return date.toLocaleTimeString("ar-EG", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  });
};

// Start connection on mount
onMounted(() => {
  startConnection();
});
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');

/* Custom scrollbar for messages area */
.messages-container::-webkit-scrollbar {
  width: 6px;
}

.messages-container::-webkit-scrollbar-track {
  background: transparent;
}

.messages-container::-webkit-scrollbar-thumb {
  background: #d1fae5;
  border-radius: 3px;
}

.messages-container::-webkit-scrollbar-thumb:hover {
  background: #a7f3d0;
}
</style>

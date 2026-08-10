<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useAuthStore } from '~/stores/auth';
import ChatSidebar from '~/components/chat/ChatSidebar.vue';
import ChatThread from '~/components/chat/ChatThread.vue';
import FreeInquiryReply from '~/components/chat/FreeInquiryReply.vue';
import type { ChatSummary, FreeInquiry } from '~/stores/chat';

const authStore = useAuthStore();
const config = useRuntimeConfig();

const chats = ref<ChatSummary[]>([]);
const inquiries = ref<FreeInquiry[]>([]);
const isLoading = ref(false);

const activeChatId = ref<number | null>(null);
const activeInquiryId = ref<number | null>(null);
const isMobileView = ref(false);

const isLawyer = computed(() => authStore.user?.role === 'Lawyer');
const token = computed(() => authStore.token || '');

const activeInquiry = computed(() =>
    inquiries.value.find(i => i.id === activeInquiryId.value) ?? null
);

const selectChat = (id: number) => {
  activeChatId.value = id;
  activeInquiryId.value = null;

  // Mark as read
  const chat = chats.value.find(c => c.id === id);
  if (chat) chat.unreadCount = 0;
};

const selectInquiry = (id: number) => {
  activeInquiryId.value = id;
  activeChatId.value = null;
};

const handleReplySent = (inquiryId: number) => {
  const inquiry = inquiries.value.find(i => i.id === inquiryId);
  if (inquiry) {
    inquiry.isRepliedTo = true;
  }
};

const checkMobileView = () => {
  isMobileView.value = window.innerWidth < 768;
};

onMounted(async () => {
  if (!token.value) return;

  checkMobileView();
  window.addEventListener('resize', checkMobileView);

  isLoading.value = true;
  try {
    // Fetch chats
    chats.value = await $fetch<ChatSummary[]>(`${config.public.apiBase}/consultations/my-consultations`, {
      headers: { Authorization: `Bearer ${token.value}` },
    });

    // Fetch inquiries (lawyers only)
    if (isLawyer.value) {
      inquiries.value = await $fetch<FreeInquiry[]>(`${config.public.apiBase}/consultations/free-messages`, {
        headers: { Authorization: `Bearer ${token.value}` },
      });
    }
  } catch (e) {
    console.error('Failed to load conversations', e);
  } finally {
    isLoading.value = false;
  }
});

// Cleanup on unmount
import { onUnmounted } from 'vue';
onUnmounted(() => {
  window.removeEventListener('resize', checkMobileView);
});
</script>

<template>
  <div dir="rtl" class="mx-auto h-[calc(100vh-5rem)] max-w-7xl p-4">
    <div class="flex h-full w-full overflow-hidden rounded-2xl border border-emerald-900/10 bg-white shadow-sm">
      <!-- Sidebar -->
      <ChatSidebar
          v-if="!isMobileView || (!activeChatId && !activeInquiryId)"
          :chats="chats"
          :inquiries="inquiries"
          :is-lawyer="isLawyer"
          :active-chat-id="activeChatId"
          :active-inquiry-id="activeInquiryId"
          :is-loading="isLoading"
          @select-chat="selectChat"
          @select-inquiry="selectInquiry"
          @clear-selection="activeChatId = null; activeInquiryId = null"
      />

      <!-- Main Content -->
      <main class="flex flex-1 flex-col overflow-hidden">
        <!-- Mobile Back Button -->
        <button
            v-if="isMobileView && (activeChatId || activeInquiryId)"
            @click="activeChatId = null; activeInquiryId = null"
            class="flex items-center gap-2 p-3 text-emerald-800 hover:bg-emerald-50 md:hidden"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 transform rotate-180" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
          </svg>
          <span class="text-sm font-medium">رجوع</span>
        </button>

        <!-- Chat Thread -->
        <ChatThread
            v-if="activeChatId"
            :key="activeChatId"
            :consultation-id="activeChatId"
        />

        <!-- Free Inquiry Reply -->
        <FreeInquiryReply
            v-else-if="activeInquiry"
            :inquiry="activeInquiry"
            @reply-sent="handleReplySent"
        />

        <!-- Empty State -->
        <div v-else class="flex flex-1 items-center justify-center bg-gray-50">
          <div class="text-center">
            <div class="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-emerald-100">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-10 w-10 text-emerald-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
            </div>
            <p class="text-emerald-900 font-bold">اختر محادثة للبدء</p>
            <p class="text-sm text-gray-500 mt-1">اختر محادثة من القائمة الجانبية</p>
          </div>
        </div>
      </main>
    </div>
  </div>
</template>
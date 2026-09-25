<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';
import ChatThread from '~/components/chat/ChatThread.vue';
import ChatSideBar from '~/components/chat/ChatSideBar.vue';
import FreeInquiryReply from '~/components/chat/FreeInquiryReply.vue';

const authStore = useAuthStore();
const chatStore = useChatStore();

const isLawyer = computed(() => authStore.user?.role === 'Lawyer');

// Use store data directly
const chats = computed(() => chatStore.consultations);
const inquiries = computed(() => chatStore.inquiries);

// Reflects real connection/load state instead of a hardcoded false —
// the sidebar spinner now actually shows while the initial fetch is
// in flight, and clears once we have data (or a definitive error).
const isLoading = computed(
    () =>
        chatStore.connectionStatus === 'connecting' &&
        chatStore.consultations.length === 0 &&
        chatStore.inquiries.length === 0
);

const activeChatId = ref<number | null>(null);
const activeInquiryId = ref<number | null>(null);
const isMobileView = ref(false);

const activeInquiry = computed(
    () => chatStore.inquiries.find((i) => i.id === activeInquiryId.value) ?? null
);

const checkMobileView = () => {
  isMobileView.value = window.innerWidth < 768;
};

onMounted(() => {
  checkMobileView();
  window.addEventListener('resize', checkMobileView);
});

onUnmounted(() => {
  window.removeEventListener('resize', checkMobileView);
});

// Clears whichever panel is currently shown (chat or inquiry) — both
// the local selection *and* the store's notion of the active
// consultation. Previously only the local refs were cleared, so
// chatStore.activeConsultationId kept pointing at the last-opened
// chat: any ReceiveMessage for that chat would get pushed into
// chatStore.messages (per the store's handler, which routes to
// `messages` instead of bumping `unread` when the id matches
// activeConsultationId) even though ChatThread for it was unmounted,
// so the message was invisible until the user reopened that exact
// chat and openChat() re-fetched history.
const clearSelection = () => {
  activeChatId.value = null;
  activeInquiryId.value = null;
  void chatStore.clearActiveConsultation();
};

const selectChat = (id: number) => {
  activeChatId.value = id;
  activeInquiryId.value = null;
  chatStore.openChat(id); // this sets chatStore.activeConsultationId = id
};

const selectInquiry = (id: number) => {
  activeInquiryId.value = id;
  activeChatId.value = null;
  // Leaving the chat view entirely — make sure the store stops
  // treating any previous chat as "active" so its unread counter
  // resumes incrementing instead of silently swallowing messages.
  void chatStore.clearActiveConsultation();
};

const handleReplySent = (inquiryId: number) => {
  const inq = chatStore.inquiries.find((i) => i.id === inquiryId);
  if (inq) inq.isRepliedTo = true;
};
</script>

<template>
  <div dir="rtl" class="mx-auto h-[calc(100dvh-8rem)] max-w-7xl overflow-hidden p-2 sm:h-[calc(100vh-4rem)] sm:p-4">
    <div class="flex h-full w-full overflow-hidden rounded-2xl border border-emerald-900/10 bg-white shadow-sm">
      <!-- Sidebar: hidden on mobile once a chat/inquiry is open, so the
           thread gets the full screen instead of squeezing next to it -->
      <ChatSideBar
          v-if="!isMobileView || (!activeChatId && !activeInquiryId)"
          :chats="chats"
          :inquiries="inquiries"
          :is-lawyer="!!isLawyer"
          :active-chat-id="activeChatId"
          :active-inquiry-id="activeInquiryId"
          :is-loading="isLoading"
          :unread-counts="chatStore.unread"
          @select-chat="selectChat"
          @select-inquiry="selectInquiry"
          @clear-selection="clearSelection"
      />

      <main class="flex flex-1 flex-col overflow-hidden">
        <!-- Mobile back button -->
        <button
            v-if="isMobileView && (activeChatId || activeInquiryId)"
            @click="clearSelection"
            class="flex items-center gap-2 p-3 text-emerald-800 hover:bg-emerald-50 md:hidden"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 rotate-180" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
          </svg>
          <span class="text-sm font-medium">رجوع</span>
        </button>

        <ChatThread v-if="activeChatId" :key="activeChatId" :consultation-id="activeChatId" />

        <FreeInquiryReply
            v-else-if="activeInquiry"
            :key="activeInquiry.id"
            :inquiry="activeInquiry"
            @reply-sent="handleReplySent"
        />

        <div v-else class="flex flex-1 items-center justify-center">
          <p class="rounded-xl border border-emerald-900/20 bg-white px-6 py-3 text-sm font-bold text-emerald-900 shadow-sm">
            اختر محادثة للبدء
          </p>
        </div>
      </main>
    </div>
  </div>
</template>
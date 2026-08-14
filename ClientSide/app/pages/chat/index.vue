<script setup lang="ts">
import { computed, ref } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';
import ChatThread from '~/components/chat/ChatThread.vue';
import ChatSideBar from '~/components/chat/ChatSideBar.vue';
import FreeInquiryReply from '~/components/chat/FreeInquiryReply.vue';
import type { ChatSummary, FreeInquiry } from '~/types/chat';

const authStore = useAuthStore();
const chatStore = useChatStore();

const isLawyer = computed(() => authStore.user?.role === 'Lawyer');

// Use store data directly
const chats = computed(() => chatStore.consultations);
const inquiries = computed(() => chatStore.inquiries);
const isLoading = ref(false); // optionally bind to store.connectionStatus

const activeChatId = ref<number | null>(null);
const activeInquiryId = ref<number | null>(null);
const activeInquiry = computed(() => chatStore.inquiries.find(i => i.id === activeInquiryId.value) ?? null);

const selectChat = (id: number) => {
  activeChatId.value = id;
  activeInquiryId.value = null;
  chatStore.openChat(id);
};

const selectInquiry = (id: number) => {
  activeInquiryId.value = id;
  activeChatId.value = null;
};

const handleReplySent = (inquiryId: number) => {
  const inq = chatStore.inquiries.find(i => i.id === inquiryId);
  if (inq) inq.isRepliedTo = true;
};
</script>

<template>
  <div dir="rtl" class="mx-auto h-[calc(100vh-4rem)] max-w-7xl p-4">
    <div class="flex h-full w-full overflow-hidden rounded-2xl border border-emerald-900/10 bg-white shadow-sm">
      <!-- ✅ Pass live unread counts from the store to the sidebar -->
      <ChatSideBar
          :chats="chats"
          :inquiries="inquiries"
          :is-lawyer="!!isLawyer"
          :active-chat-id="activeChatId"
          :active-inquiry-id="activeInquiryId"
          :is-loading="isLoading"
          :unread-counts="chatStore.unread"
          @select-chat="selectChat"
          @select-inquiry="selectInquiry"
      />

      <main class="flex flex-1 flex-col overflow-hidden">
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

    <!-- ✅ Global CallOverlay rendered once at page level -->

  </div>
</template>

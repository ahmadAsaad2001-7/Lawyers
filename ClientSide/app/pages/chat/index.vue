<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';
import ChatThread from '~/components/chat/ChatThread.vue';
import ChatSideBar from '~/components/chat/ChatSideBar.vue';
import FreeInquiryReply from '~/components/chat/FreeInquiryReply.vue';
import CallOverlay from '~/components/chat/CallOverlay.vue';
import type { ChatSummary, FreeInquiry } from '~/types/chat';

const authStore = useAuthStore();
const chatStore = useChatStore();
const config = useRuntimeConfig();

const token = computed(() => authStore.token || '');
const isLawyer = computed(() => authStore.user?.role === 'Lawyer');

const chats = ref<ChatSummary[]>([]);
const inquiries = ref<FreeInquiry[]>([]);
const isLoading = ref(false);

const activeChatId = ref<number | null>(null);
const activeInquiryId = ref<number | null>(null);
const activeInquiry = computed(() => inquiries.value.find(i => i.id === activeInquiryId.value) ?? null);

// ✅ Update to use store-level openChat instead of just setting the ID
const selectChat = (id: number) => {
  activeChatId.value = id;
  activeInquiryId.value = null;
  chatStore.openChat(id); // This sets active group, clears unread, loads history
};

const selectInquiry = (id: number) => {
  activeInquiryId.value = id;
  activeChatId.value = null;
};

const handleReplySent = (inquiryId: number) => {
  const inq = inquiries.value.find(i => i.id === inquiryId);
  if (inq) inq.isRepliedTo = true;
};

onMounted(async () => {
  // Wait for the server to validate the persisted token before treating this
  // page as authenticated. This avoids loading a stale session as a real user.
  await authStore.initAuth();
  if (!authStore.isAuthenticated) {
    navigateTo('/auth/login');
    return;
  }

  isLoading.value = true;
  try {
    const base = config.public.apiBase as string;
    const headers = { Authorization: `Bearer ${token.value}` };

    const [myChats, myInquiries] = await Promise.all([
      $fetch<ChatSummary[]>(`${base}/consultations/my-consultations`, { headers }),
      isLawyer.value
          ? $fetch<FreeInquiry[]>(`${base}/consultations/free-messages`, { headers })
          : Promise.resolve<FreeInquiry[]>([]),
    ]);

    chats.value = myChats;
    inquiries.value = myInquiries;

    // ✅ Seed unread badges from backend, then connect + join ALL consultation groups
    const unreadMap: Record<number, number> = {};
    myChats.forEach(c => {
      if (c.unreadCount && c.unreadCount > 0) {
        unreadMap[c.id] = c.unreadCount;
      }
    });
    chatStore.seedUnread(unreadMap);

    // ✅ Connect once to SignalR and join all consultation groups
    const consultationIds = myChats.map(c => c.id);
    await chatStore.connect(token.value, consultationIds);

  } catch (e) {
    console.error('Failed to load conversations', e);
  } finally {
    isLoading.value = false;
  }
});

// ✅ Cleanup connection when leaving the page
onUnmounted(() => {
  chatStore.disconnect();
});
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
    <CallOverlay />
  </div>
</template>

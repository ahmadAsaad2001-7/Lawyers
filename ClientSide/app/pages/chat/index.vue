<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from '~/stores/auth';
// ✅ Identifiers match the file names EXACTLY (ChatSideBar, not ChatSidebar)
import ChatThread from '~/components/chat/ChatThread.vue';
import ChatSideBar from '~/components/chat/ChatSideBar.vue';
import FreeInquiryReply from '~/components/chat/FreeInquiryReply.vue';
import type { ChatSummary, FreeInquiry } from '~/types/chat';

const authStore = useAuthStore();
const config = useRuntimeConfig();

const token = computed(() => authStore.token || '');
const isLawyer = computed(() => authStore.user?.role === 'Lawyer');

const chats = ref<ChatSummary[]>([]);
const inquiries = ref<FreeInquiry[]>([]);
const isLoading = ref(false);

const activeChatId = ref<number | null>(null);
const activeInquiryId = ref<number | null>(null);
const activeInquiry = computed(() => inquiries.value.find(i => i.id === activeInquiryId.value) ?? null);

const selectChat = (id: number) => { activeChatId.value = id; activeInquiryId.value = null; };
const selectInquiry = (id: number) => { activeInquiryId.value = id; activeChatId.value = null; };

const handleReplySent = (inquiryId: number) => {
  const inq = inquiries.value.find(i => i.id === inquiryId);
  if (inq) inq.isRepliedTo = true;
};

onMounted(async () => {
  // ✅ Auth guard
  if (!token.value) {
    navigateTo('/auth/login');
    return;
  }

  isLoading.value = true;
  try {
    const base = config.public.apiBase as string;
    const headers = { Authorization: `Bearer ${token.value}` };

    // ✅ REAL backend routes
    const [myChats, myInquiries] = await Promise.all([
      $fetch<ChatSummary[]>(`${base}/consultations/my-consultations`, { headers }),
      isLawyer.value
          ? $fetch<FreeInquiry[]>(`${base}/consultations/free-messages`, { headers })
          : Promise.resolve<FreeInquiry[]>([]),
    ]);

    chats.value = myChats.map(c => ({ ...c, unreadCount: c.unreadCount ?? 0 }));
    inquiries.value = myInquiries;
  } catch (e) {
    console.error('Failed to load conversations', e);
  } finally {
    isLoading.value = false;
  }
});
</script>

<template>
  <div dir="rtl" class="mx-auto h-[calc(100vh-4rem)] max-w-7xl p-4">
    <div class="flex h-full w-full overflow-hidden rounded-2xl border border-emerald-900/10 bg-white shadow-sm">
      <!-- ✅ Tag matches the import: ChatSideBar -->
      <ChatSideBar
          :chats="chats"
          :inquiries="inquiries"
          :is-lawyer="!!isLawyer"
          :active-chat-id="activeChatId"
          :active-inquiry-id="activeInquiryId"
          :is-loading="isLoading"
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
  </div>
</template>
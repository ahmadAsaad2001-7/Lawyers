<script setup lang="ts">
import { ref, computed } from 'vue';
import type { ChatSummary, FreeInquiry } from '~/types/chat';
const props = defineProps<{
  chats: ChatSummary[];
  inquiries: FreeInquiry[];
  isLawyer: boolean;
  activeChatId: number | null;
  activeInquiryId: number | null;
  isLoading: boolean;
  unreadCounts: Record<number, number>; 

}>();

const emit = defineEmits<{
  selectChat: [id: number];
  selectInquiry: [id: number];
  clearSelection: [];
}>();

const search = ref('');
const activeTab = ref<'chats' | 'inquiries'>('chats');

const filteredChats = computed(() => {
  if (!search.value.trim()) return props.chats;
  return props.chats.filter(c => c.otherUserName.includes(search.value));
});

const filteredInquiries = computed(() => {
  if (!search.value.trim()) return props.inquiries;
  return props.inquiries.filter(i =>
      i.senderName.includes(search.value) || i.senderEmail.includes(search.value)
  );
});

const totalUnread = computed(() =>
    Object.values(props.unreadCounts).reduce((sum, count) => sum + count, 0)
);

const unreadInquiries = computed(() =>
    props.inquiries.filter(i => !i.isRepliedTo).length
);

const formatWhen = (dateStr: string | null) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  const days = Math.floor(diff / (1000 * 60 * 60 * 24));

  if (days === 0) {
    return d.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
  } else if (days === 1) {
    return 'أمس';
  } else if (days < 7) {
    return d.toLocaleDateString('ar-EG', { weekday: 'short' });
  } else {
    return d.toLocaleDateString('ar-EG', { month: 'short', day: 'numeric' });
  }
};
</script>

<template>
  <aside class="flex w-full flex-col border-l border-gray-100 bg-white md:w-80 lg:w-96">
    <!-- Search -->
    <div class="border-b border-gray-100 p-3">
      <input
          v-model="search"
          type="text"
          placeholder="ابحث عن محادثة..."
          class="w-full rounded-xl border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500"
      />
    </div>

    <!-- Tabs -->
    <div class="flex border-b border-gray-100 text-sm font-medium">
      <button
          class="flex flex-1 items-center justify-center gap-2 py-3 transition-colors"
          :class="activeTab === 'chats' 
                    ? 'border-b-2 border-emerald-800 text-emerald-900' 
                    : 'text-gray-500 hover:text-gray-700'"
          @click="activeTab = 'chats'"
      >
        المحادثات
        <span
            v-if="totalUnread > 0"
            class="flex h-5 min-w-[20px] items-center justify-center rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white"
        >
                    {{ totalUnread }}
                </span>
      </button>
      <button
          v-if="isLawyer"
          class="flex flex-1 items-center justify-center gap-2 py-3 transition-colors"
          :class="activeTab === 'inquiries' 
                    ? 'border-b-2 border-amber-500 text-amber-600' 
                    : 'text-gray-500 hover:text-gray-700'"
          @click="activeTab = 'inquiries'"
      >
        الاستشارات
        <span
            v-if="unreadInquiries > 0"
            class="flex h-5 min-w-[20px] items-center justify-center rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white"
        >
                    {{ unreadInquiries }}
                </span>
      </button>
    </div>

    <!-- List -->
    <div class="flex-1 overflow-y-auto">
      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center py-10">
        <div class="h-6 w-6 animate-spin rounded-full border-b-2 border-emerald-800"></div>
      </div>

      <!-- Chats -->
      <template v-else-if="activeTab === 'chats'">
        <button
            v-for="chat in filteredChats"
            :key="chat.id"
            class="flex w-full items-center gap-3 border-b border-gray-50 px-4 py-3 text-right transition-colors hover:bg-emerald-50/50"
            :class="activeChatId === chat.id ? 'bg-emerald-50' : ''"
            @click="emit('selectChat', chat.id)"
        >
          <!-- Avatar -->
          <div class="relative shrink-0">
            <div class="h-12 w-12 overflow-hidden rounded-full border border-gray-200 bg-emerald-100">
              <img
                  v-if="chat.otherUserImageUrl"
                  :src="chat.otherUserImageUrl"
                  class="h-full w-full object-cover"
                  alt=""
              />
              <span v-else class="flex h-full w-full items-center justify-center font-bold text-emerald-800">
                                {{ chat.otherUserName.charAt(0) }}
                            </span>
            </div>
            <span
                v-if="chat.isOnline"
                class="absolute bottom-0 start-0 h-3 w-3 rounded-full border-2 border-white bg-green-500"
            ></span>
          </div>

          <!-- Info -->
          <div class="min-w-0 flex-1">
            <div class="mb-0.5 flex items-center justify-between">
              <h3 class="truncate text-sm font-bold text-emerald-950">{{ chat.otherUserName }}</h3>
              <span class="shrink-0 text-[10px] text-gray-400">{{ formatWhen(chat.lastMessageDate) }}</span>
            </div>
            <div class="flex items-center justify-between">
              <p class="truncate text-xs text-gray-500">{{ chat.lastMessageContent || 'لا توجد رسائل بعد' }}</p>
              <span
                  v-if="(props.unreadCounts[chat.id] ?? 0) > 0"
                  class="ms-2 flex h-5 min-w-[20px] shrink-0 items-center justify-center rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white"
              >
        {{ props.unreadCounts[chat.id] }}
    </span>
            </div>
          </div>
        </button>
        <p v-if="!filteredChats.length" class="p-6 text-center text-sm text-gray-500">لا توجد محادثات</p>
      </template>

      <!-- Inquiries (Lawyer only) -->
      <template v-else>
        <button
            v-for="inquiry in filteredInquiries"
            :key="inquiry.id"
            class="flex w-full items-center gap-3 border-b border-gray-50 px-4 py-3 text-right transition-colors hover:bg-amber-50/50"
            :class="activeInquiryId === inquiry.id ? 'bg-amber-50' : ''"
            @click="emit('selectInquiry', inquiry.id)"
        >
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-amber-100 font-bold text-amber-700">
            {{ inquiry.senderName.charAt(0) }}
          </div>
          <div class="min-w-0 flex-1">
            <div class="mb-0.5 flex items-center justify-between">
              <h3 class="truncate text-sm font-bold text-emerald-950">{{ inquiry.senderName }}</h3>
              <span class="shrink-0 text-[10px] text-gray-400">{{ formatWhen(inquiry.createdAt) }}</span>
            </div>
            <div class="flex items-center justify-between">
              <p class="truncate text-xs text-gray-500">{{ inquiry.content }}</p>
              <span
                  v-if="!inquiry.isRepliedTo"
                  class="ms-2 flex h-5 min-w-[20px] shrink-0 items-center justify-center rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white"
              >
                                !
                            </span>
              <span
                  v-else
                  class="ms-2 flex h-5 min-w-[20px] shrink-0 items-center justify-center rounded-full bg-emerald-500 px-1.5 text-[10px] font-bold text-white"
              >
                                ✓
                            </span>
            </div>
          </div>
        </button>
        <p v-if="!filteredInquiries.length" class="p-6 text-center text-sm text-gray-500">لا توجد استشارات مجانية</p>
      </template>
    </div>
  </aside>
</template>
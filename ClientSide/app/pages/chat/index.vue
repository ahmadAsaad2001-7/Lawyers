<template>
  <!-- 
    Changed h-screen to h-[calc(100vh-70px)] so it fits perfectly under your existing Nuxt navbar 
    without causing a double scrollbar or disappearing.
  -->
  <div class="flex bg-gray-50 overflow-hidden font-sans" style="height: calc(100vh - 70px);" dir="rtl">

    <!-- ================= SIDEBAR (Right Side) ================= -->
    <aside class="w-full md:w-96 bg-white border-l border-gray-200 flex flex-col shrink-0 relative">

      <!-- Top Header & Search -->
      <div class="p-4 border-b border-gray-100 flex flex-col gap-4">
        <!-- Search Bar -->
        <div class="relative">
          <input
              type="text"
              placeholder="بحث عن استشارة..."
              class="w-full bg-gray-100 text-sm text-gray-900 rounded-full pl-4 pr-10 py-2.5 focus:outline-none focus:ring-2 focus:ring-emerald-500/50"
          >
          <svg class="w-4 h-4 text-gray-400 absolute right-3.5 top-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
        </div>

        <!-- TABS: Sessions vs Anonymous -->
        <div class="flex bg-gray-100 rounded-xl p-1 relative">
          <button
              @click="activeTab = 'sessions'"
              class="flex-1 py-1.5 text-sm font-medium rounded-lg transition-all z-10"
              :class="activeTab === 'sessions' ? 'bg-white text-emerald-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'"
          >
            استشاراتي (الجلسات)
          </button>
          <button
              @click="activeTab = 'anonymous'"
              class="flex-1 py-1.5 text-sm font-medium rounded-lg transition-all z-10"
              :class="activeTab === 'anonymous' ? 'bg-white text-emerald-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'"
          >
            الاستفسارات المجانية
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="absolute inset-0 top-[120px] bg-white/80 z-20 flex justify-center items-center">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800"></div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="p-4 text-center text-sm text-red-600 bg-red-50 m-4 rounded-lg">
        {{ error }}
      </div>

      <!-- Contact List -->
      <div v-else class="flex-1 overflow-y-auto">

        <!-- SESSIONS TAB (Using Real API Data) -->
        <ul v-if="activeTab === 'sessions'">
          <div v-if="consultations.length === 0" class="p-8 text-center text-gray-500 text-sm">
            لا توجد استشارات حالياً.
          </div>
          <li v-for="chat in consultations" :key="chat.id" @click="selectChat(chat)"
              class="flex items-center gap-3 p-3 hover:bg-emerald-50 cursor-pointer border-b border-gray-50 transition-colors"
              :class="{ 'bg-emerald-100': activeChat?.id === chat.id }">
            <div class="relative">
              <img
                  v-if="chat.otherUserImageUrl"
                  :src="chat.otherUserImageUrl"
                  class="w-12 h-12 rounded-full object-cover border border-gray-200"
              />
              <div v-else class="w-12 h-12 rounded-full bg-emerald-200 flex items-center justify-center text-emerald-800 font-bold text-lg">
                {{ chat.otherUserName.charAt(0) }}
              </div>
              <!-- Status Indicator -->
              <span class="absolute bottom-0 right-0 w-3 h-3 border-2 border-white rounded-full"
                    :class="chat.status === 'InProgress' ? 'bg-green-500' : 'bg-gray-400'">
              </span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex justify-between items-baseline mb-1">
                <h3 class="text-sm font-semibold text-gray-900 truncate">{{ chat.otherUserName }}</h3>
                <span class="text-xs text-gray-500">{{ formatTime(chat.lastMessageDate) || formatTime(chat.scheduledAt) }}</span>
              </div>
              <p class="text-xs text-gray-500 truncate">
                <span v-if="chat.status === 'Pending'" class="text-amber-600 ml-1">(قيد الانتظار)</span>
                {{ chat.lastMessageContent || 'اضغط لبدء المحادثة...' }}
              </p>
            </div>
          </li>
        </ul>

        <!-- ANONYMOUS TAB (Placeholder for your new endpoint) -->
        <ul v-else>
          <div class="p-8 text-center text-gray-500 text-sm">
            جاري العمل على ربط الاستفسارات المجانية...
          </div>
        </ul>
      </div>
    </aside>

    <!-- ================= MAIN CHAT AREA ================= -->
    <main class="flex-1 flex flex-col bg-[#e9edef] relative" style="background-image: url('https://www.transparenttextures.com/patterns/cubes.png');">

      <!-- Empty State -->
      <div v-if="!activeChat" class="flex-1 flex items-center justify-center">
        <div class="bg-white/80 backdrop-blur-sm px-6 py-2 rounded-full shadow-sm text-sm text-gray-500 font-medium">
          اختر محادثة للبدء
        </div>
      </div>

      <template v-else>
        <!-- Chat Header -->
        <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 shadow-sm z-10">
          <!-- User Info -->
          <div class="flex items-center gap-3">
            <img
                v-if="activeChat.otherUserImageUrl"
                :src="activeChat.otherUserImageUrl"
                class="w-10 h-10 rounded-full object-cover"
            />
            <div v-else class="w-10 h-10 rounded-full bg-emerald-200 flex items-center justify-center text-emerald-800 font-bold">
              {{ activeChat.otherUserName.charAt(0) }}
            </div>
            <div>
              <h2 class="text-sm font-bold text-gray-900">{{ activeChat.otherUserName }}</h2>
              <p class="text-xs text-gray-500">
                {{ activeChat.otherUserRole === 'Lawyer' ? 'محامي' : 'عميل' }} • {{ getStatusText(activeChat.status) }}
              </p>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="flex items-center gap-4 text-gray-500">
            <!-- Start Session Button (from Snippet 2) -->
            <button
                v-if="canStartSession(activeChat)"
                @click="startVideoCall(activeChat.id)"
                class="px-4 py-1.5 bg-gradient-to-l from-amber-400 to-amber-500 text-white rounded-lg text-sm font-medium hover:from-amber-500 hover:to-amber-600 transition-all shadow-sm"
            >
              بدء الجلسة
            </button>
            <button class="hover:bg-gray-100 p-2 rounded-full transition-colors" title="تفاصيل">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z"/></svg>
            </button>
          </div>
        </header>

        <!-- Messages Feed -->
        <div class="flex-1 overflow-y-auto p-4 space-y-4">
          <!-- Mock Date Separator -->
          <div class="flex justify-center my-4">
            <span class="bg-emerald-100/80 text-emerald-800 text-[11px] font-medium px-3 py-1 rounded-lg shadow-sm">
              {{ formatDate(activeChat.scheduledAt) }}
            </span>
          </div>

          <!-- Last Message (Mocking the feed for now) -->
          <div v-if="activeChat.lastMessageContent" class="flex justify-start">
            <div class="max-w-[75%] bg-white text-gray-800 rounded-2xl rounded-tr-sm px-4 py-2 shadow-sm relative">
              <p class="text-sm pb-3">{{ activeChat.lastMessageContent }}</p>
              <span class="text-[10px] text-gray-400 absolute bottom-1 left-3">{{ formatTime(activeChat.lastMessageDate) }}</span>
            </div>
          </div>
        </div>

        <!-- Chat Input Footer -->
        <footer class="bg-white p-3 flex items-end gap-2 z-10">
          <button class="p-3 text-gray-400 hover:text-gray-600 transition-colors">
            <svg class="w-6 h-6 transform -rotate-45" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13"/></svg>
          </button>

          <textarea
              v-model="newMessage"
              rows="1"
              placeholder="اكتب رسالة..."
              class="flex-1 bg-gray-100 rounded-2xl px-4 py-3 text-sm focus:outline-none focus:ring-1 focus:ring-emerald-500 resize-none max-h-32"
          ></textarea>

          <button v-if="!newMessage.trim()" class="p-3 bg-emerald-600 text-white rounded-full hover:bg-emerald-700 transition-colors shadow-sm">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z"/></svg>
          </button>
          <button v-else @click="sendMessage" class="p-3 bg-amber-500 text-white rounded-full hover:bg-amber-600 transition-colors shadow-sm transform rtl:-scale-x-100">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/></svg>
          </button>
        </footer>
      </template>

    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from "~/stores/Auth";

// Define Interfaces
interface Consultation {
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

// Stores & State
const authStore = useAuthStore();
const activeTab = ref<'sessions' | 'anonymous'>('sessions');
const activeChat = ref<Consultation | null>(null);
const newMessage = ref('');

// API State
const consultations = ref<Consultation[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);

// Fetching Logic
const fetchConsultations = async () => {
  try {
    loading.value = true;
    error.value = null;

    const config = useRuntimeConfig();
    const { data } = await useFetch(`${config.public.apiBase}consultations/my-consultations`, {
      headers: {
        Authorization: `Bearer ${useCookie('auth_token').value}`
      }
    });

    consultations.value = data.value || [];
  } catch (err) {
    console.error('Failed to fetch consultations:', err);
    error.value = 'فشل تحميل الاستشارات. يرجى المحاولة لاحقاً.';
  } finally {
    loading.value = false;
  }
};

// Actions
const selectChat = (chat: Consultation) => {
  activeChat.value = chat;
  // TODO: Here you would load the individual chat messages via SignalR / API based on chat.id
};

const sendMessage = () => {
  if (!newMessage.value.trim() || !activeChat.value) return;
  // TODO: Send via SignalR hub invoking 'SendMessage'
  console.log(`Sending to ${activeChat.value.id}:`, newMessage.value);
  newMessage.value = '';
};

const canStartSession = (consultation: Consultation): boolean => {
  if (!authStore.isAuthenticated) return false;
  if (!['Confirmed', 'InProgress'].includes(consultation.status)) return false;

  const scheduledTime = new Date(consultation.scheduledAt);
  const endTime = new Date(scheduledTime.getTime() + consultation.durationMinutes * 60000);
  const now = new Date();
  const windowStart = new Date(scheduledTime.getTime() - 15 * 60000);

  return now >= windowStart && now <= endTime;
};

const startVideoCall = (consultationId: number) => {
  if (!authStore.isAuthenticated) {
    navigateTo('/auth/login');
    return;
  }
  navigateTo(`/consultation/${consultationId}/video`);
};

// Formatters
const formatDate = (dateString: string | null) => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString('ar-EG', {
    year: 'numeric', month: 'long', day: 'numeric'
  });
};

const formatTime = (dateString: string | null) => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleTimeString('ar-EG', {
    hour: '2-digit', minute: '2-digit'
  });
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

// Lifecycle
onMounted(() => {
  fetchConsultations();
});
</script>

<style scoped>
/* Custom scrollbar to match Telegram */
::-webkit-scrollbar {
  width: 6px;
}
::-webkit-scrollbar-track {
  background: transparent;
}
::-webkit-scrollbar-thumb {
  background: rgba(0,0,0,0.15);
  border-radius: 10px;
}
::-webkit-scrollbar-thumb:hover {
  background: rgba(0,0,0,0.3);
}
</style>
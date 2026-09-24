<template>
  <div class="h-screen flex flex-col bg-gray-50" dir="rtl">
    <!-- Header -->
    <header class="bg-white border-b border-emerald-100 px-6 py-4 flex items-center justify-between shadow-sm">
      <div class="flex items-center gap-4">
        <!-- Call Buttons -->
        <div v-if="canChat" class="flex gap-2">
          <button @click="startVideoCall" class="p-2 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
          </button>
          <button @click="startPhoneCall" class="p-2 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
            </svg>
          </button>
        </div>

        <!-- User Info -->
        <div class="flex items-center gap-3">
          <div class="relative">
            <img :src="consultation?.otherUserImageUrl || 'https://via.placeholder.com/40'" :alt="consultation?.otherUserName" class="w-10 h-10 rounded-full object-cover border-2 border-amber-400" />
            <span v-if="consultation?.isOnline" class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>
          </div>
          <div>
            <h3 class="font-bold text-emerald-900" style="font-family: 'Amiri', serif;">{{ consultation?.otherUserName }}</h3>
            <p class="text-xs text-gray-500">{{ consultation?.otherUserRole === 'Lawyer' ? 'محامي' : 'عميل' }}</p>
          </div>
        </div>
      </div>

      <div class="px-3 py-1 rounded-full text-sm font-medium" :class="getStatusColor(status)">
        {{ getStatusText(status) }}
      </div>
    </header>

    <div
        v-if="!canChat && statusMessage"
        class="border-b border-red-200 bg-red-50 px-6 py-3 text-center text-sm text-red-800"
    >
      ⏳ {{ statusMessage }}
    </div>

    <!-- Main Chat -->
    <main class="flex-1 flex overflow-hidden">
      <div class="flex-1 overflow-y-auto p-6 space-y-4" ref="messagesContainer">
        <div v-for="message in chatStore.messages" :key="message.id" class="flex" :class="message.senderId === currentUserId ? 'justify-start' : 'justify-end'">
          <div class="max-w-[70%] px-4 py-2 rounded-2xl shadow-sm" :class="message.senderId === currentUserId ? 'bg-gradient-to-l from-emerald-700 to-emerald-800 text-white rounded-br-sm' : 'bg-white text-gray-800 border border-emerald-100 rounded-bl-sm'">
            <p class="text-sm">{{ message.content }}</p>
            <p class="text-xs mt-1 opacity-70">{{ formatTime(message.createdAt) }}</p>
          </div>
        </div>
      </div>

      <aside class="w-80 bg-white border-r border-emerald-100 p-6 overflow-y-auto hidden lg:block">
        <h4 class="font-bold text-emerald-900 mb-4" style="font-family: 'Amiri', serif;">تفاصيل الاستشارة</h4>
        <div class="space-y-4 text-sm">
          <div class="flex justify-between"><span class="text-gray-500">التاريخ:</span><span class="font-medium">{{ formatDate(consultation?.scheduledAt) }}</span></div>
          <div class="flex justify-between"><span class="text-gray-500">المدة:</span><span class="font-medium">{{ consultation?.durationMinutes }} دقيقة</span></div>
          <div class="flex justify-between"><span class="text-gray-500">الحالة:</span><span class="font-medium" :class="getStatusColor(status)">{{ getStatusText(status) }}</span></div>
        </div>
      </aside>
    </main>

    <!-- Input -->
    <footer class="bg-white border-t border-emerald-100 px-6 py-4">
      <template v-if="canChat">
        <div class="flex items-center gap-3">
          <textarea v-model="newMessage" @keydown.enter.exact.prevent="handleSend" placeholder="اكتب رسالتك هنا..." rows="1" class="flex-1 resize-none rounded-xl border border-emerald-200 bg-emerald-50/50 px-4 py-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20" style="font-family: 'Amiri', serif;"></textarea>
          <button @click="handleSend" :disabled="!newMessage.trim() || !isConnected || isSending || !canChat" class="px-6 py-3 bg-gradient-to-l from-amber-400 to-amber-500 text-white rounded-xl font-medium hover:from-amber-500 hover:to-amber-600 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-md">إرسال</button>
        </div>
      </template>
      <div v-else class="border border-gray-200 rounded-xl p-3 text-center text-sm text-gray-500">
        🔒 الكتابة مقفلة حتى تأكيد الحجز
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { useAuthStore } from '~/stores/auth'
import { useChatStore } from '~/stores/Chat'

const route = useRoute()
const authStore = useAuthStore()
const chatStore = useChatStore()

const consultationId = computed(() => Number(route.params.id))
const newMessage = ref('')
const isSending = ref(false)
const messagesContainer = ref<HTMLElement | null>(null)

const currentUserId = computed(() => Number(authStore.user?.userId ?? 0))
const isConnected = computed(() => chatStore.connectionStatus === 'connected')
const isAdmin = computed(() => authStore.user?.role === 'Admin')

const consultation = computed(() =>
    chatStore.consultations.find((c) => c.id === consultationId.value)
)

const status = computed(() => consultation.value?.status)

const canChat = computed(() => {
  if (isAdmin.value) return true
  if (!status.value) return false
  return ['Confirmed', 'InProgress'].includes(status.value)
})

const statusMessage = computed(() => {
  if (isAdmin.value) return ''
  const current = status.value
  if (!current) return ''
  if (current === 'Pending') return 'الدفع غير مكتمل — المحادثة تُفتح بعد تأكيد الحجز'
  if (current === 'Cancelled') return 'تم إلغاء الحجز'
  if (current === 'Completed') return 'انتهت الاستشارة'
  return ''
})

const activateConsultation = async (id: number) => {
  if (!Number.isFinite(id) || id <= 0) return

  await chatStore.openChat(id)

  if (!consultation.value) {
    await chatStore.syncConsultation(id)
  }
}

const handleSend = async () => {
  const content = newMessage.value.trim()
  if (!content || isSending.value || !canChat.value) return

  isSending.value = true
  try {
    await chatStore.sendMessage(consultationId.value, content)
    newMessage.value = ''
  } catch (err) {
    console.error('Failed to send message:', err)
  } finally {
    isSending.value = false
  }
}

const formatTime = (dateString: string | null) => {
  if (!dateString) return ''
  return new Date(dateString).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' })
}

const formatDate = (dateString: string | null) => {
  if (!dateString) return ''
  return new Date(dateString).toLocaleDateString('ar-EG')
}

const getStatusColor = (currentStatus?: string) => {
  const colors: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Confirmed: 'bg-emerald-100 text-emerald-800',
    InProgress: 'bg-blue-100 text-blue-800',
    Completed: 'bg-gray-100 text-gray-800',
    Cancelled: 'bg-red-100 text-red-800',
  }
  return colors[currentStatus || ''] || 'bg-gray-100 text-gray-800'
}

const getStatusText = (currentStatus?: string) => {
  const texts: Record<string, string> = {
    Pending: 'قيد الانتظار',
    Confirmed: 'مؤكد',
    InProgress: 'جاري',
    Completed: 'مكتمل',
    Cancelled: 'ملغي',
  }
  return texts[currentStatus || ''] || currentStatus
}

const startVideoCall = () => navigateTo(`/consultations/${consultationId.value}/video`)
const startPhoneCall = () => console.log('Start phone call')

watch(
    () => chatStore.messages.length,
    () => {
      nextTick(() => {
        if (messagesContainer.value) {
          messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
        }
      })
    }
)

watch(consultationId, (id) => activateConsultation(id), { immediate: true })

onUnmounted(() => {
  if (chatStore.activeConsultationId === consultationId.value) {
    chatStore.activeConsultationId = null
  }
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&display=swap');
</style>

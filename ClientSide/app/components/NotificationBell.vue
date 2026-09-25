<script setup lang="ts">
import { useNotificationStore, type AppNotification } from '~/stores/notifications'

defineProps<{ compact?: boolean }>()

const store = useNotificationStore()
const isOpen = ref(false)
const panelRef = ref<HTMLElement | null>(null)

const unread = computed(() => store.unreadCount)
const hasUnread = computed(() => unread.value > 0)

const toggle = async () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) await store.markAllSeen()
}

const close = () => {
  isOpen.value = false
}

const onClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement
  if (!target.closest('[data-notification-bell]')) close()
}

const onVisible = () => {
  if (document.visibilityState === 'visible') store.load()
}

onMounted(() => {
  store.load()
  document.addEventListener('click', onClickOutside)
  document.addEventListener('visibilitychange', onVisible)
})
onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('visibilitychange', onVisible)
})

const formatWhen = (iso: string) => {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  const diffMs = Date.now() - date.getTime()
  const mins = Math.floor(diffMs / 60000)
  if (mins < 1) return 'الآن'
  if (mins < 60) return `منذ ${mins} د`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `منذ ${hours} س`
  return date.toLocaleDateString('ar-EG', { day: 'numeric', month: 'short' })
}

const goTo = (n: AppNotification) => {
  close()
  if (n.title.includes('رسالة') || n.title.includes('استفسار')) navigateTo('/chat')
  else if (n.title.includes('حجز') || n.title.includes('استشار')) navigateTo('/chat')
}

defineExpose({ close })
</script>

<template>
  <div ref="panelRef" class="relative" data-notification-bell>
    <button
        type="button"
        class="relative flex h-10 w-10 items-center justify-center rounded-full transition-colors"
        :class="hasUnread
          ? 'bg-red-500/20 text-red-300 hover:bg-red-500/30'
          : 'text-emerald-100 hover:bg-white/10 hover:text-amber-300'"
        :aria-label="hasUnread ? `الإشعارات، ${unread} غير مقروء` : 'الإشعارات'"
        @click.stop="toggle"
    >
      <!-- Filled bell when there are unseen notifications -->
      <svg
          v-if="hasUnread"
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="currentColor"
      >
        <path d="M12 22a2.5 2.5 0 002.45-2h-4.9A2.5 2.5 0 0012 22zm8-6V11a8 8 0 10-16 0v5l-1.7 1.7A1 1 0 003 19h18a1 1 0 00.7-1.7L20 16z" />
      </svg>
      <!-- Outline bell when all seen -->
      <svg
          v-else
          xmlns="http://www.w3.org/2000/svg"
          class="h-5 w-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          stroke-width="2"
      >
        <path stroke-linecap="round" stroke-linejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>
      <span
          v-if="hasUnread"
          class="absolute -start-0.5 -top-0.5 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white"
      >
        {{ unread > 99 ? '99+' : unread }}
      </span>
    </button>

    <div
        v-if="isOpen"
        class="absolute end-0 z-40 mt-2 w-[min(20rem,calc(100vw-1.5rem))] overflow-hidden rounded-xl border border-gray-100 bg-white text-sm shadow-xl"
        dir="rtl"
    >
      <div class="flex items-center justify-between border-b border-gray-100 px-4 py-2.5">
        <span class="font-semibold text-emerald-950">الإشعارات</span>
        <span class="text-xs text-gray-400">{{ store.items.length }}</span>
      </div>
      <div v-if="store.isLoading" class="px-4 py-8 text-center text-gray-400">جاري التحميل...</div>
      <div v-else-if="!store.items.length" class="px-4 py-8 text-center text-gray-400">
        لا توجد إشعارات
      </div>
      <ul v-else class="max-h-80 overflow-y-auto">
        <li
            v-for="n in store.items"
            :key="n.id"
            class="cursor-pointer border-b border-gray-50 px-4 py-3 last:border-0 hover:bg-emerald-50"
            @click="goTo(n)"
        >
          <p class="font-medium text-emerald-950">{{ n.title }}</p>
          <p class="mt-0.5 line-clamp-2 text-xs text-gray-600">{{ n.message }}</p>
          <p class="mt-1 text-[10px] text-gray-400">{{ formatWhen(n.createdAt) }}</p>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
interface User {
  fullName?: string
  email: string
  profileImageUrl?: string
  role?: string
  specialization?: string
  
}

const props = defineProps<{
  user: User | null
  isLawyer: boolean
  activeSection: string
}>()

const emit = defineEmits<{
  sectionChange: [section: string]
}>()

const getInitials = (name: string) => {
  if (!name) return 'U'
  const parts = name.trim().split(' ')
  return parts.length >= 2
      ? (parts[0][0] + parts[1][0]).toUpperCase()
      : name.slice(0, 2).toUpperCase()
}

const navItems = computed(() => {
  const items = [
    { id: 'overview', label: 'نظرة عامة', icon: '📊' },
    { id: 'posts', label: 'المنشورات', icon: '📝' },
  ]

  if (props.isLawyer) {
    items.push(
        { id: 'schedule', label: 'الجدول الزمني', icon: '📅' },
        { id: 'analytics', label: 'الإحصائيات', icon: '📈' },
        { id: 'consultations', label: 'سجل الاستشارات', icon: '💬' }
    )
  }

  return items
})
</script>

<template>
  <aside class="w-full lg:w-80 shrink-0">
    <div class="bg-white rounded-2xl border border-emerald-900/10 shadow-sm overflow-hidden sticky top-4">

      <!-- Profile Header -->
      <div class="p-6 border-b border-gray-100">
        <!-- Name -->
        <h2 class="text-lg font-bold text-emerald-950 text-center mb-4" style="font-family: 'Amiri', serif;">
          {{ user?.fullName || 'المستخدم' }}
        </h2>

        <!-- Image -->
        <div class="flex justify-center mb-4">
          <div class="relative">
            <img
                v-if="user?.profileImageUrl"
                :src="user.profileImageUrl"
                :alt="user.fullName"
                class="w-24 h-24 rounded-full object-cover border-4 border-emerald-50"
            />
            <div
                v-else
                class="w-24 h-24 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-800 font-bold text-2xl border-4 border-emerald-50"
            >
              {{ getInitials(user?.fullName || '') }}
            </div>
            <!-- Online indicator -->
            <div class="absolute bottom-1 right-1 w-4 h-4 bg-emerald-500 rounded-full border-2 border-white"></div>
          </div>
        </div>

        <!-- Email -->
        <div class="text-center mb-2">
          <p class="text-sm text-gray-600">{{ user?.email }}</p>
        </div>

        <!-- Speciality (Lawyer only) -->
        <div v-if="isLawyer && user?.specialization" class="text-center">
          <span class="inline-block bg-emerald-50 text-emerald-700 text-xs font-semibold px-3 py-1 rounded-full">
            {{ user.specialization }}
          </span>
        </div>
      </div>

      <!-- Navigation Buttons -->
      <nav class="p-4 space-y-2">
        <button
            v-for="item in navItems"
            :key="item.id"
            @click="emit('sectionChange', item.id)"
            class="w-full text-right px-4 py-3 rounded-xl transition-all flex items-center gap-3 text-sm font-medium"
            :class="activeSection === item.id 
            ? 'bg-emerald-800 text-white shadow-md' 
            : 'bg-gray-50 text-gray-700 hover:bg-emerald-50 hover:text-emerald-700'"
        >
          <span class="text-lg">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </button>
      </nav>
    </div>
  </aside>
</template>
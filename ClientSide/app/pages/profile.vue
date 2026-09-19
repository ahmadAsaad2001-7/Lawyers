<script setup lang="ts">
import AnalyticsSection from "~/components/profile/AnalyticsSection.vue";

definePageMeta({
  layout: 'default',
  middleware: ['auth']
})



const authStore = useAuthStore()
const isLawyer = computed(() => authStore.user?.role === 'Lawyer')

// Active section state
const activeSection = ref<'posts' | 'schedule' | 'analytics' | 'consultations' | 'overview'>('overview')

const handleSectionChange = (section: string) => {
  activeSection.value = section as any
}
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Main Layout: Sidebar + Content -->
    <div class="max-w-7xl mx-auto p-4 md:p-6">
      <div class="flex flex-col lg:flex-row gap-6">

        <!-- LEFT SIDEBAR -->
        <ProfileSideBar
            :user="authStore.user"
            :is-lawyer="isLawyer"
            :active-section="activeSection"
            @section-change="handleSectionChange"
        />

        <!-- RIGHT CONTENT SECTION -->
        <div class="flex-1 min-w-0">
          <ProfileOverview v-if="activeSection === 'overview'" />
          <LawyerPostsManager v-else-if="activeSection === 'posts'" />
          <LawyerScheduleManager v-else-if="activeSection === 'schedule' && isLawyer" />
          <AnalyticsSection v-else-if="activeSection === 'analytics' && isLawyer" />
          <ConsultationHistorySection v-else-if="activeSection === 'consultations' && isLawyer" />
        </div>
      </div>
    </div>
  </div>
</template>
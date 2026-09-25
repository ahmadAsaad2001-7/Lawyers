<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { PostType, type LawyerPostSummaryDto } from '~/types/Lawyer';
import { useAuthStore } from '~/stores/auth';

const route = useRoute();
const router = useRouter();
const config = useRuntimeConfig();
const authStore = useAuthStore();

const lawyerId = route.params.id as string;

// Admin check
const isAdmin = computed(() => authStore.user?.role === 'Admin');

// 1. Fetch Lawyer Profile - CLIENT SIDE ONLY
const lawyer = ref<any>(null);
const isLawyerLoading = ref(true);
const lawyerError = ref<string | null>(null);

const fetchLawyer = async () => {
  isLawyerLoading.value = true;
  lawyerError.value = null;

  try {
    const response = await $fetch(`${config.public.apiBase}/Lawyers/${lawyerId}`, {
      headers: {
        Authorization: authStore.token ? `Bearer ${authStore.token}` : ''
      }
    });
    lawyer.value = response;
  } catch (err: any) {
    console.error('Failed to fetch lawyer:', err);
    lawyerError.value = 'تعذر تحميل بيانات المحامي';
  } finally {
    isLawyerLoading.value = false;
  }
};


// 2. Fetch Lawyer Posts State
const posts = ref<LawyerPostSummaryDto[]>([]);
const isPostsLoading = ref(true);
const postsError = ref<string | null>(null);

const fetchPosts = async () => {
  isPostsLoading.value = true;
  postsError.value = null;

  try {
    const data = await $fetch<LawyerPostSummaryDto[]>(
        `${config.public.apiBase}/lawyer-posts/lawyer/${lawyerId}`,
        {
          query: { page: 1, pageSize: 10 }
        }
    );
    posts.value = data || [];
  } catch (err: any) {
    console.error('Failed to fetch lawyer posts:', err);
    postsError.value = 'تعذر تحميل منشورات وإنجازات المحامي.';
  } finally {
    isPostsLoading.value = false;
  }
};

onMounted(() => {
  fetchLawyer();
  fetchPosts();
});

// Helper: Badge styling based on PostType Enum
const getPostTypeBadge = (type: PostType) => {
  switch (type) {
    case PostType.CaseVictory:
      return { text: 'قضية رابحة', icon: '⚖️', style: 'bg-emerald-100 text-emerald-800 border-emerald-200' };
    case PostType.Achievement:
      return { text: 'إنجاز', icon: '🏆', style: 'bg-amber-100 text-amber-800 border-amber-200' };
    case PostType.LegalArticle:
      return { text: 'مقال قانوني', icon: '📜', style: 'bg-blue-100 text-blue-800 border-blue-200' };
    default:
      return { text: 'رأي قانوني', icon: '💡', style: 'bg-gray-100 text-gray-800 border-gray-200' };
  }
};

const handlePostClick = (postId: number) => {
  router.push(`/posts/${postId}`);
};

const isBookingOpen = ref(false);
const bookingLawyer = computed(() => {
  if (!lawyer.value) return null;
  return {
    id: lawyer.value.id,
    name: lawyer.value.fullName,
    avatar: lawyer.value.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(lawyer.value.fullName)}&background=065f46&color=fff`,
    hourlyRate: lawyer.value.hourlyRate
  };
});
const handleBooked = (response: any) => {
  isBookingOpen.value = false;
  if (response?.paymentClientSecret) window.location.href = response.paymentClientSecret;
};
</script>

<template>
  <div>
  <div dir="rtl" class="grid grid-cols-1 lg:grid-cols-4 gap-6">

    <!-- Left Panel: Sidebar (1 Column) -->
    <aside class="lg:col-span-1 space-y-6">
      <div class="bg-white rounded-2xl p-6 border border-emerald-900/10 shadow-sm flex flex-col items-center text-center">

        <!-- Loading State -->
        <div v-if="isLawyerLoading" class="py-8">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800 mx-auto"></div>
        </div>

        <!-- Error State -->
        <div v-else-if="lawyerError" class="text-red-500 text-sm py-4">
          تعذر تحميل بيانات المحامي.
        </div>

        <!-- Lawyer Profile Data -->
        <div v-else-if="lawyer" class="w-full flex flex-col items-center">
          <div class="relative inline-block mb-4">
            <img
                :src="lawyer.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(lawyer.fullName)}&background=065f46&color=fff`"
                :alt="lawyer.fullName"
                class="w-24 h-24 rounded-full object-cover border-4 border-emerald-50 shadow-sm"
            />

            <div
                v-if="lawyer.isVerified"
                class="absolute bottom-0 right-0 bg-emerald-600 text-white p-1.5 rounded-full border-2 border-white shadow-sm flex items-center justify-center"
                title="محامي موثق"
            >
              <svg class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
            </div>
          </div>

          <h1 class="font-bold text-emerald-950 text-xl mb-1" style="font-family: 'Amiri', serif;">
            {{ lawyer.fullName }}
          </h1>
          <span class="inline-block bg-emerald-50 text-emerald-700 text-xs font-semibold px-3 py-1 rounded-full mb-3">
            {{ lawyer.specialization }}
          </span>

          <div v-if="lawyer.lawFirmName" class="flex items-center gap-1.5 text-gray-500 text-sm mb-6">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"></path>
            </svg>
            {{ lawyer.lawFirmName }}
          </div>

          <hr class="w-full border-gray-100 mb-5" />

          <div class="w-full grid grid-cols-2 gap-4 mb-5">
            <div class="bg-gray-50 rounded-xl p-3 flex flex-col items-center justify-center">
              <span class="text-xs text-gray-500 mb-1">سعر الساعة</span>
              <span class="font-bold text-emerald-900 text-sm">{{ lawyer.hourlyRate }} ج.م</span>
            </div>

            <div class="bg-amber-50 rounded-xl p-3 flex flex-col items-center justify-center">
              <span class="text-xs text-amber-700 mb-1">التقييم العام</span>
              <div class="flex items-center gap-1">
                <span class="font-bold text-amber-900 text-sm">{{ lawyer.averageRating }}</span>
                <svg class="w-4 h-4 text-amber-500 fill-current" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                </svg>
              </div>
            </div>
          </div>

          <div class="w-full flex items-start gap-2 text-sm text-gray-600 mb-5 bg-gray-50 p-3 rounded-xl text-right">
            <svg class="w-5 h-5 text-gray-400 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
            </svg>
            <span>{{ lawyer.city }}</span>
          </div>

          <div v-if="lawyer.bio" class="w-full text-right">
            <h4 class="text-xs font-bold text-emerald-900 mb-2">نبذة عن المحامي</h4>
            <p class="text-sm text-gray-600 leading-relaxed">
              {{ lawyer.bio }}
            </p>
          </div>

          <button
              @click="isBookingOpen = true"
              class="w-full mt-6 bg-emerald-800 text-white font-semibold py-3 rounded-xl text-sm hover:bg-emerald-700 transition-colors shadow-sm"
          >
            طلب استشارة
          </button>

          <!-- Admin-only free conversation button -->
          <button
              v-if="isAdmin"
              @click="isBookingOpen = true"
              class="w-full mt-3 bg-amber-500 text-white font-semibold py-3 rounded-xl text-sm hover:bg-amber-600 transition-colors shadow-sm flex items-center justify-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
            محادثة مجانية (مدير)
          </button>
        </div>
      </div>
    </aside>

    <!-- Right Panel: Posts Feed (3 Columns) -->
    <main class="lg:col-span-3 space-y-6">

      <!-- Section Header -->
      <div class="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-emerald-900/10 bg-white p-4 shadow-sm sm:p-6">
        <div>
          <h2 class="text-xl font-bold text-emerald-950" style="font-family: 'Amiri', serif;">
            المنشورات والإنجازات
          </h2>
          <p class="text-xs text-gray-500 mt-1">
            المقالات القانونية والأحكام الصادرة والقضايا التي شارك فيها المحامي
          </p>
        </div>
        <span class="bg-emerald-50 text-emerald-800 text-xs font-bold px-3 py-1.5 rounded-xl">
          {{ posts.length }} منشور
        </span>
      </div>

      <!-- Loading State -->
      <div v-if="isPostsLoading" class="bg-white rounded-2xl p-12 border border-emerald-900/10 text-center">
        <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-emerald-800 mx-auto"></div>
        <p class="text-sm text-gray-500 mt-4 font-medium">جاري تحميل منشورات المحامي...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="postsError" class="bg-red-50 rounded-2xl p-8 border border-red-100 text-center">
        <p class="text-red-600 font-medium text-sm mb-3">{{ postsError }}</p>
        <button
            @click="fetchPosts"
            class="px-4 py-2 bg-red-600 text-white text-xs font-bold rounded-xl hover:bg-red-700 transition-colors"
        >
          إعادة المحاولة
        </button>
      </div>

      <!-- Empty State -->
      <div v-else-if="posts.length === 0" class="bg-white rounded-2xl p-12 border border-dashed border-gray-200 text-center">
        <div class="w-16 h-16 bg-emerald-50 text-emerald-700 rounded-full flex items-center justify-center mx-auto mb-4 text-2xl">
          📝
        </div>
        <h3 class="text-lg font-bold text-emerald-950 mb-1">لا توجد منشورات حتى الآن</h3>
        <p class="text-sm text-gray-500">لم يقم المحامي بنشر أي مقالات أو قضايا في الوقت الحالي.</p>
      </div>

      <!-- Posts Grid -->
      <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <article
            v-for="post in posts"
            :key="post.id"
            @click="handlePostClick(post.id)"
            class="group bg-white rounded-2xl border border-emerald-900/10 p-5 hover:shadow-md hover:border-emerald-800/30 transition-all cursor-pointer flex flex-col justify-between"
        >
          <div>
            <!-- Cover Image -->
            <div v-if="post.coverImageUrl" class="relative h-44 rounded-xl bg-gray-100 mb-4 overflow-hidden border border-gray-100">
              <img
                  :src="post.coverImageUrl"
                  :alt="post.title"
                  class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
              />
            </div>

            <!-- Category & Featured Badges -->
            <div class="flex items-center gap-2 mb-3">
              <span
                  class="text-xs font-bold px-2.5 py-1 rounded-lg border flex items-center gap-1"
                  :class="getPostTypeBadge(post.type).style"
              >
                <span>{{ getPostTypeBadge(post.type).icon }}</span>
                <span>{{ getPostTypeBadge(post.type).text }}</span>
              </span>

              <span
                  v-if="post.isFeatured"
                  class="bg-emerald-900 text-white text-xs font-bold px-2 py-0.5 rounded-lg flex items-center gap-1"
              >
                📌 مميز
              </span>
            </div>

            <!-- Title & Snippet -->
            <h3 class="font-bold text-emerald-950 text-base mb-2 group-hover:text-emerald-700 transition-colors line-clamp-2" style="font-family: 'Amiri', serif;">
              {{ post.title }}
            </h3>

            <p class="text-gray-600 text-xs leading-relaxed mb-4 line-clamp-3">
              {{ post.excerpt || 'انقر لقراءة المزيد عن هذا الموضوع...' }}
            </p>
          </div>

          <!-- Footer Metadata -->
          <div class="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-400">
            <span>{{ new Date(post.createdAt).toLocaleDateString('ar-EG', { year: 'numeric', month: 'short', day: 'numeric' }) }}</span>
            <span class="text-emerald-800 font-bold group-hover:translate-x-[-4px] transition-transform flex items-center gap-1">
              اقرأ المزيد ←
            </span>
          </div>
        </article>
      </div>

    </main>
  </div>

  <BookingModel
      v-if="bookingLawyer"
      :lawyer="bookingLawyer"
      :is-open="isBookingOpen"
      @close="isBookingOpen = false"
      @booked="handleBooked"
  />
  </div>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.line-clamp-3 {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
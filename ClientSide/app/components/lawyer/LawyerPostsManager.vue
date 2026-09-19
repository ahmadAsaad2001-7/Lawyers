<script setup lang="ts">
import {PostType, type LawyerPostSummaryDto } from '~/types/Lawyer'

const posts = ref<LawyerPostSummaryDto[]>([])
const loading = ref(false)
const isCreating = ref(false)
const showCreateModal = ref(false)
const editingPost = ref<LawyerPostSummaryDto | null>(null)

// Fetch lawyer's posts
const fetchPosts = async () => {
  loading.value = true
  try {
    const data = await $fetch<LawyerPostSummaryDto[]>('/api/lawyer-posts/lawyer/me')
    posts.value = data
  } catch (error) {
    console.error('Failed to fetch posts:', error)
  } finally {
    loading.value = false
  }
}

// Delete post
const deletePost = async (postId: number) => {
  if (!confirm('هل أنت متأكد من حذف هذا المنشور؟')) return

  try {
    await $fetch(`/api/lawyer-posts/${postId}`, { method: 'DELETE' })
    await fetchPosts()
  } catch (error) {
    console.error('Failed to delete post:', error)
    alert('فشل حذف المنشور')
  }
}

// Edit post
const editPost = (post: LawyerPostSummaryDto) => {
  editingPost.value = post
  showCreateModal.value = true
}

onMounted(() => {
  fetchPosts()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header with Create Button -->
    <div class="flex items-center justify-between">
      <h2 class="text-lg font-bold text-emerald-950">منشوراتي</h2>
      <button
          @click="showCreateModal = true; editingPost = null"
          class="px-4 py-2 bg-emerald-800 text-white rounded-xl text-sm font-medium hover:bg-emerald-900 transition-colors"
      >
        + منشور جديد
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-emerald-800"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="!posts.length" class="bg-white rounded-2xl border border-dashed border-gray-300 p-12 text-center">
      <div class="w-16 h-16 bg-emerald-50 text-emerald-700 rounded-full flex items-center justify-center mx-auto mb-4 text-2xl">
        📝
      </div>
      <h3 class="text-lg font-bold text-emerald-950 mb-1">لا توجد منشورات بعد</h3>
      <p class="text-sm text-gray-500">ابدأ بنشر إنجازاتك ومقالاتك القانونية</p>
    </div>

    <!-- Posts Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div
          v-for="post in posts"
          :key="post.id"
          class="bg-white rounded-2xl border border-emerald-900/10 p-5 hover:shadow-md transition-all"
      >
        <!-- Cover Image -->
        <div v-if="post.coverImageUrl" class="relative h-44 rounded-xl bg-gray-100 mb-4 overflow-hidden">
          <img :src="post.coverImageUrl" :alt="post.title" class="w-full h-full object-cover" />
        </div>

        <!-- Badges -->
        <div class="flex items-center gap-2 mb-3">
          <span
              class="text-xs font-bold px-2.5 py-1 rounded-lg border"
              :class="{
              'bg-emerald-100 text-emerald-800 border-emerald-200': post.type === PostType.CaseVictory,
              'bg-amber-100 text-amber-800 border-amber-200': post.type === PostType.Achievement,
              'bg-blue-100 text-blue-800 border-blue-200': post.type === PostType.LegalArticle,
              'bg-gray-100 text-gray-800 border-gray-200': post.type === PostType.GeneralInsight
            }"
          >
            {{ PostType[post.type] }}
          </span>
          <span v-if="post.isFeatured" class="bg-emerald-900 text-white text-xs font-bold px-2 py-0.5 rounded-lg">
             مميز
          </span>
        </div>

        <!-- Title & Excerpt -->
        <h3 class="font-bold text-emerald-950 text-base mb-2">{{ post.title }}</h3>
        <p class="text-gray-600 text-xs mb-4 line-clamp-2">{{ post.excerpt || 'لا يوجد ملخص' }}</p>

        <!-- Footer -->
        <div class="flex items-center justify-between pt-3 border-t border-gray-100">
          <span class="text-xs text-gray-400">
            {{ new Date(post.createdAt).toLocaleDateString('ar-EG') }}
          </span>
          <div class="flex gap-2">
            <button
                @click="editPost(post)"
                class="text-xs text-emerald-700 hover:text-emerald-800 font-medium"
            >
              تعديل
            </button>
            <button
                @click="deletePost(post.id)"
                class="text-xs text-red-600 hover:text-red-700 font-medium"
            >
              حذف
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <LawyerPostModal
        v-if="showCreateModal"
        :post="editingPost"
        @close="showCreateModal = false"
        @saved="fetchPosts(); showCreateModal = false"
    />
  </div>
</template>
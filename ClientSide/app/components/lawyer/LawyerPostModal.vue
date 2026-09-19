<script setup lang="ts">
import type { LawyerPostSummaryDto } from '~/types/Lawyer'

const props = defineProps<{
  post: LawyerPostSummaryDto | null
}>()

const emit = defineEmits<{
  close: []
  saved: []
}>()

const form = ref({
  title: '',
  excerpt: '',
  content: '',
  type: 0,
  coverImageUrl: '',
  isFeatured: false
})

const saving = ref(false)

watch(() => props.post, (post) => {
  if (post) {
    form.value = {
      title: post.title,
      excerpt: post.excerpt || '',
      content: '',
      type: post.type,
      coverImageUrl: post.coverImageUrl || '',
      isFeatured: post.isFeatured
    }
  } else {
    form.value = {
      title: '',
      excerpt: '',
      content: '',
      type: 0,
      coverImageUrl: '',
      isFeatured: false
    }
  }
}, { immediate: true })

const save = async () => {
  saving.value = true
  try {
    if (props.post) {
      await $fetch(`/api/lawyer-posts/${props.post.id}`, {
        method: 'PUT',
        body: { ...form.value, id: props.post.id }
      })
    } else {
      await $fetch('/api/lawyer-posts', {
        method: 'POST',
        body: form.value
      })
    }
    emit('saved')
  } catch (error) {
    console.error('Failed to save post:', error)
    alert('فشل حفظ المنشور')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl p-6 max-h-[90vh] overflow-y-auto">
        <h2 class="text-xl font-bold text-emerald-950 mb-6">
          {{ post ? 'تعديل المنشور' : 'منشور جديد' }}
        </h2>

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">العنوان</label>
            <input
                v-model="form.title"
                type="text"
                class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">الملخص</label>
            <textarea
                v-model="form.excerpt"
                rows="3"
                class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">النوع</label>
            <select
                v-model="form.type"
                class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option :value="0">رأي عام</option>
              <option :value="1">قضية رابحة</option>
              <option :value="2">إنجاز</option>
              <option :value="3">مقال قانوني</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">رابط الصورة</label>
            <input
                v-model="form.coverImageUrl"
                type="url"
                class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div class="flex items-center gap-2">
            <input
                v-model="form.isFeatured"
                type="checkbox"
                id="featured"
                class="w-4 h-4 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
            />
            <label for="featured" class="text-sm text-gray-700">تثبيت المنشور</label>
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-2">
          <button
              @click="emit('close')"
              class="px-4 py-2 border border-gray-200 text-gray-600 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors"
          >
            إلغاء
          </button>
          <button
              @click="save"
              :disabled="saving"
              class="px-4 py-2 bg-emerald-800 text-white rounded-xl text-sm font-medium hover:bg-emerald-900 transition-colors disabled:opacity-50"
          >
            {{ saving ? 'جاري الحفظ...' : 'حفظ' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
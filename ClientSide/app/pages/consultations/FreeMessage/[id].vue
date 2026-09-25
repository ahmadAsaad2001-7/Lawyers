<template>
  <div class="min-h-screen bg-gray-50 flex flex-col" dir="rtl">

    <!-- Top Header -->
    <header class="flex items-center justify-between border-b border-emerald-100 bg-white px-3 py-4 shadow-sm sm:px-6">
      <div v-if="lawyer" class="flex min-w-0 items-center gap-3">
        <img
            :src="lawyer.avatar || 'https://png.pngtree.com/background/20230809/original/pngtree-serious-man-portrait-handsome-caucasian-person-photo-picture-image_4530325.jpg'"
            :alt="lawyer.fullName"
            class="w-10 h-10 rounded-full object-cover border-2 border-amber-400"
        />
        <div>
          <h3 class="truncate text-sm font-bold text-emerald-900" style="font-family: 'Amiri', serif;">
            استشارة مجانية مع {{ lawyer.fullName }}
          </h3>
          <p class="text-xs text-gray-500">{{ lawyer.specialization }}</p>
        </div>
      </div>
    </header>

    <!-- STEP 1: Form View -->
    <main v-if="!isSuccess" class="flex flex-1 items-center justify-center p-4 sm:p-6">
      <div class="w-full max-w-lg rounded-2xl border border-emerald-900/10 bg-white p-5 shadow-sm sm:p-8">
        <h2 class="text-xl font-bold text-emerald-950 mb-2 text-center" style="font-family: 'Amiri', serif;">
          أرسل استفسارك المجاني
        </h2>
        <p class="text-xs text-gray-500 mb-6 text-center">
          أدخل بياناتك ليتمكن المحامي من مراجعة استفسارك والرد عليك.
        </p>

        <!-- Error Alert Banner -->
        <div v-if="errorMessage" class="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-xl text-xs flex items-center gap-2">
          <span>{{ errorMessage }}</span>
        </div>

        <form @submit.prevent="handleSubmit" class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-emerald-900 mb-1">الاسم بالكامل *</label>
            <input
                v-model="guestForm.name"
                type="text"
                required
                placeholder="مثال: أحمد محمد"
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 text-sm focus:border-emerald-500 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-emerald-900 mb-1">رقم الهاتف *</label>
            <input
                v-model="guestForm.phone"
                type="tel"
                required
                placeholder="010xxxxxxxx"
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 text-sm focus:border-emerald-500 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-emerald-900 mb-1">البريد الإلكتروني *</label>
            <input
                v-model="guestForm.email"
                type="email"
                required
                placeholder="example@mail.com"
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 text-sm focus:border-emerald-500 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-emerald-900 mb-1">تفاصيل الاستفسار *</label>
            <textarea
                v-model="guestForm.content"
                required
                rows="4"
                placeholder="اكتب تفاصيل استفسارك القانوني هنا..."
                class="w-full px-4 py-2.5 rounded-xl border border-gray-200 text-sm focus:border-emerald-500 focus:outline-none resize-none"
            ></textarea>
          </div>

          <button
              type="submit"
              :disabled="isSubmitting"
              class="w-full py-3 bg-emerald-800 text-white font-semibold rounded-xl text-sm hover:bg-emerald-900 transition-all shadow-md disabled:opacity-50"
          >
            <span v-if="!isSubmitting">إرسال الاستفسار الآن</span>
            <span v-else>جاري الإرسال...</span>
          </button>
        </form>
      </div>
    </main>

    <!-- STEP 2: Success Confirmation Screen -->
    <main v-else class="flex flex-1 items-center justify-center p-4 sm:p-6">
      <div class="w-full max-w-md rounded-2xl border border-emerald-900/10 bg-white p-5 text-center shadow-sm sm:p-8">
        <div class="w-16 h-16 bg-emerald-100 text-emerald-700 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
          </svg>
        </div>

        <h2 class="text-2xl font-bold text-emerald-950 mb-2" style="font-family: 'Amiri', serif;">
          تم إرسال استفسارك بنجاح!
        </h2>
        <p class="text-sm text-gray-600 mb-6">
          شكرًا لك. تم استلام تفاصيل استفسارك وسيتم مراجعته والتواصل معك عبر البريد الإلكتروني أو رقم الهاتف المدخل قريباً.
        </p>

        <NuxtLink
            to="/"
            class="inline-block min-h-11 rounded-xl bg-emerald-800 px-6 py-3 text-sm font-medium text-white transition-colors hover:bg-emerald-900"
        >
          العودة لقائمة المحامين
        </NuxtLink>
      </div>
    </main>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';

const route = useRoute();
const lawyerId = route.params.id as string;

// Access lawyer composable
const { lawyer, sendFreeMessage } = useLawyer(lawyerId);

const isSubmitting = ref(false);
const isSuccess = ref(false);
const errorMessage = ref<string | null>(null);

const guestForm = reactive({
  name: '',
  phone: '',
  email: '',
  content: ''
});

const handleSubmit = async () => {
  isSubmitting.value = true;
  errorMessage.value = null;

  try {
    await sendFreeMessage(guestForm);
    isSuccess.value = true; // Displays success view
  } catch (err: any) {
    console.error('Submission failed:', err);
    errorMessage.value = err.data?.message || err.response?._data?.message || 'حدث خطأ أثناء إرسال الرسالة. يرجى المحاولة لاحقاً.';
  } finally {
    isSubmitting.value = false;
  }
};
</script>
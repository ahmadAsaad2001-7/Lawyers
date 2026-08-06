<script setup lang="ts">
import { ref } from 'vue';
import BookingModal from "./BookingModel.vue";

interface Lawyer {
  id: number; // ✅ needed so the modal knows which lawyer to book
  name: string;
  city: string;
  officeName: string;
  address: string;
  phone: string;
  expertise: string[];
  languages: string[];
  avatar: string;
  rating: number;
  hourlyRate: number; // ✅ needed for the modal's cost preview
}

const props = defineProps<{
  lawyer: Lawyer;
}>();

const currentTab = ref<'front' | 'back'>('front');
const isBookingModalOpen = ref(false);

// Elegant 24-point star SVG path
const starPath = 'M 44.78 10.34 Q 50.00 4.00 55.22 10.34 Q 61.91 5.57 65.31 13.04 Q 73.00 10.16 74.35 18.27 Q 82.53 17.47 81.73 25.65 Q 89.84 27.00 86.96 34.69 Q 94.43 38.09 89.66 44.78 Q 96.00 50.00 89.66 55.22 Q 94.43 61.91 86.96 65.31 Q 89.84 73.00 81.73 74.35 Q 82.53 82.53 74.35 81.73 Q 73.00 89.84 65.31 86.96 Q 61.91 94.43 55.22 89.66 Q 50.00 96.00 44.78 89.66 Q 38.09 94.43 34.69 86.96 Q 27.00 89.84 25.65 81.73 Q 17.47 82.53 18.27 74.35 Q 10.16 73.00 13.04 65.31 Q 5.57 61.91 10.34 55.22 Q 4.00 50.00 10.34 44.78 Q 5.57 38.09 13.04 34.69 Q 10.16 27.00 18.27 25.65 Q 17.47 17.47 25.65 18.27 Q 27.00 10.16 34.69 13.04 Q 38.09 5.57 44.78 10.34 Z';

// Fires only once the backend has reserved the consultation and generated
// the Kashier hosted checkout URL.
const handleBooked = (response: {
  consultationId: number;
  paymentClientSecret: string;
  totalCost: number;
  scheduledAt: string;
  status: string;
}) => {
  console.log('Consultation reserved, awaiting payment:', response);
  isBookingModalOpen.value = false;

  if (response.paymentClientSecret) {
    window.location.href = response.paymentClientSecret;
  }
};
</script>

<template>
  <div class="bg-white rounded-2xl p-5 border border-emerald-900/10 shadow-sm hover:shadow-md transition-shadow relative flex flex-col justify-between min-h-[380px]">

    <!-- FRONT SLIDE -->
    <div v-if="currentTab === 'front'" class="flex flex-col items-center text-center space-y-4">
      <div class="w-full flex items-center justify-between text-xs font-medium">
        <span class="bg-emerald-50 text-emerald-700 px-2.5 py-1 rounded-full border border-emerald-200">نشط الآن</span>
        <span class="bg-amber-100 text-amber-800 px-2.5 py-1 rounded-full flex items-center gap-1">
          {{ (lawyer.rating || 5.0).toFixed(1) }} ★
        </span>
      </div>

      <!-- ISLAMIC 24-POINT STAR FRAME (SVG) -->
      <div class="relative flex items-center justify-center w-36 h-36 my-2">
        <svg viewBox="0 0 100 100" class="absolute inset-0 w-full h-full">
          <path :d="starPath" fill="url(#goldGradient)" stroke="#d4a017" stroke-width="0.25" />
          <path :d="starPath" fill="white" transform="scale(0.92) translate(4,4)" />
          <defs>
            <linearGradient id="goldGradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stop-color="#f59e0b" />
              <stop offset="50%" stop-color="#fbbf24" />
              <stop offset="100%" stop-color="#d97706" />
            </linearGradient>
          </defs>
        </svg>

        <svg viewBox="0 0 100 100" class="absolute inset-0 w-full h-full p-1.5">
          <defs>
            <clipPath id="starClip">
              <path :d="starPath" transform="scale(0.88) translate(6.8, 6.8)" />
            </clipPath>
          </defs>
          <image
              :href="lawyer.avatar || 'https://png.pngtree.com/background/20230809/original/pngtree-serious-man-portrait-handsome-caucasian-person-photo-picture-image_4530325.jpg'"
              :xlink:href="lawyer.avatar || 'https://png.pngtree.com/background/20230809/original/pngtree-serious-man-portrait-handsome-caucasian-person-photo-picture-image_4530325.jpg'"
              x="0" y="0" width="100" height="100"
              clip-path="url(#starClip)"
              preserveAspectRatio="xMidYMid slice"
          />
        </svg>
      </div>

      <div>
        <h3 class="font-bold text-lg text-emerald-950" style="font-family: 'Amiri', serif;">{{ lawyer.name }}</h3>
        <p class="text-xs text-gray-500 mt-0.5">{{ lawyer.city }}</p>
      </div>

      <div class="grid grid-cols-3 gap-2 w-full pt-2 text-xs">
        <nuxt-link :to="`/Lawyers/${lawyer.id}`" class="bg-emerald-800 text-white py-2 rounded-lg hover:bg-emerald-900 transition-colors">
          <button >
            الملف الشخصي
          </button>
        </nuxt-link>
        <!-- ✅ hire button now opens the booking modal -->
        <button
            @click="isBookingModalOpen = true"
            class="bg-amber-400 text-emerald-950 font-semibold py-2 rounded-lg hover:bg-amber-300 transition-colors"
        >
          توظيف
        </button>
        <nuxt-link :to="`/consultations/FreeMessage/${lawyer.id}`" class="bg-emerald-800 text-white py-2 rounded-lg hover:bg-emerald-900 transition-colors">
          <button >
            محادثة
          </button>
        </nuxt-link>
      </div>
    </div>

    <!-- BACK SLIDE -->
    <div v-else class="flex flex-col space-y-4 text-right">
      <div class="bg-emerald-50/50 p-3 rounded-xl border border-emerald-100">
        <h4 class="font-bold text-sm text-emerald-900" style="font-family: 'Amiri', serif;">{{ lawyer.officeName }}</h4>
        <p class="text-xs text-gray-600 mt-1">📍 {{ lawyer.address }}</p>
        <p class="text-xs text-gray-600 mt-0.5">📞 {{ lawyer.phone }}</p>
      </div>

      <div>
        <span class="text-xs font-bold text-emerald-900 block mb-1.5">مجالات الاختصاص:</span>
        <div class="flex flex-wrap gap-1.5">
          <span
              v-for="area in lawyer.expertise"
              :key="area"
              class="bg-gray-100 text-gray-700 text-[11px] px-2 py-0.5 rounded-md"
          >
            {{ area }}
          </span>
        </div>
      </div>

      <div>
        <span class="text-xs font-bold text-emerald-900 block mb-1.5">اللغات:</span>
        <p class="text-xs text-gray-600">{{ lawyer.languages.join('، ') }}</p>
      </div>
    </div>

    <!-- Slide Indicators -->
    <div class="flex items-center justify-center gap-2 pt-4 border-t border-gray-100">
      <button
          @click="currentTab = 'front'"
          class="h-2 rounded-full transition-all"
          :class="currentTab === 'front' ? 'w-6 bg-amber-400' : 'w-2 bg-gray-300'"
          aria-label="عرض الوجه الأمامي"
      ></button>
      <button
          @click="currentTab = 'back'"
          class="h-2 rounded-full transition-all"
          :class="currentTab === 'back' ? 'w-6 bg-amber-400' : 'w-2 bg-gray-300'"
          aria-label="عرض الوجه الخلفي"
      ></button>
    </div>

    <!-- Booking Modal -->
    <BookingModal
        :lawyer="{ id: lawyer.id, name: lawyer.name, avatar: lawyer.avatar, hourlyRate: lawyer.hourlyRate }"
        :is-open="isBookingModalOpen"
        @close="isBookingModalOpen = false"
        @booked="handleBooked"
    />
  </div>
</template>

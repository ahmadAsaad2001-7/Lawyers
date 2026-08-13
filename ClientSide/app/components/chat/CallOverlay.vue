<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { useChatStore } from '~/stores/Chat';

const store = useChatStore();
const localVideo = ref<HTMLVideoElement | null>(null);
const remoteVideo = ref<HTMLVideoElement | null>(null);
const remoteAudio = ref<HTMLAudioElement | null>(null);

// Watch both stream instances AND state transitions so remounted <video> elements get bound
watch(
    [() => store.localStream, () => store.callState],
    async () => {
      await nextTick();
      if (localVideo.value && store.localStream) {
        localVideo.value.srcObject = store.localStream;
        await localVideo.value.play().catch(() => {});
      }
    },
    { immediate: true }
);

watch(
    [() => store.remoteStream, () => store.callState],
    async () => {
      await nextTick();
      if (remoteVideo.value && store.remoteStream) {
        remoteVideo.value.srcObject = store.remoteStream;
        try {
          await remoteVideo.value.play();
          // After successful play, unmute
          remoteVideo.value.muted = false;
        } catch (e) {
          console.error('Autoplay blocked, keep muted or show play button');
        }
      }
      if (remoteAudio.value && store.remoteStream) {
        remoteAudio.value.srcObject = store.remoteStream;
        await remoteAudio.value.play().catch(() => {});
      }
    },
    { immediate: true }
);
</script>
<template>
  <Teleport to="body">
    <!-- Incoming -->
    <div v-if="store.callState === 'incoming' && store.incomingCall" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div class="w-80 rounded-2xl bg-white p-8 text-center shadow-xl" dir="rtl">
        <div class="mb-3 text-4xl">{{ store.incomingCall.mode === 'video' ? '🎥' : '🎙️' }}</div>
        <h3 class="mb-1 font-bold text-emerald-950">مكالمة واردة</h3>
        <p class="mb-6 text-sm text-gray-500">{{ store.incomingCall.mode === 'video' ? 'مكالمة فيديو' : 'مكالمة صوتية' }}</p>
        <div class="flex justify-center gap-3">
          <button @click="store.acceptCall()" class="rounded-xl bg-green-600 px-6 py-2.5 font-semibold text-white hover:bg-green-700">رد</button>
          <button @click="store.rejectCall()" class="rounded-xl bg-red-600 px-6 py-2.5 font-semibold text-white hover:bg-red-700">رفض</button>
        </div>
      </div>
    </div>

    <!-- Ringing / connecting -->
    <div v-else-if="store.callState === 'ringing' || store.callState === 'connecting'" class="fixed inset-0 z-50 flex flex-col items-center justify-center bg-gray-900" dir="rtl">
      <div class="mb-4 text-6xl">{{ store.callMode === 'video' ? '🎥' : '🎙️' }}</div>
      <p class="mb-6 text-lg font-medium text-white">
        {{ store.callState === 'connecting' ? 'تمت الموافقة — جاري التوصيل...' : 'جاري الاتصال...' }}
      </p>
      <p v-if="store.callState === 'ringing'" class="-mt-4 mb-6 text-sm text-gray-300">
        ستنتهي المكالمة تلقائياً خلال {{ store.ringSecondsRemaining }} ثانية
      </p>
      <p v-if="store.callDiagnostic" class="-mt-3 mb-5 max-w-md px-6 text-center text-xs text-gray-400" dir="ltr">
        {{ store.callDiagnostic }}
      </p>
      <video
          v-if="store.callMode === 'video'"
          ref="remoteVideo"
          autoplay
          playsinline
          muted
          class="h-full w-full object-contain"
      ></video>
      <button @click="store.cancelCall()" class="rounded-full bg-red-600 px-8 py-3 font-bold text-white hover:bg-red-700">إلغاء</button>
    </div>

    <!-- In call -->
    <div v-else-if="store.callState === 'in-call'" class="fixed inset-0 z-50 flex flex-col bg-gray-900" dir="rtl">
      <div class="relative flex flex-1 items-center justify-center">
        <video v-if="store.callMode === 'video'" ref="remoteVideo" autoplay playsinline class="h-full w-full object-contain"></video>
        <div v-else class="text-6xl text-white">🎙️</div>
        <audio v-if="store.callMode === 'audio'" ref="remoteAudio" autoplay></audio>
        <video v-if="store.callMode === 'video' && !store.isCameraOff && store.localStream"
               ref="localVideo" autoplay playsinline muted
               class="absolute bottom-4 start-4 h-28 w-40 rounded-xl border-2 border-white/30 object-cover shadow-lg"></video>
      </div>
      <div class="flex items-center justify-center gap-4 bg-black/40 py-5">
        <button @click="store.toggleMute()" class="rounded-full p-4 text-white" :class="store.isMuted ? 'bg-red-600' : 'bg-white/20 hover:bg-white/30'">{{ store.isMuted ? '🔇' : '🎤' }}</button>
        <button v-if="store.callMode === 'video'" @click="store.toggleCamera()" class="rounded-full p-4 text-white" :class="store.isCameraOff ? 'bg-red-600' : 'bg-white/20 hover:bg-white/30'">{{ store.isCameraOff ? '🚫' : '🎥' }}</button>
        <button @click="store.endCall()" class="rounded-full bg-red-600 px-8 py-4 font-bold text-white hover:bg-red-700">إنهاء</button>
      </div>
    </div>
  </Teleport>
</template>

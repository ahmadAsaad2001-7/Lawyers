<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onBeforeUnmount } from 'vue';
import { useChatStore } from '~/stores/Chat';

const store = useChatStore();

const localVideo = ref<HTMLVideoElement | null>(null);
const remoteVideo = ref<HTMLVideoElement | null>(null);
const remoteAudio = ref<HTMLAudioElement | null>(null);
const audioBlocked = ref(false);

const tryPlay = async (el: HTMLMediaElement | null): Promise<boolean> => {
  if (!el) return false;
  try {
    await el.play();
    return true;
  } catch {
    return false;
  }
};

const playAll = async () => {
  // The video is muted, so its play() will succeed.
  await tryPlay(remoteVideo.value);
  // The audio element is not muted, may be blocked.
  const ok = await tryPlay(remoteAudio.value);
  audioBlocked.value = store.callState === 'in-call' && !ok;
  if (ok) {
    document.removeEventListener('pointerdown', onTap);
  }
};

// Any user interaction during a call retries audio playback.
const onTap = () => {
  if (store.callState === 'in-call') {
    playAll();
  }
};

onMounted(() => document.addEventListener('pointerdown', onTap));
onBeforeUnmount(() => document.removeEventListener('pointerdown', onTap));

// Watch remoteStream and assign the whole stream to both elements
watch(
    
    () => store.remoteStream,
    async (stream) => {
      const audioTracks = store.remoteStream?.getAudioTracks();
      console.log('Audio tracks in remoteStream:', audioTracks?.length);
      if (audioTracks && audioTracks.length > 0) {
        console.log('Setting audio element srcObject');
      }
      await nextTick();
      if (stream) {
        if (remoteVideo.value) {
          // video element gets the full stream but is muted
          remoteVideo.value.srcObject = stream;
        }
        if (remoteAudio.value) {
          // audio element gets the full stream and plays audio
          remoteAudio.value.srcObject = stream;
          // Attempt to play (may be blocked)
          await playAll();
        }
      } else {
        // Cleanup if remoteStream becomes null
        if (remoteVideo.value) remoteVideo.value.srcObject = null;
        if (remoteAudio.value) remoteAudio.value.srcObject = null;
        audioBlocked.value = false;
      }
    },
    { immediate: true }
);

// Watch local stream and call state separately for local video and play triggers
watch(
    () => store.localStream,
    async (stream) => {
      await nextTick();
      if (localVideo.value) {
        localVideo.value.srcObject = stream;
      }
    },
    { immediate: true }
);

watch(
    () => store.callState,
    async (state) => {
      await nextTick();
      if (state === 'in-call') {
        await playAll();
      } else if (state === 'idle') {
        audioBlocked.value = false;
      }
    }
);
</script>

<template>
  <Teleport to="body">
    <!-- ═══ MEDIA LAYER: always mounted, visibility via v-show ═══ -->

    <!-- Remote VIDEO (kept MUTED so it can always autoplay; sound comes from <audio>) -->
    <video
        ref="remoteVideo"
        muted
        playsinline
        v-show="store.callState === 'in-call' && store.callMode === 'video'"
        class="fixed inset-0 z-40 h-full w-full bg-gray-900 object-contain"
    ></video>

    <!-- Local video (muted, for preview) -->
    <video
        ref="localVideo"
        muted
        playsinline
        v-show="store.callState === 'in-call' && store.callMode === 'video' && !store.isCameraOff && !!store.localStream"
        class="fixed bottom-24 start-4 z-40 h-28 w-40 rounded-xl border-2 border-white/30 object-cover shadow-lg"
    ></video>



    <audio ref="remoteAudio" autoplay></audio>


    <div
        v-show="store.callState === 'in-call' && store.callMode === 'audio'"
        class="fixed inset-0 z-40 flex items-center justify-center bg-gray-900"
    >
      <div class="text-6xl text-white">🎙️</div>
    </div>

    <!-- Button shown if audio autoplay is blocked -->
    <button
        v-if="audioBlocked"
        @click="playAll"
        class="fixed left-1/2 top-6 z-[60] -translate-x-1/2 rounded-full bg-amber-500 px-6 py-3 text-sm font-bold text-white shadow-2xl"
    >
      🔊 فعّل الصوت
    </button>

    
    <!-- ═══ Incoming call dialog ═══ -->
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

    <!-- ═══ Ringing / connecting ═══ -->
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
      <button @click="store.cancelCall()" class="rounded-full bg-red-600 px-8 py-3 font-bold text-white hover:bg-red-700">إلغاء</button>
    </div>

    <!-- ═══ In-call controls ═══ -->
    <div v-show="store.callState === 'in-call'" class="fixed inset-x-0 bottom-0 z-50 flex items-center justify-center gap-4 bg-black/40 py-5">
      <button @click="store.toggleMute()" class="rounded-full p-4 text-white" :class="store.isMuted ? 'bg-red-600' : 'bg-white/20 hover:bg-white/30'">
        {{ store.isMuted ? '🔇' : '🎤' }}
      </button>
      <button v-if="store.callMode === 'video'" @click="store.toggleCamera()" class="rounded-full p-4 text-white" :class="store.isCameraOff ? 'bg-red-600' : 'bg-white/20 hover:bg-white/30'">
        {{ store.isCameraOff ? '🚫' : '🎥' }}
      </button>
      <button @click="store.endCall()" class="rounded-full bg-red-600 px-8 py-4 font-bold text-white hover:bg-red-700">إنهاء</button>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';
import CallOverlay from '~/components/chat/CallOverlay.vue';

const authStore = useAuthStore();
const chatStore = useChatStore();

// Ringtone audio elements
const incomingAudio = ref<HTMLAudioElement | null>(null);
const outgoingAudio = ref<HTMLAudioElement | null>(null);
let isAudioUnlocked = false;

// Unlock audio on first user interaction
const unlockGlobalAudio = async () => {
  if (isAudioUnlocked) return;

  const inc = incomingAudio.value;
  const out = outgoingAudio.value;

  // Play and immediately pause at 0 volume to whitelist the elements
  if (inc) {
    inc.volume = 0;
    try {
      await inc.play();
      inc.pause();
      inc.currentTime = 0;
    } catch (e) {
      console.warn('Failed to unlock incoming audio:', e);
    }
  }

  if (out) {
    out.volume = 0;
    try {
      await out.play();
      out.pause();
      out.currentTime = 0;
    } catch (e) {
      console.warn('Failed to unlock outgoing audio:', e);
    }
  }

  // Restore volume for when they actually ring
  if (inc) inc.volume = 1;
  if (out) out.volume = 1;

  isAudioUnlocked = true;
  console.log('[Audio] Global audio unlocked');
};

// Watch call state and trigger ringtones
watch(() => chatStore.callState, (newState) => {
  if (newState === 'incoming') {
    incomingAudio.value?.play().catch(() => {
      console.warn('Incoming ringtone blocked - audio not unlocked yet');
    });
  } else if (newState === 'ringing') {
    outgoingAudio.value?.play().catch(() => {
      console.warn('Outgoing ringtone blocked - audio not unlocked yet');
    });
  } else {
    // Stop all audio if idle, connecting, or in-call
    if (incomingAudio.value) {
      incomingAudio.value.pause();
      incomingAudio.value.currentTime = 0;
    }
    if (outgoingAudio.value) {
      outgoingAudio.value.pause();
      outgoingAudio.value.currentTime = 0;
    }
  }
}, { immediate: true });

// Attach unlock to first user interaction
onMounted(async () => {
  await authStore.initAuth();

  if (authStore.isAuthenticated && authStore.token) {
    const isLawyer = authStore.user?.role === 'Lawyer';
    await chatStore.initializeGlobal(authStore.token, isLawyer);
  }

  // Listen for any user gesture to unlock audio
  const unlock = () => {
    unlockGlobalAudio();
    // Remove listeners after first unlock
    document.removeEventListener('click', unlock);
    document.removeEventListener('keydown', unlock);
    document.removeEventListener('touchstart', unlock);
  };

  document.addEventListener('click', unlock);
  document.addEventListener('keydown', unlock);
  document.addEventListener('touchstart', unlock);
});

onBeforeUnmount(() => {
  // Cleanup audio
  if (incomingAudio.value) {
    incomingAudio.value.pause();
    incomingAudio.value = null;
  }
  if (outgoingAudio.value) {
    outgoingAudio.value.pause();
    outgoingAudio.value = null;
  }
});
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <NavBar />
    <main class="container mx-auto px-6 py-8">
      <slot />
    </main>

    <!-- Global Call Overlay -->
    <CallOverlay v-if="chatStore.callState !== 'idle'" />

    <!-- Ringtone Audio Elements (hidden, always in DOM) -->
    <audio
        ref="incomingAudio"
        loop
        preload="auto"
        src="/sounds/archipelago.mp3"
    ></audio>

    <audio
        ref="outgoingAudio"
        loop
        preload="auto"
        src="/sounds/telephone_ring.mp3"
    ></audio>
  </div>
</template>
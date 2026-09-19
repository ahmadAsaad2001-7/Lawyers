<script setup lang="ts">
import { onMounted, onBeforeUnmount, watch } from 'vue';
import { useAuthStore } from '~/stores/auth';
import { useChatStore } from '~/stores/Chat';
import CallOverlay from '~/components/chat/CallOverlay.vue';
import { useCallRingtones } from '~/composables/CallRingtones';
import { useNotificationStore } from '~/stores/notifications';

const authStore = useAuthStore();
const chatStore = useChatStore();

// Ringtones + audio-unlock logic live once here (see CallRingtones
// composable) instead of being duplicated per-layout, so the unlock
// state and the <audio> elements survive layout switches (e.g.
// default -> Admin) instead of resetting.
const { incomingAudio, outgoingAudio, teardown } = useCallRingtones();

// authStore.initAuth() is already kicked off by plugins/auth.client.ts
// on app boot. We don't call it again here to avoid a second
// concurrent /auth/me request — we just react once it resolves.
// `watch(..., { immediate: true })` covers both cases: if initAuth()
// has already finished by the time this component mounts, the
// callback fires immediately with the current value; if it's still
// in flight, it fires as soon as isAuthenticated flips true.
const stopAuthWatch = watch(
    () => authStore.isAuthenticated,
    async (isAuthed) => {
      if (isAuthed && authStore.token) {
        await chatStore.initializeGlobal(
            authStore.token,
            authStore.user?.role === 'Lawyer'
        );
        await useNotificationStore().load();
      } else {
        useNotificationStore().reset();
      }
    },
    { immediate: true }
);

onBeforeUnmount(() => {
  stopAuthWatch();
  teardown();
});
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>

  <!-- Global Call Overlay: mounted once for the whole app so it -->
  <!-- survives layout changes mid-call. -->
  <CallOverlay v-if="chatStore.callState !== 'idle'" />

  <!-- Ringtone Audio Elements (hidden, always in DOM, once) -->
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
</template>
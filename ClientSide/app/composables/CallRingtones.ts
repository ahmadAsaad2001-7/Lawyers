import { ref, watch, type Ref } from 'vue';
import { useChatStore } from '~/stores/Chat';

export interface UseCallRingtones {
    incomingAudio: Ref<HTMLAudioElement | null>;
    outgoingAudio: Ref<HTMLAudioElement | null>;
    teardown: () => void;
}

/**
 * Owns the incoming/outgoing ringtone <audio> element refs, the
 * "unlock audio on first user gesture" dance, and the watcher that
 * plays/stops them based on chatStore.callState.
 *
 * Call this exactly once (from app.vue) so there is a single set of
 * <audio> elements and a single unlock flag for the whole app. This
 * used to be copy-pasted into every layout, which meant:
 *   - the unlock flag reset every time the user crossed a layout
 *     boundary, silently re-blocking ringtone playback until they
 *     tapped again
 *   - the two copies could (and did) drift out of sync
 *   - a mid-call layout switch tore down and recreated the audio
 *     elements
 * Hoisting this to a single composable + a single app.vue mount
 * fixes all three.
 */
export function useCallRingtones(): UseCallRingtones {
    const chatStore = useChatStore();

    const incomingAudio = ref<HTMLAudioElement | null>(null);
    const outgoingAudio = ref<HTMLAudioElement | null>(null);
    let isAudioUnlocked = false;

    const unlockGlobalAudio = async () => {
        if (isAudioUnlocked) return;

        for (const el of [incomingAudio.value, outgoingAudio.value]) {
            if (!el) continue;

            el.volume = 0;

            try {
                await el.play();
                el.pause();
                el.currentTime = 0;
            } catch (err) {
                console.warn('[Audio] Failed to unlock element:', err);
            }

            el.volume = 1;
        }

        isAudioUnlocked = true;
        console.log('[Audio] Global audio unlocked');
    };

    const stopWatcher = watch(
        () => chatStore.callState,
        (state) => {
            if (state === 'incoming') {
                incomingAudio.value?.play().catch(() => {
                    console.warn('[Audio] Incoming ringtone blocked — not unlocked yet');
                });
            } else if (state === 'ringing') {
                outgoingAudio.value?.play().catch(() => {
                    console.warn('[Audio] Outgoing ringtone blocked — not unlocked yet');
                });
            } else {
                for (const el of [incomingAudio.value, outgoingAudio.value]) {
                    if (!el) continue;
                    el.pause();
                    el.currentTime = 0;
                }
            }
        },
        { immediate: true }
    );

    const unlock = () => {
        unlockGlobalAudio();
        removeUnlockListeners();
    };

    const removeUnlockListeners = () => {
        (['click', 'keydown', 'touchstart'] as const).forEach((evt) =>
            document.removeEventListener(evt, unlock)
        );
    };

    if (import.meta.client) {
        (['click', 'keydown', 'touchstart'] as const).forEach((evt) =>
            document.addEventListener(evt, unlock)
        );
    }

    const teardown = () => {
        stopWatcher();
        removeUnlockListeners();

        incomingAudio.value?.pause();
        outgoingAudio.value?.pause();
        incomingAudio.value = null;
        outgoingAudio.value = null;
    };

    return { incomingAudio, outgoingAudio, teardown };
}   
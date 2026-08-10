import { useAuthStore } from '~/stores/auth';

export interface BookingResponseDto {
    consultationId: number;
    lawyerId: number;
    scheduledAt: string;
    status: string;
    totalCost: number;
    paymentClientSecret: string;
}

export type PaymentChannel = 'Card' | 'MobileWallet' | 'InstaPay';

export interface BookingPayload {
    lawyerId: number;
    scheduledAt: string;
    durationMinutes: number;
    channel: PaymentChannel;
    cardToken?: string;
    walletPhoneNumber?: string;
}

export const useBooking = () => {
    const isSubmitting = ref(false);
    const bookingError = ref('');

    const submitBooking = async (payload: BookingPayload): Promise<BookingResponseDto | null> => {
        const authStore = useAuthStore();
        const config = useRuntimeConfig();

        if (!authStore.token) {
            bookingError.value = 'يجب تسجيل الدخول أولاً لحجز استشارة';
            return null;
        }

        isSubmitting.value = true;
        bookingError.value = '';

        try {
            const base = (config.public.apiBase as string).endsWith('/')
                ? (config.public.apiBase as string)
                : `${config.public.apiBase}/`;
            const response = await $fetch<BookingResponseDto>(`${base}consultations/book`, {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${authStore.token}`,
                },
                body: {
                    lawyerId: payload.lawyerId,
                    scheduledAt: payload.scheduledAt,
                    durationMinutes: payload.durationMinutes,
                    channel: payload.channel,
                    cardToken: payload.cardToken || null,
                    walletPhoneNumber: payload.walletPhoneNumber || null,
                },
            });

            return response;
        } catch (err: any) {
            bookingError.value =
                err?.data?.message ||
                err?.data?.detail ||
                (typeof err?.data === 'string' ? err.data : null) ||
                translateFallback(err?.statusCode);
            return null;
        } finally {
            isSubmitting.value = false;
        }
    };

    const translateFallback = (statusCode?: number): string => {
        switch (statusCode) {
            case 401:
                return 'انتهت جلستك، يرجى تسجيل الدخول مرة أخرى';
            case 400:
                return 'بيانات الحجز غير صحيحة';
            case 409:
                return 'هذا الموعد محجوز بالفعل، يرجى اختيار وقت آخر';
            default:
                return 'حدث خطأ أثناء الحجز، يرجى المحاولة مرة أخرى';
        }
    };

    return { isSubmitting, bookingError, submitBooking };
};
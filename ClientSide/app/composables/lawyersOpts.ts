import type { Lawyer } from "~/types/Lawyer";

export interface FreeMessagePayload {
    name: string;
    phone: string;
    email: string;
    content: string;
}

export const useLawyer = (lawyerId: string | number) => {
    const config = useRuntimeConfig();
    const base = computed(() => {
        const b = config.public.apiBase as string;
        return b.endsWith('/') ? b : `${b}/`;
    });
    // Fetch lawyer details via SSR-friendly useFetch
    const { data: lawyer, pending: isLoading, error, refresh } = useFetch<Lawyer>(
        `/Lawyers/${lawyerId}`,
        {
            baseURL: config.public.apiBase,
            default: () => ({
                id: Number(lawyerId),
                fullName: 'أحمد محمود',
                bio: 'محامي متخصص في القضايا التجارية وتأسيس الشركات بخبرة تزيد عن 10 سنوات في المحاكم المصرية.',
                hourlyRate: 500,
                specialization: 'قانون تجاري',
                city: 'القاهرة، مصر',
                averageRating: 4.8,
                lawFirmName: 'مؤسسة العدالة للمحاماة',
                isVerified: true,
                avatar: null
            })
        }
    );

    // Send anonymous/free message payload
    const sendFreeMessage = async (payload: FreeMessagePayload) => {
        return await $fetch(`${config.public.apiBase}/consultations/free-message`, {
            method: 'POST',
            body: {
                lawyerId: Number(lawyerId),
                ...payload
            }
        });
    };

    return {
        lawyer,
        isLoading,
        error,
        refresh,
        sendFreeMessage
    };
};
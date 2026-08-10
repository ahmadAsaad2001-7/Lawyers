import { defineStore } from 'pinia';
import { ref, computed } from "vue";
import type { LawyerFilters, Lawyer, PagedResult } from "~/types/Lawyer";

export const useLawyerSearchStore = defineStore('lawyerSearch', () => {
    const filters = ref<LawyerFilters>({
        search: '',
        state: '',
        specialization: '',
        city: '',
        maxHourlyRate: undefined,
        sortBy: 'rating',
        isDescending: true
    });

    // ✅ Use Lawyer directly instead of CardLawyer
    const lawyers = ref<PagedResult<Lawyer> | null>(null);
    const isLoading = ref(false);
    const error = ref<string | null>(null);

    const currentPage = computed(() => lawyers.value?.pageNumber || 1);
    const totalPages = computed(() => lawyers.value ? Math.ceil(lawyers.value.totalCount / lawyers.value.pageSize) : 1);

    const fetchLawyers = async (pageNumber: number = 1) => {
        isLoading.value = true;
        error.value = null;

        try {
            const config = useRuntimeConfig();
            const baseUrl = config.public.apiBase.endsWith('/') ? config.public.apiBase : `${config.public.apiBase}/`;

            // ✅ Fetch directly as Lawyer
            const response = await $fetch<PagedResult<Lawyer>>(`${baseUrl}Lawyers/search`, {
                method: 'GET',
                query: {
                    ...filters.value,
                    pageNumber,
                    pageSize: 9
                }
            });

            // ✅ No mapping needed! Just assign the raw API response.
            lawyers.value = response;

        } catch (err: any) {
            console.error('Failed to fetch lawyers:', err);
            error.value = err.message || 'فشل في جلب بيانات المحامين';
            lawyers.value = null;
        } finally {
            isLoading.value = false;
        }
    };

    const resetFilters = () => {
        filters.value = {
            search: '', state: '', specialization: '', city: '',
            maxHourlyRate: undefined, sortBy: 'rating', isDescending: true
        };
        fetchLawyers(1);
    };

    return {
        filters, lawyers, isLoading, error,
        currentPage, totalPages,
        fetchLawyers, resetFilters
    };
});
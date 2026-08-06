// stores/lawyerSearch.ts
import { defineStore } from 'pinia';
import {ref} from "vue";
import type {LawyerFilters, LawyerDto, PagedResult, CardLawyer} from "~/types/Lawyer";

export const useLawyerSearchStore = defineStore('lawyerSearch', () => {
    // --- State ---
    const filters = ref<LawyerFilters>(
        {
        id: undefined,              
        hourlyRate: undefined,      
        search: '',
        state: '',
        specialization: '',
        city: '',
        maxHourlyRate: undefined,
        sortBy: 'rating',
        isDescending: true
    });

    const lawyers = ref<PagedResult<CardLawyer> | null>(null);
    const isLoading = ref(false);
    const error = ref<string | null>(null);

    // --- Actions ---
    const fetchLawyers = async (pageNumber: number = 1) => {
        isLoading.value = true;
        error.value = null;

        try {
            const config = useRuntimeConfig();
            const response = await $fetch<PagedResult<LawyerDto>>(`${config.public.apiBase}Lawyers/search`, {
                method: 'GET',
                query: {
                    ...filters.value,
                    pageNumber,
                    pageSize: 9 // Adjust based on your grid (e.g., 3x3)
                }
            });

            // Map backend DTO to frontend Card interface
            lawyers.value = {
                ...response,
                items: response.items.map((dto): CardLawyer => ({
                    id: dto.id,
                    hourlyRate: dto.hourlyRate,
                    name: dto.fullName,
                    city: dto.city,
                    officeName: 'مكتب المحامي',
                    address: dto.city,
                    phone: '0500000000',
                    expertise: dto.specialization ? [dto.specialization] : [],
                    languages: ['العربية', 'English'],
                    avatar: 'https://png.pngtree.com/background/20230809/original/pngtree-serious-man-portrait-handsome-caucasian-person-photo-picture-image_4530325.jpg',
                    rating: dto.averageRating
                }))
            };
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
            search: '',
            state: '',
            specialization: '',
            city: '',
            maxHourlyRate: undefined,
            sortBy: 'rating',
            isDescending: true
        };
        fetchLawyers(1);
    };

    return {
        filters,
        lawyers,
        isLoading,
        error,
        fetchLawyers,
        resetFilters
    };
});

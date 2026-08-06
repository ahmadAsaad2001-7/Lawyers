// types/lawyer.ts
export interface LawyerDto {
    id: number;
    fullName: string;
    avatar: string | null;  // 👈 changed
    bio: string;
    hourlyRate: number;
    specialization: string;
    city: string;
    averageRating: number;
    lawFirmName?: string;
    isVerified?: boolean;
}
export enum PostType {
    GeneralInsight = 0,
    CaseVictory = 1,
    Achievement = 2,
    LegalArticle = 3
}

export interface LawyerPostSummaryDto {
    id: number;
    title: string;
    excerpt: string | null;
    type: PostType;
    coverImageUrl: string | null;
    likeCount: number;
    isFeatured: boolean;
    createdAt: string;
}
export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
}

export interface LawyerFilters {
    id?: number;
    hourlyRate?: number;
    search: string;          // Maps to general search
    state: string;           // Maps to State
    specialization: string;  // Maps to Specialization
    city: string;            // Maps to City
    maxHourlyRate?: number;  // Maps to MaxHourlyRate
    sortBy: string;          // Maps to SortBy
    isDescending: boolean;   // Maps to IsDescending
}

// Interface tailored for your Card.vue component
export interface CardLawyer {
    id?:number;
    hourlyRate?: number;
    name: string;
    city: string;
    officeName: string;
    address: string;
    phone: string;
    expertise: string[];
    languages: string[];
    avatar: string;
    rating: number;
}
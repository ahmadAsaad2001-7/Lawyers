// types/lawyer.ts

export interface Lawyer {
    id: number;
    fullName: string;
    avatar: string | null;
    bio: string;
    hourlyRate: number;
    specialization: string;
    city: string;
    state?: string; // Added in case your backend includes it
    averageRating: number;
    lawFirmName?: string;
    isVerified?: boolean;
    officeName?: string;
    address?: string;
    phone?: string;
    languages?: string[];
    
}

export interface LawyerFilters {
    search: string;
    state: string;
    specialization: string;
    city: string;
    maxHourlyRate?: number;
    sortBy: string;
    isDescending: boolean;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
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
// Common types used across the application

// API response types
export interface ApiError {
  type: 'Validation' | 'NotFound' | 'Conflict' | 'Internal';
  message: string;
  details?: string[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Filter and pagination parameters
export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

import axios, { AxiosInstance, AxiosError, AxiosResponse, AxiosRequestConfig } from 'axios';
import { ApiError, PagedResponse, PaginationParams } from './types';

/**
 * HTTP client wrapper implementing SRP (Single Responsibility Principle)
 * Responsible only for HTTP communication and error handling
 */
class ApiClient {
  private client: AxiosInstance;

  constructor(baseURL: string = 'http://localhost:5000/api') {
    this.client = axios.create({
      baseURL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    // Request interceptor for auth token and logging
    this.client.interceptors.request.use(
      (config) => {
        // Add auth token from localStorage
        const token = localStorage.getItem('admin_access_token');
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        console.log(`🔄 API Request: ${config.method?.toUpperCase()} ${config.url}`);
        return config;
      },
      (error) => {
        console.error('❌ API Request Error:', error);
        return Promise.reject(error);
      }
    );

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => {
        console.log(`✅ API Response: ${response.status} ${response.config.url}`);
        return response;
      },
      (error: AxiosError) => {
        const apiError = this.handleError(error);
        // Don't log 404 errors for expected cases (like missing configs)
        // These are handled gracefully by the application
        if (error.response?.status !== 404) {
          console.error('❌ API Response Error:', apiError);
        }
        return Promise.reject(apiError);
      }
    );
  }

  private handleError(error: AxiosError): ApiError {
    if (error.response) {
      const data = error.response.data as any;
      return {
        type: this.mapErrorType(error.response.status),
        message: data?.message || data?.error?.message || `HTTP ${error.response.status} Error`,
        details: data?.details || data?.error?.details || []
      };
    } else if (error.request) {
      return {
        type: 'Internal',
        message: 'Network error - please check your connection',
        details: []
      };
    } else {
      return {
        type: 'Internal',
        message: error.message || 'An unexpected error occurred',
        details: []
      };
    }
  }

  private mapErrorType(status: number): ApiError['type'] {
    switch (status) {
      case 400:
        return 'Validation';
      case 404:
        return 'NotFound';
      case 409:
        return 'Conflict';
      default:
        return 'Internal';
    }
  }

  // Generic HTTP methods following DRY principle
  async get<T>(url: string, params?: Record<string, any>): Promise<T> {
    const response: AxiosResponse<T> = await this.client.get(url, { params });
    return response.data;
  }

  async post<T, D = any>(url: string, data: D, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.client.post(url, data, config);
    return response.data;
  }

  async put<T, D = any>(url: string, data: D): Promise<T> {
    const response: AxiosResponse<T> = await this.client.put(url, data);
    return response.data;
  }

  async delete<T>(url: string): Promise<T> {
    const response: AxiosResponse<T> = await this.client.delete(url);
    return response.data;
  }

  async getPaged<T>(url: string, params: PaginationParams = {}): Promise<PagedResponse<T>> {
    const queryParams = {
      pageNumber: params.pageNumber || 1,
      pageSize: params.pageSize || 10,
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortDescending !== undefined && { sortDescending: params.sortDescending }),
      // pass-through of any extra filter fields (excluding empty strings, null, and undefined)
      ...Object.fromEntries(
        Object.entries(params).filter(
          ([key, value]) =>
            !['pageNumber', 'pageSize', 'searchTerm', 'sortBy', 'sortDescending'].includes(key) &&
            value !== undefined && value !== null && value !== ''
        )
      ),
    };

    const res = await this.get<any>(`${url}/paged`, queryParams);

    // Normalize backend shape (Data/CurrentPage) to frontend shape (items/pageNumber)
    const items = res.items ?? res.data ?? [];
    const totalCount = res.totalCount ?? res.total ?? 0;
    const pageNumber = res.pageNumber ?? res.currentPage ?? queryParams.pageNumber;
    const pageSize = res.pageSize ?? queryParams.pageSize;
    const totalPages =
      res.totalPages ?? (pageSize ? Math.ceil(totalCount / pageSize) : 0);

    return {
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages,
    };
  }
}

// Singleton instance following SRP
export const apiClient = new ApiClient();

import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios'

// Determine API URL based on environment or hostname
const getApiBaseUrl = () => {
  // Use environment variable if set (highest priority)
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  
  // If accessing via domain name, try to detect backend IP from current host
  const hostname = window.location.hostname;
  if (hostname === 'rcmr.xcdify.com' || hostname === 'employee.rcmr.xcdify.com') {
    // Use VITE_BACKEND_IP env var or fallback to default
    const backendIP = import.meta.env.VITE_BACKEND_IP || '192.168.86.250';
    const backendPort = import.meta.env.VITE_BACKEND_PORT || '5001';
    return `http://${backendIP}:${backendPort}/api`;
  }
  
  // Default to relative path for local development (uses vite proxy)
  return '/api';
};

const API_BASE_URL = getApiBaseUrl();

// Create axios instance
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Token storage keys
const ACCESS_TOKEN_KEY = 'portal_access_token'
const REFRESH_TOKEN_KEY = 'portal_refresh_token'

// Token management
export const tokenStorage = {
  getAccessToken: () => localStorage.getItem(ACCESS_TOKEN_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_TOKEN_KEY),
  setTokens: (accessToken: string, refreshToken: string) => {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  },
  clearTokens: () => {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  },
}

// Request interceptor - add auth token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = tokenStorage.getAccessToken()
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor - handle token refresh
let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (error: unknown) => void
}> = []

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else if (token) {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean
    }

    // If 401 and not already retrying
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`
            }
            return apiClient(originalRequest)
          })
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      const refreshToken = tokenStorage.getRefreshToken()
      if (!refreshToken) {
        tokenStorage.clearTokens()
        window.location.href = '/login'
        return Promise.reject(error)
      }

      try {
        const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
          refreshToken,
        })

        const { accessToken, refreshToken: newRefreshToken } = response.data
        tokenStorage.setTokens(accessToken, newRefreshToken)
        processQueue(null, accessToken)

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`
        }
        return apiClient(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError, null)
        tokenStorage.clearTokens()
        window.location.href = '/login'
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient

import { useMemo } from 'react'
import type { PaginationParams } from '@/services/api/types'

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  pageCount: number
  pageNumber: number
  pageSize: number
  hasPrevious: boolean
  hasNext: boolean
}

/**
 * Client-side pagination utility for React Query data
 *
 * Provides consistent pagination pattern across all entities.
 * Benefits:
 * - Instant filtering/sorting (no network requests)
 * - Works offline
 * - Consistent UX across the app
 */
export function usePaginatedData<T extends Record<string, any>>(
  data: T[] | undefined,
  params: PaginationParams = {},
  searchFields: (keyof T)[] = []
): PaginatedResult<T> {
  return useMemo(() => {
    const safeData = data || []
    const {
      pageNumber = 1,
      pageSize = 50,
      searchTerm = '',
      sortBy,
      sortDescending = false,
    } = params
    const sortOrder = sortDescending ? 'desc' : 'asc'

    // Step 1: Filter by search term
    let filtered = safeData
    if (searchTerm && searchFields.length > 0) {
      const lowerSearch = searchTerm.toLowerCase()
      filtered = safeData.filter((item) =>
        searchFields.some((field) => {
          const value = item[field]
          if (value === null || value === undefined) return false
          return String(value).toLowerCase().includes(lowerSearch)
        })
      )
    }

    // Step 2: Sort
    let sorted = filtered
    if (sortBy) {
      sorted = [...filtered].sort((a, b) => {
        const aVal = a[sortBy]
        const bVal = b[sortBy]

        // Handle null/undefined
        if (aVal === null || aVal === undefined) return 1
        if (bVal === null || bVal === undefined) return -1

        // String comparison
        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.toLowerCase().localeCompare(bVal.toLowerCase())
          return sortOrder === 'desc' ? -comparison : comparison
        }

        // Number comparison
        if (typeof aVal === 'number' && typeof bVal === 'number') {
          return sortOrder === 'desc' ? bVal - aVal : aVal - bVal
        }

        // Date comparison
        if (aVal instanceof Date && bVal instanceof Date) {
          return sortOrder === 'desc'
            ? bVal.getTime() - aVal.getTime()
            : aVal.getTime() - bVal.getTime()
        }

        // Default string comparison
        const comparison = String(aVal).localeCompare(String(bVal))
        return sortOrder === 'desc' ? -comparison : comparison
      })
    }

    // Step 3: Paginate
    const totalCount = sorted.length
    const pageCount = Math.ceil(totalCount / pageSize)
    const start = (pageNumber - 1) * pageSize
    const items = sorted.slice(start, start + pageSize)

    return {
      items,
      totalCount,
      pageCount,
      pageNumber,
      pageSize,
      hasPrevious: pageNumber > 1,
      hasNext: pageNumber < pageCount,
    }
  }, [data, params, searchFields])
}

/**
 * Simple in-memory filtering utility
 */
export function filterData<T extends Record<string, any>>(
  data: T[],
  searchTerm: string,
  searchFields: (keyof T)[]
): T[] {
  if (!searchTerm) return data

  const lowerSearch = searchTerm.toLowerCase()
  return data.filter((item) =>
    searchFields.some((field) => {
      const value = item[field]
      if (value === null || value === undefined) return false
      return String(value).toLowerCase().includes(lowerSearch)
    })
  )
}

/**
 * Simple in-memory sorting utility
 */
export function sortData<T extends Record<string, any>>(
  data: T[],
  sortBy: keyof T,
  sortOrder: 'asc' | 'desc' = 'asc'
): T[] {
  return [...data].sort((a, b) => {
    const aVal = a[sortBy]
    const bVal = b[sortBy]

    if (aVal === null || aVal === undefined) return 1
    if (bVal === null || bVal === undefined) return -1

    if (typeof aVal === 'string' && typeof bVal === 'string') {
      const comparison = aVal.toLowerCase().localeCompare(bVal.toLowerCase())
      return sortOrder === 'desc' ? -comparison : comparison
    }

    if (typeof aVal === 'number' && typeof bVal === 'number') {
      return sortOrder === 'desc' ? bVal - aVal : aVal - bVal
    }

    const comparison = String(aVal).localeCompare(String(bVal))
    return sortOrder === 'desc' ? -comparison : comparison
  })
}

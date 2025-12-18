import { useState, useEffect } from 'react'

/**
 * Custom hook for responsive media queries
 * @param query - CSS media query string (e.g., '(min-width: 1024px)')
 * @returns boolean indicating if the query matches
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() => {
    // Check if window is available (SSR safety)
    if (typeof window !== 'undefined') {
      return window.matchMedia(query).matches
    }
    return false
  })

  useEffect(() => {
    if (typeof window === 'undefined') return

    const mediaQuery = window.matchMedia(query)

    // Set initial value
    setMatches(mediaQuery.matches)

    // Create event listener
    const handler = (event: MediaQueryListEvent) => {
      setMatches(event.matches)
    }

    // Add listener
    mediaQuery.addEventListener('change', handler)

    // Cleanup
    return () => {
      mediaQuery.removeEventListener('change', handler)
    }
  }, [query])

  return matches
}

/**
 * Hook to check if the viewport is desktop size (>= 1024px)
 */
export function useIsDesktop(): boolean {
  return useMediaQuery('(min-width: 1024px)')
}

/**
 * Hook to check if the viewport is tablet size (>= 768px)
 */
export function useIsTablet(): boolean {
  return useMediaQuery('(min-width: 768px)')
}

/**
 * Hook to check if the viewport is mobile size (< 768px)
 */
export function useIsMobile(): boolean {
  return !useMediaQuery('(min-width: 768px)')
}

import { useState, useEffect, useCallback } from 'react'

const STORAGE_KEY = 'sidebar-collapsed'

export const useSidebarCollapse = () => {
  const [isCollapsed, setIsCollapsed] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false

    try {
      const saved = localStorage.getItem(STORAGE_KEY)
      if (saved !== null) {
        return JSON.parse(saved)
      }
    } catch (e) {
      console.error('Failed to load sidebar collapse state:', e)
    }
    return false
  })

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(isCollapsed))
    } catch (e) {
      console.error('Failed to save sidebar collapse state:', e)
    }
  }, [isCollapsed])

  const toggle = useCallback(() => {
    setIsCollapsed((prev) => !prev)
  }, [])

  const collapse = useCallback(() => {
    setIsCollapsed(true)
  }, [])

  const expand = useCallback(() => {
    setIsCollapsed(false)
  }, [])

  return {
    isCollapsed,
    toggle,
    collapse,
    expand,
  }
}


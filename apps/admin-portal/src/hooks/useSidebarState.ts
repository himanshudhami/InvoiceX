import { useState, useEffect, useCallback } from 'react'

const STORAGE_KEY = 'sidebar-expanded-groups'
const DEFAULT_EXPANDED = ['People & Payroll']

export const useSidebarState = () => {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(() => {
    if (typeof window === 'undefined') return new Set(DEFAULT_EXPANDED)

    try {
      const saved = localStorage.getItem(STORAGE_KEY)
      if (saved) {
        return new Set(JSON.parse(saved))
      }
    } catch (e) {
      console.error('Failed to load sidebar state:', e)
    }
    return new Set(DEFAULT_EXPANDED)
  })

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(Array.from(expandedGroups)))
    } catch (e) {
      console.error('Failed to save sidebar state:', e)
    }
  }, [expandedGroups])

  const toggleGroup = useCallback((groupName: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(groupName)) {
        next.delete(groupName)
      } else {
        next.add(groupName)
      }
      return next
    })
  }, [])

  const expandGroup = useCallback((groupName: string) => {
    setExpandedGroups((prev) => {
      if (prev.has(groupName)) return prev
      const next = new Set(prev)
      next.add(groupName)
      return next
    })
  }, [])

  const collapseGroup = useCallback((groupName: string) => {
    setExpandedGroups((prev) => {
      if (!prev.has(groupName)) return prev
      const next = new Set(prev)
      next.delete(groupName)
      return next
    })
  }, [])

  const isExpanded = useCallback(
    (groupName: string) => expandedGroups.has(groupName),
    [expandedGroups]
  )

  return {
    expandedGroups,
    toggleGroup,
    expandGroup,
    collapseGroup,
    isExpanded,
  }
}

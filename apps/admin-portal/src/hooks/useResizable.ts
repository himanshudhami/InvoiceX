import { useState, useCallback, useEffect, useRef } from 'react'

interface UseResizableOptions {
  /** Initial width in pixels */
  initialWidth: number
  /** Minimum width in pixels */
  minWidth?: number
  /** Maximum width in pixels */
  maxWidth?: number
  /** Storage key for persisting width (optional) */
  storageKey?: string
  /** Direction of resize handle */
  direction?: 'left' | 'right'
}

interface UseResizableReturn {
  /** Current width in pixels */
  width: number
  /** Whether currently dragging */
  isDragging: boolean
  /** Props to spread on the resize handle element */
  handleProps: {
    onMouseDown: (e: React.MouseEvent) => void
    onTouchStart: (e: React.TouchEvent) => void
    style: React.CSSProperties
  }
  /** Reset to initial width */
  reset: () => void
}

/**
 * Hook for creating resizable panels/drawers
 * Follows SRP: Only handles resize logic, not UI concerns
 */
export const useResizable = ({
  initialWidth,
  minWidth = 320,
  maxWidth = window.innerWidth - 100,
  storageKey,
  direction = 'left',
}: UseResizableOptions): UseResizableReturn => {
  // Load persisted width or use initial
  const getInitialWidth = () => {
    if (storageKey) {
      const stored = localStorage.getItem(storageKey)
      if (stored) {
        const parsed = parseInt(stored, 10)
        if (!isNaN(parsed) && parsed >= minWidth && parsed <= maxWidth) {
          return parsed
        }
      }
    }
    return initialWidth
  }

  const [width, setWidth] = useState(getInitialWidth)
  const [isDragging, setIsDragging] = useState(false)
  const startXRef = useRef(0)
  const startWidthRef = useRef(0)

  // Persist width changes
  useEffect(() => {
    if (storageKey && !isDragging) {
      localStorage.setItem(storageKey, width.toString())
    }
  }, [width, storageKey, isDragging])

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      if (!isDragging) return

      const deltaX = direction === 'left'
        ? startXRef.current - e.clientX
        : e.clientX - startXRef.current

      const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidthRef.current + deltaX))
      setWidth(newWidth)
    },
    [isDragging, minWidth, maxWidth, direction]
  )

  const handleTouchMove = useCallback(
    (e: TouchEvent) => {
      if (!isDragging || !e.touches[0]) return

      const deltaX = direction === 'left'
        ? startXRef.current - e.touches[0].clientX
        : e.touches[0].clientX - startXRef.current

      const newWidth = Math.min(maxWidth, Math.max(minWidth, startWidthRef.current + deltaX))
      setWidth(newWidth)
    },
    [isDragging, minWidth, maxWidth, direction]
  )

  const handleMouseUp = useCallback(() => {
    setIsDragging(false)
  }, [])

  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
      document.addEventListener('touchmove', handleTouchMove)
      document.addEventListener('touchend', handleMouseUp)
      // Prevent text selection while dragging
      document.body.style.userSelect = 'none'
      document.body.style.cursor = 'ew-resize'
    }

    return () => {
      document.removeEventListener('mousemove', handleMouseMove)
      document.removeEventListener('mouseup', handleMouseUp)
      document.removeEventListener('touchmove', handleTouchMove)
      document.removeEventListener('touchend', handleMouseUp)
      document.body.style.userSelect = ''
      document.body.style.cursor = ''
    }
  }, [isDragging, handleMouseMove, handleTouchMove, handleMouseUp])

  const startDrag = useCallback(
    (clientX: number) => {
      startXRef.current = clientX
      startWidthRef.current = width
      setIsDragging(true)
    },
    [width]
  )

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault()
      startDrag(e.clientX)
    },
    [startDrag]
  )

  const handleTouchStart = useCallback(
    (e: React.TouchEvent) => {
      if (e.touches[0]) {
        startDrag(e.touches[0].clientX)
      }
    },
    [startDrag]
  )

  const reset = useCallback(() => {
    setWidth(initialWidth)
    if (storageKey) {
      localStorage.removeItem(storageKey)
    }
  }, [initialWidth, storageKey])

  return {
    width,
    isDragging,
    handleProps: {
      onMouseDown: handleMouseDown,
      onTouchStart: handleTouchStart,
      style: { cursor: 'ew-resize' },
    },
    reset,
  }
}

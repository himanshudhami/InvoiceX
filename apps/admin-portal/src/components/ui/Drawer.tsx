import { Fragment, ReactNode, useMemo } from 'react'
import { Dialog, Transition } from '@headlessui/react'
import { cn } from '@/lib/utils'
import { useResizable } from '@/hooks/useResizable'

interface DrawerProps {
  isOpen: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Preset size - ignored if resizable is true */
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '2/3' | 'full'
  showCloseButton?: boolean
  /** Enable drag-to-resize functionality */
  resizable?: boolean
  /** Storage key for persisting resized width */
  resizeStorageKey?: string
  /** Minimum width when resizable (default: 320px) */
  minWidth?: number
  /** Maximum width when resizable (default: screen width - 100px) */
  maxWidth?: number
}

// Tailwind max-width classes for preset sizes
const sizeClasses: Record<string, string> = {
  sm: 'max-w-sm',      // 384px
  md: 'max-w-md',      // 448px
  lg: 'max-w-lg',      // 512px
  xl: 'max-w-xl',      // 576px
  '2xl': 'max-w-2xl',  // 672px
  '3xl': 'max-w-3xl',  // 768px
  '2/3': 'max-w-[66vw]', // 2/3 of viewport
  full: 'max-w-full',
}

// Default widths in pixels for resizable mode
const defaultWidths: Record<string, number> = {
  sm: 384,
  md: 448,
  lg: 512,
  xl: 576,
  '2xl': 672,
  '3xl': 768,
  '2/3': Math.round(window.innerWidth * 0.66),
  full: window.innerWidth - 100,
}

export const Drawer = ({
  isOpen,
  onClose,
  title,
  children,
  size = 'lg',
  showCloseButton = true,
  resizable = false,
  resizeStorageKey,
  minWidth = 320,
  maxWidth,
}: DrawerProps) => {
  const initialWidth = defaultWidths[size] || defaultWidths.lg
  const effectiveMaxWidth = maxWidth || (typeof window !== 'undefined' ? window.innerWidth - 100 : 1200)

  const { width, isDragging, handleProps } = useResizable({
    initialWidth,
    minWidth,
    maxWidth: effectiveMaxWidth,
    storageKey: resizeStorageKey,
    direction: 'left',
  })

  // Memoize panel style for resizable mode
  const panelStyle = useMemo(() => {
    if (!resizable) return undefined
    return { width: `${width}px` }
  }, [resizable, width])

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        {/* Backdrop */}
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-25" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-hidden">
          <div className="absolute inset-0 overflow-hidden">
            <div className="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16">
              <Transition.Child
                as={Fragment}
                enter="transform transition ease-in-out duration-500"
                enterFrom="translate-x-full"
                enterTo="translate-x-0"
                leave="transform transition ease-in-out duration-500"
                leaveFrom="translate-x-0"
                leaveTo="translate-x-full"
              >
                <Dialog.Panel
                  className={cn(
                    "pointer-events-auto w-screen relative",
                    !resizable && sizeClasses[size]
                  )}
                  style={panelStyle}
                >
                  {/* Resize Handle - only shown when resizable */}
                  {resizable && (
                    <div
                      {...handleProps}
                      className={cn(
                        "absolute left-0 top-0 bottom-0 w-1 hover:w-1.5 transition-all",
                        "bg-transparent hover:bg-blue-400",
                        "group flex items-center justify-center",
                        isDragging && "w-1.5 bg-blue-500"
                      )}
                      title="Drag to resize"
                    >
                      {/* Visual grip indicator */}
                      <div className={cn(
                        "absolute left-0 top-1/2 -translate-y-1/2 -translate-x-1/2",
                        "w-4 h-12 rounded-full",
                        "bg-gray-300 hover:bg-blue-400 transition-colors",
                        "opacity-0 group-hover:opacity-100",
                        "flex items-center justify-center",
                        isDragging && "opacity-100 bg-blue-500"
                      )}>
                        <div className="flex flex-col gap-0.5">
                          <div className="w-0.5 h-0.5 bg-white rounded-full" />
                          <div className="w-0.5 h-0.5 bg-white rounded-full" />
                          <div className="w-0.5 h-0.5 bg-white rounded-full" />
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="flex h-full flex-col overflow-y-scroll bg-white shadow-xl">
                    {/* Header */}
                    <div className="bg-gray-50 px-4 py-6 sm:px-6">
                      <div className="flex items-center justify-between">
                        <Dialog.Title className="text-base font-semibold leading-6 text-gray-900">
                          {title}
                        </Dialog.Title>
                        {showCloseButton && (
                          <div className="ml-3 flex h-7 items-center">
                            <button
                              type="button"
                              className="rounded-md bg-gray-50 text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                              onClick={onClose}
                            >
                              <span className="sr-only">Close panel</span>
                              <svg
                                className="h-6 w-6"
                                fill="none"
                                viewBox="0 0 24 24"
                                strokeWidth={1.5}
                                stroke="currentColor"
                              >
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  d="M6 18L18 6M6 6l12 12"
                                />
                              </svg>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Content */}
                    <div className="relative flex-1 px-4 py-6 sm:px-6">
                      {children}
                    </div>
                  </div>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </div>
      </Dialog>
    </Transition>
  )
}

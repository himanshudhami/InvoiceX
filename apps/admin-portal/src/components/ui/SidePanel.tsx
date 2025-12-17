import { Fragment, ReactNode } from 'react'
import { Transition } from '@headlessui/react'
import { cn } from '@/lib/utils'
import { X } from 'lucide-react'

interface SidePanelProps {
  isOpen: boolean
  onClose: () => void
  title?: ReactNode
  children: ReactNode
  width?: 'md' | 'lg' | 'xl' | '2xl'
  showCloseButton?: boolean
  header?: ReactNode
}

const widthClasses = {
  md: 'w-[400px]',
  lg: 'w-[500px]',
  xl: 'w-[600px]',
  '2xl': 'w-[700px]',
}

export const SidePanel = ({
  isOpen,
  onClose,
  title,
  children,
  width = 'lg',
  showCloseButton = true,
  header,
}: SidePanelProps) => {
  return (
    <Transition show={isOpen} as={Fragment}>
      <div className="fixed inset-y-0 right-0 z-40 flex">
        <Transition.Child
          as={Fragment}
          enter="transform transition ease-in-out duration-300"
          enterFrom="translate-x-full"
          enterTo="translate-x-0"
          leave="transform transition ease-in-out duration-300"
          leaveFrom="translate-x-0"
          leaveTo="translate-x-full"
        >
          <div
            className={cn(
              'relative flex h-full flex-col bg-white shadow-2xl border-l border-gray-200',
              widthClasses[width]
            )}
          >
            {/* Header */}
            {header ? (
              header
            ) : title ? (
              <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
                  {showCloseButton && (
                    <button
                      type="button"
                      className="rounded-md p-1 text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                      onClick={onClose}
                    >
                      <span className="sr-only">Close panel</span>
                      <X className="h-5 w-5" />
                    </button>
                  )}
                </div>
              </div>
            ) : showCloseButton ? (
              <div className="absolute top-3 right-3 z-10">
                <button
                  type="button"
                  className="rounded-md p-1 text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  onClick={onClose}
                >
                  <span className="sr-only">Close panel</span>
                  <X className="h-5 w-5" />
                </button>
              </div>
            ) : null}

            {/* Content */}
            <div className="flex-1 overflow-y-auto">{children}</div>
          </div>
        </Transition.Child>
      </div>
    </Transition>
  )
}

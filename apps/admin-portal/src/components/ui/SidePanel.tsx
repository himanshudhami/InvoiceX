import { Fragment, ReactNode } from 'react'
import { Dialog, Transition } from '@headlessui/react'
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
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black/25" />
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
                    'pointer-events-auto h-full flex flex-col bg-white shadow-2xl border-l border-gray-200',
                    widthClasses[width]
                  )}
                >
                  {/* Header */}
                  {header ? (
                    header
                  ) : title ? (
                    <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
                      <div className="flex items-center justify-between">
                        <Dialog.Title className="text-lg font-semibold text-gray-900">
                          {title}
                        </Dialog.Title>
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
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </div>
      </Dialog>
    </Transition>
  )
}

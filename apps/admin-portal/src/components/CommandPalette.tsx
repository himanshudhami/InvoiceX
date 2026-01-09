import { Fragment, useEffect, useMemo, useRef, useState, type ComponentType, type KeyboardEvent } from 'react'
import { Dialog, Transition } from '@headlessui/react'
import { Search } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { getIconStyle } from '@/lib/iconPalette'
import { navigationGroups } from '@/data/navigation'

type CommandItem = {
  name: string
  href: string
  group?: string
  icon: ComponentType<{ className?: string }>
}

export const CommandPalette = () => {
  const navigate = useNavigate()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(0)

  const items = useMemo<CommandItem[]>(() => {
    return navigationGroups.flatMap((item) => {
      if (item.type === 'single') {
        return [{ name: item.name, href: item.href, icon: item.icon }]
      }
      return item.items.map((subItem) => ({
        name: subItem.name,
        href: subItem.href,
        icon: subItem.icon,
        group: item.name,
      }))
    })
  }, [])

  const filteredItems = useMemo(() => {
    const trimmed = query.trim().toLowerCase()
    if (!trimmed) return items
    return items.filter((item) => {
      const haystack = `${item.name} ${item.group ?? ''}`.toLowerCase()
      return haystack.includes(trimmed)
    })
  }, [items, query])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.ctrlKey && event.key.toLowerCase() === 't') {
        event.preventDefault()
        setIsOpen((prev) => !prev)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  useEffect(() => {
    if (isOpen) {
      setQuery('')
      setActiveIndex(0)
      requestAnimationFrame(() => inputRef.current?.focus())
    }
  }, [isOpen])

  useEffect(() => {
    if (filteredItems.length === 0) {
      setActiveIndex(-1)
      return
    }
    setActiveIndex(0)
  }, [query, filteredItems.length])

  const closePalette = () => setIsOpen(false)

  const handleNavigate = (href: string) => {
    navigate(href)
    closePalette()
  }

  const handleInputKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((prev) => Math.min(prev + 1, filteredItems.length - 1))
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((prev) => Math.max(prev - 1, 0))
    }
    if (event.key === 'Enter' && activeIndex >= 0) {
      event.preventDefault()
      const item = filteredItems[activeIndex]
      if (item) handleNavigate(item.href)
    }
  }

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-[60]" onClose={closePalette}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-200"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-150"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-start justify-center px-4 py-12">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-200"
              enterFrom="opacity-0 translate-y-4 scale-95"
              enterTo="opacity-100 translate-y-0 scale-100"
              leave="ease-in duration-150"
              leaveFrom="opacity-100 translate-y-0 scale-100"
              leaveTo="opacity-0 translate-y-4 scale-95"
            >
              <Dialog.Panel className="w-full max-w-2xl overflow-hidden rounded-2xl bg-white shadow-2xl ring-1 ring-slate-900/10">
                <div className="flex items-center gap-3 border-b border-slate-200 px-5 py-4">
                  <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-sky-100 via-emerald-100 to-rose-100">
                    <Search className="h-5 w-5 text-slate-700" />
                  </div>
                  <input
                    ref={inputRef}
                    value={query}
                    onChange={(event) => setQuery(event.target.value)}
                    onKeyDown={handleInputKeyDown}
                    placeholder="Search menu items..."
                    className="w-full bg-transparent text-lg text-slate-900 placeholder:text-slate-400 focus:outline-none"
                  />
                  <div className="rounded-full bg-slate-100 px-2 py-1 text-xs font-medium text-slate-500">
                    Ctrl + T
                  </div>
                </div>

                <div className="max-h-[60vh] overflow-y-auto py-2">
                  {filteredItems.length === 0 ? (
                    <div className="px-5 py-10 text-center text-sm text-slate-500">
                      No results. Try a different search.
                    </div>
                  ) : (
                    filteredItems.map((item, index) => {
                      const Icon = item.icon
                      const palette = getIconStyle(`${item.group ?? 'root'}-${item.name}`)
                      const isActive = index === activeIndex
                      return (
                        <button
                          key={`${item.href}-${item.name}`}
                          type="button"
                          onClick={() => handleNavigate(item.href)}
                          onMouseEnter={() => setActiveIndex(index)}
                          className={cn(
                            'flex w-full items-center gap-4 px-5 py-3 text-left transition-colors',
                            isActive ? 'bg-slate-100' : 'hover:bg-slate-50'
                          )}
                        >
                          <div className={cn('flex h-9 w-9 items-center justify-center rounded-lg', palette.bg)}>
                            <Icon className={cn('h-5 w-5', palette.text)} />
                          </div>
                          <div className="flex flex-1 flex-col">
                            <span className="text-sm font-semibold text-slate-900">{item.name}</span>
                            {item.group && (
                              <span className="text-xs text-slate-500">{item.group}</span>
                            )}
                          </div>
                        </button>
                      )
                    })
                  )}
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  )
}

"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, Loader2, Search } from "lucide-react"
import { cn } from "@/lib/utils"

export interface TypeaheadOption<T = unknown> {
  id: string
  label: string
  description?: string
  subLabel?: string
  data: T
}

interface AsyncTypeaheadProps<T> {
  placeholder?: string
  className?: string
  disabled?: boolean
  minChars?: number
  debounceMs?: number
  onSearch: (query: string) => Promise<TypeaheadOption<T>[]>
  onSelect: (option: TypeaheadOption<T>) => void
  renderOption?: (option: TypeaheadOption<T>, active: boolean, selected: boolean) => React.ReactNode
  emptyMessage?: string
  loadingMessage?: string
}

/**
 * AsyncTypeahead - A reusable async typeahead component
 * Follows SRP: Only handles async search and selection
 */
export function AsyncTypeahead<T>({
  placeholder = "Search...",
  className,
  disabled = false,
  minChars = 2,
  debounceMs = 300,
  onSearch,
  onSelect,
  renderOption,
  emptyMessage = "No results found",
  loadingMessage = "Searching...",
}: AsyncTypeaheadProps<T>) {
  const [query, setQuery] = React.useState("")
  const [options, setOptions] = React.useState<TypeaheadOption<T>[]>([])
  const [isLoading, setIsLoading] = React.useState(false)
  const [isOpen, setIsOpen] = React.useState(false)
  const [hasSearched, setHasSearched] = React.useState(false)

  const abortControllerRef = React.useRef<AbortController | null>(null)

  // Debounced search effect
  React.useEffect(() => {
    if (query.length < minChars) {
      setOptions([])
      setHasSearched(false)
      return
    }

    // Cancel previous request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
    }
    abortControllerRef.current = new AbortController()

    setIsLoading(true)
    const timer = setTimeout(async () => {
      try {
        const results = await onSearch(query)
        setOptions(results)
        setHasSearched(true)
      } catch (error) {
        if ((error as Error).name !== 'AbortError') {
          console.error('Search failed:', error)
          setOptions([])
        }
      } finally {
        setIsLoading(false)
      }
    }, debounceMs)

    return () => {
      clearTimeout(timer)
    }
  }, [query, minChars, debounceMs, onSearch])

  const handleSelect = (optionId: string) => {
    const option = options.find(o => o.id === optionId)
    if (option) {
      onSelect(option)
      setQuery("")
      setOptions([])
      setIsOpen(false)
      setHasSearched(false)
    }
  }

  const defaultRenderOption = (option: TypeaheadOption<T>, active: boolean, selected: boolean) => (
    <div className="flex flex-col">
      <span className={cn("block truncate", selected && "font-semibold")}>
        {option.label}
      </span>
      {option.description && (
        <span className="text-xs text-gray-500 truncate">
          {option.description}
        </span>
      )}
      {option.subLabel && (
        <span className="text-xs text-gray-400 truncate">
          {option.subLabel}
        </span>
      )}
    </div>
  )

  return (
    <HeadlessCombobox
      value=""
      onChange={handleSelect}
      disabled={disabled}
    >
      <div className={cn("relative", className)}>
        <div className="relative">
          <span className="absolute inset-y-0 left-0 flex items-center pl-3">
            {isLoading ? (
              <Loader2 className="size-4 text-gray-400 animate-spin" aria-hidden="true" />
            ) : (
              <Search className="size-4 text-gray-400" aria-hidden="true" />
            )}
          </span>
          <ComboboxInput
            className={cn(
              "w-full pl-10 pr-3 py-2 text-sm border border-gray-300 rounded-md",
              "focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
              "disabled:cursor-not-allowed disabled:opacity-50",
              "bg-white text-gray-900"
            )}
            value={query}
            onChange={(event) => {
              setQuery(event.target.value)
              setIsOpen(true)
            }}
            onFocus={() => setIsOpen(true)}
            onBlur={() => setTimeout(() => setIsOpen(false), 200)}
            placeholder={placeholder}
          />
        </div>

        {isOpen && (query.length >= minChars || options.length > 0) && (
          <ComboboxOptions
            static
            className={cn(
              "absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md",
              "bg-white py-1 text-sm shadow-lg ring-1 ring-black/5",
              "focus:outline-none"
            )}
          >
            {isLoading ? (
              <div className="relative cursor-default select-none px-4 py-3 text-gray-500 flex items-center gap-2">
                <Loader2 className="size-4 animate-spin" />
                {loadingMessage}
              </div>
            ) : options.length === 0 && hasSearched ? (
              <div className="relative cursor-default select-none px-4 py-3 text-gray-500">
                {emptyMessage}
              </div>
            ) : (
              options.map((option) => (
                <ComboboxOption
                  key={option.id}
                  value={option.id}
                  className={({ active, selected }) =>
                    cn(
                      "relative cursor-pointer select-none py-2 pl-10 pr-4",
                      active ? "bg-blue-50 text-blue-900" : "text-gray-900",
                      selected && "font-medium"
                    )
                  }
                >
                  {({ active, selected }) => (
                    <>
                      {renderOption ? renderOption(option, active, selected) : defaultRenderOption(option, active, selected)}
                      {selected && (
                        <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-blue-600">
                          <CheckIcon className="size-4" aria-hidden="true" />
                        </span>
                      )}
                    </>
                  )}
                </ComboboxOption>
              ))
            )}
          </ComboboxOptions>
        )}
      </div>
    </HeadlessCombobox>
  )
}

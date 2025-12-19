"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon, PackageSearch } from "lucide-react"
import { cn } from "@/lib/utils"
import { useProductsPaged } from "@/features/products/hooks/useProducts"

interface ProductSelectProps {
  companyId?: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
  error?: string
}

const PAGE_SIZE = 20
const QUERY_DEBOUNCE_MS = 200

export function ProductSelect({
  companyId,
  value,
  onChange,
  placeholder = "Select product...",
  className,
  disabled = false,
  error,
}: ProductSelectProps) {
  const [query, setQuery] = React.useState("")
  const [isOpen, setIsOpen] = React.useState(false)
  const [pageNumber, setPageNumber] = React.useState(1)

  // Debounce the search term to avoid hammering the API
  const [debouncedQuery, setDebouncedQuery] = React.useState(query)
  React.useEffect(() => {
    const handle = setTimeout(() => setDebouncedQuery(query), QUERY_DEBOUNCE_MS)
    return () => clearTimeout(handle)
  }, [query])

  const { data, isLoading, isFetching } = useProductsPaged({
    companyId,
    searchTerm: debouncedQuery || undefined,
    pageNumber,
    pageSize: PAGE_SIZE,
  })

  const products = data?.items || []
  const totalPages = data?.totalPages || 1

  const selectedProduct = products.find((p) => p.id === value)

  const handleChange = (newValue: string) => {
    onChange(newValue)
    setIsOpen(false)
  }

  const handleLoadMore = () => {
    if (pageNumber < totalPages) {
      setPageNumber((prev) => prev + 1)
    }
  }

  const hasMore = pageNumber < totalPages

  return (
    <div className={className}>
      <HeadlessCombobox
        value={value}
        onChange={handleChange}
        disabled={disabled}
      >
        {({ open }) => (
          <div className="relative">
            <div className="relative">
              <ComboboxInput
                className={cn(
                  "w-full px-3 py-2 pr-10 text-sm border rounded-md",
                  "focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent",
                  "disabled:cursor-not-allowed disabled:opacity-50",
                  "bg-white text-gray-900",
                  error ? "border-red-500" : "border-gray-300"
                )}
                displayValue={() => selectedProduct ? selectedProduct.name : ""}
                onChange={(event) => {
                  setQuery(event.target.value)
                  setIsOpen(true)
                  setPageNumber(1) // reset to first page on new search
                }}
                onFocus={() => setIsOpen(true)}
                onClick={() => setIsOpen(true)}
                onBlur={() => setIsOpen(false)}
                placeholder={placeholder}
              />
              <ComboboxButton
                className="absolute inset-y-0 right-0 flex items-center pr-2"
                onClick={() => setIsOpen((prev) => !prev)}
              >
                <ChevronDownIcon className="size-4 text-gray-400" aria-hidden="true" />
              </ComboboxButton>
            </div>

            <ComboboxOptions
              static={isOpen || open}
              className={cn(
                "absolute z-50 mt-1 max-h-72 w-full overflow-auto rounded-md",
                "bg-white py-1 text-sm shadow-lg ring-1 ring-black/5",
                "focus:outline-none"
              )}
            >
              {isLoading && (
                <div className="px-4 py-2 text-gray-500">Loading products...</div>
              )}
              {!isLoading && products.length === 0 && (
                <div className="px-4 py-2 text-gray-500">
                  {debouncedQuery ? "No products match your search." : "No products available."}
                </div>
              )}
              {products.map((product) => (
                <ComboboxOption
                  key={product.id}
                  value={product.id}
                  className={({ active, selected }) =>
                    cn(
                      "relative cursor-pointer select-none py-2 pl-10 pr-4",
                      active ? "bg-blue-50 text-blue-900" : "text-gray-900",
                      selected && "font-medium"
                    )
                  }
                >
                  {({ selected }) => (
                    <>
                      <span className="absolute inset-y-0 left-0 flex items-center pl-3">
                        <PackageSearch className="size-4 text-gray-400" aria-hidden="true" />
                      </span>
                      <div className="flex flex-col">
                        <span className={cn("block truncate", selected && "font-semibold")}>
                          {product.name}
                        </span>
                        <span className="text-xs text-gray-500 truncate">
                          {product.unitPrice != null ? `Price: ${product.unitPrice}` : ''}
                        </span>
                      </div>
                      {selected && (
                        <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-blue-600">
                          <CheckIcon className="size-4" aria-hidden="true" />
                        </span>
                      )}
                    </>
                  )}
                </ComboboxOption>
              ))}
              {hasMore && (
                <button
                  type="button"
                  onClick={handleLoadMore}
                  disabled={isFetching}
                  className="w-full text-left px-4 py-2 text-primary hover:bg-primary/5 disabled:text-gray-400 disabled:cursor-not-allowed"
                >
                  {isFetching ? "Loading..." : "Load more"}
                </button>
              )}
            </ComboboxOptions>
          </div>
        )}
      </HeadlessCombobox>
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
    </div>
  )
}

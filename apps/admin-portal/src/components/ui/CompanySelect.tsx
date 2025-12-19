"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon, Building } from "lucide-react"
import { cn } from "@/lib/utils"

export interface Company {
  id: string
  name: string
}

interface CompanySelectProps {
  companies: Company[]
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
  error?: string
  showAllOption?: boolean
  allOptionLabel?: string
}

export function CompanySelect({
  companies,
  value,
  onChange,
  placeholder = "Select company...",
  className,
  disabled = false,
  error,
  showAllOption = false,
  allOptionLabel = "All Companies",
}: CompanySelectProps) {
  const [query, setQuery] = React.useState("")
  const [isOpen, setIsOpen] = React.useState(false)

  const selectedCompany = companies.find((c) => c.id === value)

  const filteredCompanies =
    query === ""
      ? companies
      : companies.filter((company) => {
          return company.name.toLowerCase().includes(query.toLowerCase())
        })

  const handleChange = (newValue: string) => {
    onChange(newValue === "__all__" ? "" : newValue)
    setIsOpen(false)
  }

  const displayValue = () => {
    if (showAllOption && !value) return allOptionLabel
    return selectedCompany?.name || ""
  }

  return (
    <div className={className}>
      <HeadlessCombobox
        value={showAllOption && !value ? "__all__" : value}
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
                displayValue={displayValue}
                onChange={(event) => {
                  setQuery(event.target.value)
                  setIsOpen(true)
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
                "absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md",
                "bg-white py-1 text-sm shadow-lg ring-1 ring-black/5",
                "focus:outline-none"
              )}
            >
              {showAllOption && (
                <ComboboxOption
                  value="__all__"
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
                        <Building className="size-4 text-gray-400" aria-hidden="true" />
                      </span>
                      <span className={cn("block truncate", selected && "font-semibold")}>
                        {allOptionLabel}
                      </span>
                      {selected && (
                        <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-blue-600">
                          <CheckIcon className="size-4" aria-hidden="true" />
                        </span>
                      )}
                    </>
                  )}
                </ComboboxOption>
              )}
              {filteredCompanies.length === 0 && query !== "" ? (
                <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
                  No companies found.
                </div>
              ) : (
                filteredCompanies.map((company) => (
                  <ComboboxOption
                    key={company.id}
                    value={company.id}
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
                          <Building className="size-4 text-gray-400" aria-hidden="true" />
                        </span>
                        <span className={cn("block truncate", selected && "font-semibold")}>
                          {company.name}
                        </span>
                        {selected && (
                          <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-blue-600">
                            <CheckIcon className="size-4" aria-hidden="true" />
                          </span>
                        )}
                      </>
                    )}
                  </ComboboxOption>
                ))
              )}
            </ComboboxOptions>
          </div>
        )}
      </HeadlessCombobox>
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
    </div>
  )
}

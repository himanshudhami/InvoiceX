"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon, User, Building } from "lucide-react"
import { cn } from "@/lib/utils"

export interface CustomerOption {
  id: string
  name: string
  companyName?: string
  email?: string
  phone?: string
}

interface CustomerSelectProps {
  customers: CustomerOption[]
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
  error?: string
  showAllOption?: boolean
  allOptionLabel?: string
}

export function CustomerSelect({
  customers,
  value,
  onChange,
  placeholder = "Select customer...",
  className,
  disabled = false,
  error,
  showAllOption = false,
  allOptionLabel = "All Customers",
}: CustomerSelectProps) {
  const [query, setQuery] = React.useState("")
  const [isOpen, setIsOpen] = React.useState(false)

  const selectedCustomer = customers.find((c) => c.id === value)

  const filteredCustomers =
    query === ""
      ? customers
      : customers.filter((customer) => {
          const name = customer.name.toLowerCase()
          const company = (customer.companyName || "").toLowerCase()
          const email = (customer.email || "").toLowerCase()
          const q = query.toLowerCase()
          return name.includes(q) || company.includes(q) || email.includes(q)
        })

  const handleChange = (newValue: string) => {
    onChange(newValue === "__all__" ? "" : newValue)
    setIsOpen(false)
  }

  const displayValue = () => {
    if (showAllOption && !value) return allOptionLabel
    return selectedCustomer?.name || ""
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
              {filteredCustomers.length === 0 ? (
                <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
                  {query ? "No customers found." : "No customers available."}
                </div>
              ) : (
                filteredCustomers.map((customer) => (
                  <ComboboxOption
                    key={customer.id}
                    value={customer.id}
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
                          <User className="size-4 text-gray-400" aria-hidden="true" />
                        </span>
                        <div className="flex flex-col">
                          <span className={cn("block truncate", selected && "font-semibold")}>
                            {customer.name}
                          </span>
                          {(customer.companyName || customer.email) && (
                            <span className="text-xs text-gray-500 truncate">
                              {customer.companyName || customer.email}
                            </span>
                          )}
                        </div>
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

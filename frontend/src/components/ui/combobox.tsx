"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon } from "lucide-react"
import { cn } from "@/lib/utils"

export interface ComboboxOption {
  value: string
  label: string
  description?: string
}

interface ComboboxProps {
  options: ComboboxOption[]
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
}

export function Combobox({
  options,
  value,
  onChange,
  placeholder = "Select...",
  className,
  disabled = false,
}: ComboboxProps) {
  const [query, setQuery] = React.useState("")

  const selectedOption = options.find((opt) => opt.value === value) || null

  const filteredOptions =
    query === ""
      ? options
      : options.filter((option) => {
          const searchText = `${option.value} ${option.label} ${option.description || ""}`.toLowerCase()
          return searchText.includes(query.toLowerCase())
        })

  return (
    <HeadlessCombobox
      value={value}
      onChange={onChange}
      disabled={disabled}
    >
      <div className={cn("relative", className)}>
        <div className="relative">
          <ComboboxInput
            className={cn(
              "w-full px-3 py-2 pr-10 text-sm border border-gray-300 rounded-md",
              "focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent",
              "disabled:cursor-not-allowed disabled:opacity-50",
              "bg-white"
            )}
            displayValue={(val: string) => {
              const opt = options.find((o) => o.value === val)
              return opt ? `${opt.value} - ${opt.label}` : val
            }}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={placeholder}
          />
          <ComboboxButton className="absolute inset-y-0 right-0 flex items-center pr-2">
            <ChevronDownIcon className="size-4 text-gray-400" aria-hidden="true" />
          </ComboboxButton>
        </div>

        <ComboboxOptions
          className={cn(
            "absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md",
            "bg-white py-1 text-sm shadow-lg ring-1 ring-black/5",
            "focus:outline-none"
          )}
        >
          {filteredOptions.length === 0 && query !== "" ? (
            <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
              No results found.
            </div>
          ) : (
            filteredOptions.map((option) => (
              <ComboboxOption
                key={option.value}
                value={option.value}
                className={({ active, selected }) =>
                  cn(
                    "relative cursor-default select-none py-2 pl-3 pr-9",
                    active ? "bg-primary/10 text-primary" : "text-gray-900",
                    selected && "font-medium"
                  )
                }
              >
                {({ selected }) => (
                  <>
                    <div className="flex flex-col">
                      <span className={cn("block truncate", selected && "font-semibold")}>
                        {option.value}
                      </span>
                      {option.label && (
                        <span className="text-xs text-gray-500 truncate">
                          {option.label}
                          {option.description && ` - ${option.description}`}
                        </span>
                      )}
                    </div>
                    {selected && (
                      <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-primary">
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
    </HeadlessCombobox>
  )
}

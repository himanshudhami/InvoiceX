"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon, User } from "lucide-react"
import { cn } from "@/lib/utils"

export interface Employee {
  id: string
  firstName?: string
  lastName?: string
  employeeName?: string
  email?: string
  companyId?: string
}

interface EmployeeSelectProps {
  employees: Employee[]
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  disabled?: boolean
  error?: string
}

export function EmployeeSelect({
  employees,
  value,
  onChange,
  placeholder = "Select employee...",
  className,
  disabled = false,
  error,
}: EmployeeSelectProps) {
  const [query, setQuery] = React.useState("")
  const [isOpen, setIsOpen] = React.useState(false)

  const getEmployeeName = (emp: Employee) => {
    if (emp.employeeName) return emp.employeeName
    if (emp.firstName && emp.lastName) return `${emp.firstName} ${emp.lastName}`
    if (emp.firstName) return emp.firstName
    return emp.email || emp.id
  }

  const selectedEmployee = employees.find((emp) => emp.id === value)

  const filteredEmployees =
    query === ""
      ? employees
      : employees.filter((emp) => {
          const name = getEmployeeName(emp).toLowerCase()
          const email = (emp.email || "").toLowerCase()
          return name.includes(query.toLowerCase()) || email.includes(query.toLowerCase())
        })

  return (
    <div className={className}>
      <HeadlessCombobox
        value={value}
        onChange={(val) => {
          onChange(val)
          setIsOpen(false)
        }}
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
                displayValue={() => selectedEmployee ? getEmployeeName(selectedEmployee) : ""}
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
              {filteredEmployees.length === 0 ? (
                <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
                  {query ? "No employees found." : "No employees available."}
                </div>
              ) : (
                filteredEmployees.map((emp) => (
                  <ComboboxOption
                    key={emp.id}
                    value={emp.id}
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
                            {getEmployeeName(emp)}
                          </span>
                          {emp.email && (
                            <span className="text-xs text-gray-500 truncate">
                              {emp.email}
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

"use client"

import * as React from "react"
import { Combobox as HeadlessCombobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/react"
import { CheckIcon, ChevronDownIcon, Building2, CreditCard } from "lucide-react"
import { cn } from "@/lib/utils"
import { BankAccount } from "@/services/api/types"
import { bankAccountService } from "@/services/api/finance/banking/bankAccountService"

interface BankAccountSelectProps {
  companyId?: string
  value: string
  onChange: (value: string, account?: BankAccount) => void
  placeholder?: string
  className?: string
  disabled?: boolean
  error?: string
  label?: string
  required?: boolean
  autoSelectPrimary?: boolean
}

export function BankAccountSelect({
  companyId,
  value,
  onChange,
  placeholder = "Select bank account...",
  className,
  disabled = false,
  error,
  label,
  required = false,
  autoSelectPrimary = true,
}: BankAccountSelectProps) {
  const [query, setQuery] = React.useState("")
  const [isOpen, setIsOpen] = React.useState(false)
  const [accounts, setAccounts] = React.useState<BankAccount[]>([])
  const [loading, setLoading] = React.useState(false)

  // Load bank accounts when companyId changes
  React.useEffect(() => {
    const loadAccounts = async () => {
      if (!companyId) {
        setAccounts([])
        return
      }

      setLoading(true)
      try {
        const data = await bankAccountService.getByCompanyId(companyId)
        const activeAccounts = data.filter(a => a.isActive)
        setAccounts(activeAccounts)

        // Auto-select primary account if no value selected and autoSelectPrimary is true
        if (autoSelectPrimary && !value && activeAccounts.length > 0) {
          const primary = activeAccounts.find(a => a.isPrimary) || activeAccounts[0]
          onChange(primary.id, primary)
        }
      } catch (err) {
        console.error('Failed to load bank accounts:', err)
        setAccounts([])
      } finally {
        setLoading(false)
      }
    }

    loadAccounts()
  }, [companyId])

  const selectedAccount = accounts.find((a) => a.id === value)

  const filteredAccounts =
    query === ""
      ? accounts
      : accounts.filter((account) => {
          const searchStr = `${account.accountName} ${account.bankName} ${account.accountNumber}`.toLowerCase()
          return searchStr.includes(query.toLowerCase())
        })

  const handleChange = (newValue: string) => {
    const account = accounts.find(a => a.id === newValue)
    onChange(newValue, account)
    setIsOpen(false)
  }

  const displayValue = () => {
    if (selectedAccount) {
      return `${selectedAccount.accountName} - ${selectedAccount.bankName}`
    }
    return ""
  }

  const formatAccountDisplay = (account: BankAccount) => {
    const masked = account.accountNumber.slice(-4).padStart(account.accountNumber.length, '*')
    return (
      <div className="flex flex-col">
        <span className="font-medium">{account.accountName}</span>
        <span className="text-xs text-gray-500">
          {account.bankName} - ****{account.accountNumber.slice(-4)} ({account.currency})
        </span>
      </div>
    )
  }

  return (
    <div className={className}>
      {label && (
        <label className="block text-sm font-medium text-gray-700 mb-2">
          <Building2 className="w-4 h-4 inline mr-1" />
          {label}
          {required && <span className="text-red-500 ml-1">*</span>}
        </label>
      )}
      <HeadlessCombobox
        value={value}
        onChange={handleChange}
        disabled={disabled || loading}
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
                onBlur={() => setTimeout(() => setIsOpen(false), 200)}
                placeholder={loading ? "Loading accounts..." : placeholder}
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
              {loading ? (
                <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
                  Loading bank accounts...
                </div>
              ) : filteredAccounts.length === 0 ? (
                <div className="relative cursor-default select-none px-4 py-2 text-gray-500">
                  {query !== "" ? "No accounts found." : "No bank accounts available."}
                </div>
              ) : (
                filteredAccounts.map((account) => (
                  <ComboboxOption
                    key={account.id}
                    value={account.id}
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
                          <CreditCard className="size-4 text-gray-400" aria-hidden="true" />
                        </span>
                        <div className="flex flex-col">
                          <div className="flex items-center gap-2">
                            <span className={cn("block truncate", selected && "font-semibold")}>
                              {account.accountName}
                            </span>
                            {account.isPrimary && (
                              <span className="text-xs bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded">
                                Primary
                              </span>
                            )}
                          </div>
                          <span className="text-xs text-gray-500">
                            {account.bankName} - ****{account.accountNumber.slice(-4)} ({account.currency})
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
                ))
              )}
            </ComboboxOptions>
          </div>
        )}
      </HeadlessCombobox>
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
      {!companyId && !disabled && (
        <p className="text-yellow-600 text-xs mt-1">Select a company first to load bank accounts</p>
      )}
    </div>
  )
}

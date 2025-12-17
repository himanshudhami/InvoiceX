import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatINR } from '@/lib/currency'
import { useTaxSlabs } from '@/features/payroll/hooks/useTaxConfiguration'

interface TaxCalculatorProps {
  className?: string
}

export const TaxCalculator = ({ className }: TaxCalculatorProps) => {
  const [annualIncome, setAnnualIncome] = useState<number>(0)
  const [regime, setRegime] = useState<'old' | 'new'>('new')
  const [financialYear] = useState<string>('2024-25')
  const [deductions, setDeductions] = useState<number>(0)

  const { data: taxSlabs = [] } = useTaxSlabs(regime, financialYear)

  // Calculate tax
  const calculateTax = () => {
    if (annualIncome <= 0) return { tax: 0, surcharge: 0, cess: 0, total: 0 }

    const standardDeduction = regime === 'new' ? 75000 : 50000
    const taxableIncome = Math.max(0, annualIncome - standardDeduction - deductions)

    // Calculate tax by slabs
    let remainingIncome = taxableIncome
    let tax = 0

    for (const slab of taxSlabs.sort((a, b) => a.minIncome - b.minIncome)) {
      if (remainingIncome <= 0) break

      const slabMin = slab.minIncome
      const slabMax = slab.maxIncome || Number.MAX_SAFE_INTEGER
      const slabRange = slabMax - slabMin

      const taxableInSlab = Math.min(remainingIncome, slabRange)
      if (taxableInSlab > 0) {
        tax += (taxableInSlab * slab.rate) / 100
        remainingIncome -= taxableInSlab
      }
    }

    // Apply Section 87A rebate
    let rebate = 0
    if (regime === 'new' && taxableIncome <= 700000) {
      rebate = Math.min(tax, 25000)
    } else if (regime === 'old' && taxableIncome <= 500000) {
      rebate = Math.min(tax, 12500)
    }
    const taxAfterRebate = Math.max(0, tax - rebate)

    // Calculate surcharge
    let surcharge = 0
    if (taxableIncome > 5000000) {
      if (taxableIncome <= 10000000) {
        surcharge = (taxAfterRebate * 10) / 100
      } else if (taxableIncome <= 20000000) {
        surcharge = (taxAfterRebate * 15) / 100
      } else if (taxableIncome <= 50000000) {
        surcharge = (taxAfterRebate * 25) / 100
      } else {
        surcharge = (taxAfterRebate * 25) / 100 // Capped for new regime
      }
    }

    // Calculate cess (4%)
    const cess = ((taxAfterRebate + surcharge) * 4) / 100

    const total = taxAfterRebate + surcharge + cess

    return { tax, rebate, taxAfterRebate, surcharge, cess, total }
  }

  const result = calculateTax()
  const monthlyTds = result.total / 12

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Tax Calculator</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Annual Income (₹)
          </label>
          <input
            type="number"
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={annualIncome}
            onChange={(e) => setAnnualIncome(parseFloat(e.target.value) || 0)}
            placeholder="Enter annual income"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Tax Regime
          </label>
          <select
            className="w-full rounded-md border border-gray-300 px-3 py-2"
            value={regime}
            onChange={(e) => setRegime(e.target.value as 'old' | 'new')}
          >
            <option value="new">New Regime</option>
            <option value="old">Old Regime</option>
          </select>
        </div>

        {regime === 'old' && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Total Deductions (₹)
            </label>
            <input
              type="number"
              className="w-full rounded-md border border-gray-300 px-3 py-2"
              value={deductions}
              onChange={(e) => setDeductions(parseFloat(e.target.value) || 0)}
              placeholder="80C, 80D, HRA, etc."
            />
          </div>
        )}

        {annualIncome > 0 && (
          <div className="bg-gray-50 p-4 rounded-lg space-y-2">
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Standard Deduction:</span>
              <span className="text-sm font-medium">
                {formatINR(regime === 'new' ? 75000 : 50000)}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Taxable Income:</span>
              <span className="text-sm font-medium">
                {formatINR(Math.max(0, annualIncome - (regime === 'new' ? 75000 : 50000) - deductions))}
              </span>
            </div>
            {result.rebate > 0 && (
              <div className="flex justify-between text-green-600">
                <span className="text-sm">Section 87A Rebate:</span>
                <span className="text-sm font-medium">-{formatINR(result.rebate)}</span>
              </div>
            )}
            <div className="flex justify-between border-t pt-2 mt-2">
              <span className="text-sm font-semibold">Annual Tax:</span>
              <span className="text-sm font-bold">{formatINR(result.total)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm font-semibold">Monthly TDS:</span>
              <span className="text-sm font-bold text-blue-600">{formatINR(monthlyTds)}</span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}





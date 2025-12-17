import { useMemo } from 'react'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { useMonthlyExpenses, useYearlyExpenseSummary, useDepartmentExpenses } from '@/hooks/api/useExpenseAnalytics'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useInvoices } from '@/hooks/api/useInvoices'
import { usePayrollExpenses } from '@/hooks/api/usePayrollExpenses'
import { useSubscriptionMonthlyExpenses } from '@/hooks/api/useSubscriptions'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Calendar, TrendingUp, Users, Building, DollarSign, PieChart, Activity } from 'lucide-react'
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  BarChart,
  Bar,
} from 'recharts'

const ExpenseDashboard = () => {
  const currentYear = new Date().getFullYear()
  const currentMonth = new Date().getMonth() + 1

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      year: parseAsInteger.withDefault(currentYear),
      month: parseAsInteger.withDefault(currentMonth),
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  // Derive selectedMonth: if 0, it means "all months"
  const selectedMonth = urlState.month === 0 ? undefined : urlState.month
  const selectedYear = urlState.year
  const selectedCompanyId = urlState.company

  const { data: companies = [] } = useCompanies()
  const { data: invoices = [] } = useInvoices()
  const { data: salaryTransactions = [] } = usePayrollExpenses({
    companyId: selectedCompanyId || undefined,
    year: selectedYear,
    month: selectedMonth,
  })
  const { data: subscriptionExpenses = [] } = useSubscriptionMonthlyExpenses(selectedYear, selectedMonth, selectedCompanyId || undefined)

  // Get selected company name for filtering
  const selectedCompany = useMemo(() => {
    if (!selectedCompanyId) return null
    return companies.find(c => c.id === selectedCompanyId)
  }, [selectedCompanyId, companies])

  // Filter invoices by company
  const filteredInvoices = useMemo(() => {
    if (!selectedCompany) return invoices
    return invoices.filter(inv => inv.companyId === selectedCompany.id)
  }, [invoices, selectedCompany])

  // Filter salary transactions by company
  const filteredSalaryTransactions = useMemo(() => {
    if (!selectedCompany) return salaryTransactions
    return salaryTransactions.filter(tx => tx.companyId === selectedCompany.id)
  }, [salaryTransactions, selectedCompany])

  // Use filtered transactions for expense analytics
  const { data: monthlyExpenses = [], isLoading: isLoadingMonthly } = useMonthlyExpenses(selectedYear, filteredSalaryTransactions)
  const { data: yearlyData, isLoading: isLoadingYearly } = useYearlyExpenseSummary(selectedYear, filteredSalaryTransactions)
  const { data: departmentExpenses = [], isLoading: isLoadingDepartments } = useDepartmentExpenses(selectedYear, selectedMonth, filteredSalaryTransactions)

  // Fixed FX: 1 USD = 86 INR
  const toInr = (amount: number, currency?: string) => {
    if (!currency || currency.toUpperCase() === 'INR') return amount
    if (currency.toUpperCase() === 'USD') return amount * 86
    return amount // fallback: treat as INR if unknown
  }

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount)

  const getMonthName = (monthNum: number) => 
    new Date(selectedYear, monthNum - 1, 1).toLocaleString('default', { month: 'long' })

  const currentMonthData = monthlyExpenses.find(m => m.month === currentMonth)
  const hasMonthly = monthlyExpenses && monthlyExpenses.length > 0
  const topDepartments = [...departmentExpenses]
    .sort((a, b) => (b.totalGrossSalary || 0) - (a.totalGrossSalary || 0))
    .slice(0, 5)

  // Income (invoices) per month for selected year
  // Income is calculated based on when invoices were marked as paid (updatedAt)
  const monthlyIncome = useMemo(() => {
    const map = new Map<number, number>()
    filteredInvoices.forEach((inv) => {
      // Only count paid invoices
      if (!inv.status || inv.status.toLowerCase() !== 'paid') return
      
      // Use updatedAt to determine when the invoice was marked as paid
      // Fall back to createdAt if updatedAt is missing (should be rare for paid invoices)
      const dateStr = inv.updatedAt || inv.createdAt
      if (!dateStr) return // Skip invoices without a valid date
      
      const d = new Date(dateStr)
      // Check if date is valid
      if (isNaN(d.getTime())) return
      
      // Filter by year based on updatedAt
      if (d.getFullYear() !== selectedYear) return
      
      const m = d.getMonth() + 1
      const inInr = toInr(inv.totalAmount || 0, inv.currency)
      map.set(m, (map.get(m) || 0) + inInr)
    })
    return Array.from({ length: 12 }, (_, i) => ({
      month: i + 1,
      income: map.get(i + 1) || 0,
    }))
  }, [filteredInvoices, selectedYear])

  // Expenses per month (gross salary)
  const monthlyExpenseGross = useMemo(() => {
    const map = new Map<number, number>()
    filteredSalaryTransactions.forEach((tx) => {
      const m = tx.salaryMonth
      const y = tx.salaryYear
      if (y !== selectedYear) return
      map.set(m, (map.get(m) || 0) + (tx.grossSalary || 0))
    })
    return Array.from({ length: 12 }, (_, i) => ({
      month: i + 1,
      expense: map.get(i + 1) || 0,
    }))
  }, [filteredSalaryTransactions, selectedYear])

  // Subscription expenses per month
  const monthlySubscriptionExpenses = useMemo(() => {
    const map = new Map<number, number>()
    subscriptionExpenses.forEach((exp) => {
      if (exp.year === selectedYear) {
        map.set(exp.month, (map.get(exp.month) || 0) + (exp.totalCostInInr || 0))
      }
    })
    return Array.from({ length: 12 }, (_, i) => ({
      month: i + 1,
      subscriptionExpense: map.get(i + 1) || 0,
    }))
  }, [subscriptionExpenses, selectedYear])

  // Combined income vs expense by month
  const incomeVsExpense = monthlyIncome.map((row) => {
    const expenseRow = monthlyExpenseGross.find((e) => e.month === row.month)
    const subscriptionRow = monthlySubscriptionExpenses.find((e) => e.month === row.month)
    const totalExpense = (expenseRow?.expense || 0) + (subscriptionRow?.subscriptionExpense || 0)
    return {
      month: row.month,
      income: row.income,
      expense: expenseRow?.expense || 0,
      subscriptionExpense: subscriptionRow?.subscriptionExpense || 0,
      totalExpense,
      net: row.income - totalExpense,
    }
  })

  // Forecast next month using average of last 3 months with actual data
  // Only calculate forecast when viewing the current year
  // For expenses: Use actual salary data (salaries are paid every month)
  // For income: Use actual income data from paid invoices
  const forecast = useMemo(() => {
    // Only show forecast for current year
    if (selectedYear !== currentYear) {
      return { income: 0, expense: 0 }
    }

    // Need at least 1 month of history to forecast
    if (currentMonth <= 1) {
      return { income: 0, expense: 0 }
    }

    // Get the last 3 months before current month (or as many as available)
    const last3MonthNumbers: number[] = []
    const startMonth = Math.max(1, currentMonth - 3)
    for (let i = startMonth; i < currentMonth; i++) {
      last3MonthNumbers.push(i)
    }

    // Get expense data from last 3 months (salaries are recurring monthly expenses)
    // Only include months with actual expense data (> 0)
    const last3ExpenseData = monthlyExpenseGross
      .filter((e) => last3MonthNumbers.includes(e.month) && e.expense > 0)
    
    // Get subscription expense data from last 3 months
    const last3SubscriptionData = monthlySubscriptionExpenses
      .filter((e) => last3MonthNumbers.includes(e.month) && e.subscriptionExpense > 0)
    
    // Get income data from last 3 months
    // Only include months with actual income data (> 0)
    const last3IncomeData = monthlyIncome
      .filter((i) => last3MonthNumbers.includes(i.month) && i.income > 0)

    // Calculate forecast: average of available months with actual data
    // For expenses: Average of months with salary data (salaries are paid every month)
    // For income: Average of months with income data (may vary)
    const forecastIncome = last3IncomeData.length > 0
      ? last3IncomeData.reduce((s, r) => s + r.income, 0) / last3IncomeData.length
      : 0
    
    const forecastSalaryExpense = last3ExpenseData.length > 0
      ? last3ExpenseData.reduce((s, r) => s + r.expense, 0) / last3ExpenseData.length
      : 0
    
    const forecastSubscriptionExpense = last3SubscriptionData.length > 0
      ? last3SubscriptionData.reduce((s, r) => s + r.subscriptionExpense, 0) / last3SubscriptionData.length
      : 0
    
    const forecastExpense = forecastSalaryExpense + forecastSubscriptionExpense

    return {
      income: forecastIncome,
      expense: forecastExpense,
    }
  }, [selectedYear, currentYear, currentMonth, monthlyExpenseGross, monthlySubscriptionExpenses, monthlyIncome])

  // Detail lists for selected month
  // Only show paid invoices that were marked as paid (updatedAt) in the selected month/year
  const monthInvoices = filteredInvoices.filter((inv) => {
    // Only include paid invoices
    if (!inv.status || inv.status.toLowerCase() !== 'paid') return false
    
    // Use updatedAt to determine when the invoice was marked as paid
    // Fall back to createdAt if updatedAt is missing (should be rare for paid invoices)
    const dateStr = inv.updatedAt || inv.createdAt
    if (!dateStr) return false // Skip invoices without a valid date
    
    const d = new Date(dateStr)
    // Check if date is valid
    if (isNaN(d.getTime())) return false
    
    // Filter by year and month based on updatedAt
    return d.getFullYear() === selectedYear && (!selectedMonth || d.getMonth() + 1 === selectedMonth)
  })
  const monthSalaryTx = filteredSalaryTransactions.filter(
    (tx) => tx.salaryYear === selectedYear && (!selectedMonth || tx.salaryMonth === selectedMonth)
  )

  const years = Array.from({ length: 5 }, (_, i) => currentYear - 2 + i)
  const months = Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: new Date(selectedYear, i, 1).toLocaleString('default', { month: 'long' })
  }))

  if (isLoadingMonthly || isLoadingYearly || isLoadingDepartments) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Employee Expense Dashboard</h1>
          <p className="text-gray-600 mt-1">Track and analyze employee expenses across time periods</p>
        </div>
        
        {/* Filters */}
        <div className="flex flex-wrap gap-3">
          <CompanyFilterDropdown
            value={selectedCompanyId}
            onChange={(val) => setUrlState({ company: val || '' })}
          />
          
          <select
            value={selectedYear}
            onChange={(e) => setUrlState({ year: parseInt(e.target.value) })}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
          >
            {years.map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
          
          <select
            value={urlState.month}
            onChange={(e) => {
              const value = e.target.value
              setUrlState({ month: value ? parseInt(value) : 0 })
            }}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value={0}>All Months</option>
            {months.map((month) => (
              <option key={month.value} value={month.value}>
                {month.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0 p-3 bg-green-100 rounded-full">
              <DollarSign className="w-6 h-6 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Gross Salary</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(yearlyData?.totalGrossSalary || 0)}
              </p>
              <p className="text-xs text-gray-500">Year {selectedYear}</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0 p-3 bg-blue-100 rounded-full">
              <TrendingUp className="w-6 h-6 text-blue-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Net Salary Paid</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(yearlyData?.totalNetSalary || 0)}
              </p>
              <p className="text-xs text-gray-500">After deductions</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0 p-3 bg-purple-100 rounded-full">
              <Building className="w-6 h-6 text-purple-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Employer PF</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(yearlyData?.totalPFEmployer || 0)}
              </p>
              <p className="text-xs text-gray-500">Company contribution</p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0 p-3 bg-orange-100 rounded-full">
              <Users className="w-6 h-6 text-orange-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">This Month</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatCurrency(currentMonthData?.totalGrossSalary || 0)}
              </p>
              <p className="text-xs text-gray-500">{currentMonthData?.employeeCount || 0} employees</p>
            </div>
          </div>
        </div>
      </div>

      {/* Monthly Breakdown Chart */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="bg-white rounded-lg shadow xl:col-span-2">
          <div className="border-b border-gray-200 px-6 py-4 flex items-center">
            <Calendar className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Monthly Trend - {selectedYear}</h3>
          </div>
          <div className="p-6" style={{ minHeight: 320 }}>
            {hasMonthly ? (
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={monthlyExpenses}>
                  <defs>
                    <linearGradient id="gross" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#2563eb" stopOpacity={0.4} />
                      <stop offset="95%" stopColor="#2563eb" stopOpacity={0.05} />
                    </linearGradient>
                    <linearGradient id="net" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#22c55e" stopOpacity={0.35} />
                      <stop offset="95%" stopColor="#22c55e" stopOpacity={0.05} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis
                    dataKey="month"
                    tickFormatter={(m) => getMonthName(m).slice(0, 3)}
                    stroke="#9ca3af"
                  />
                  <YAxis stroke="#9ca3af" tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    labelFormatter={(m) => getMonthName(Number(m))}
                  />
                  <Legend />
                  <Area
                    type="monotone"
                    dataKey="totalGrossSalary"
                    name="Gross Salary"
                    stroke="#2563eb"
                    fillOpacity={1}
                    fill="url(#gross)"
                  />
                  <Area
                    type="monotone"
                    dataKey="totalNetSalary"
                    name="Net Salary"
                    stroke="#22c55e"
                    fillOpacity={1}
                    fill="url(#net)"
                  />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <div className="text-center py-8 text-gray-500">No salary data found for {selectedYear}</div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-lg shadow">
          <div className="border-b border-gray-200 px-6 py-4 flex items-center">
            <Activity className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Deductions vs Employer PF</h3>
          </div>
          <div className="p-6" style={{ minHeight: 320 }}>
            {hasMonthly ? (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={monthlyExpenses}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis
                    dataKey="month"
                    tickFormatter={(m) => getMonthName(m).slice(0, 3)}
                    stroke="#9ca3af"
                  />
                  <YAxis stroke="#9ca3af" tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    labelFormatter={(m) => getMonthName(Number(m))}
                  />
                  <Legend />
                  <Bar dataKey="totalDeductions" name="Deductions" fill="#f97316" />
                  <Bar dataKey="totalPFEmployer" name="Employer PF" fill="#8b5cf6" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="text-center py-8 text-gray-500">No deduction data</div>
            )}
          </div>
        </div>
      </div>

      {/* Income vs Expense */}
      <div className="bg-white rounded-lg shadow">
        <div className="border-b border-gray-200 px-6 py-4 flex items-center">
          <TrendingUp className="w-5 h-5 text-gray-500 mr-2" />
          <h3 className="text-lg font-medium text-gray-900">Income vs Expenses - {selectedYear}</h3>
        </div>
        <div className="p-6" style={{ minHeight: 320 }}>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={incomeVsExpense}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
              <XAxis
                dataKey="month"
                tickFormatter={(m) => getMonthName(m).slice(0, 3)}
                stroke="#9ca3af"
              />
              <YAxis stroke="#9ca3af" tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`} />
              <Tooltip
                formatter={(value: number) => formatCurrency(value)}
                labelFormatter={(m) => getMonthName(Number(m))}
              />
              <Legend />
              <Bar dataKey="income" name="Income (paid invoices)" fill="#22c55e" />
              <Bar dataKey="expense" name="Expenses (salary gross)" fill="#ef4444" />
              <Bar dataKey="subscriptionExpense" name="Subscription Expenses" fill="#f59e0b" />
            </BarChart>
          </ResponsiveContainer>
          <div className="mt-3 text-sm text-gray-700">
            Forecast next month — Income: <span className="font-semibold">{formatCurrency(forecast.income)}</span>, Expenses:{' '}
            <span className="font-semibold">{formatCurrency(forecast.expense)}</span>
          </div>
        </div>
      </div>

      {/* Monthly Breakdown Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="border-b border-gray-200 px-6 py-4">
          <div className="flex items-center">
            <Calendar className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Monthly Breakdown - {selectedYear}</h3>
          </div>
        </div>
        <div className="p-6">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Month</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Employees</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Gross Salary</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Net Salary</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Deductions</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Employer PF</th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Subscription Spend</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {monthlyExpenses.map((month) => (
                  <tr key={month.month} className={month.month === currentMonth ? 'bg-blue-50' : ''}>
                    <td
                      className="px-6 py-4 whitespace-nowrap cursor-pointer hover:bg-blue-50"
                      onClick={() => setUrlState({ month: month.month })}
                    >
                      <div className="flex items-center">
                        {month.month === currentMonth && (
                          <div className="w-2 h-2 bg-blue-500 rounded-full mr-2"></div>
                        )}
                        <span className="font-medium text-gray-900">{getMonthName(month.month)}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                      {month.employeeCount}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                      {formatCurrency(month.totalGrossSalary)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                      {formatCurrency(month.totalNetSalary)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-red-600">
                      {formatCurrency(month.totalDeductions)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                      {formatCurrency(month.totalPFEmployer)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                      {formatCurrency(monthlySubscriptionExpenses.find((e) => e.month === month.month)?.subscriptionExpense || 0)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {monthlyExpenses.length === 0 && (
            <div className="text-center py-8 text-gray-500">
              No salary data found for {selectedYear}
            </div>
          )}
        </div>
      </div>

      {/* Department Breakdown */}
      <div className="bg-white rounded-lg shadow">
        <div className="border-b border-gray-200 px-6 py-4">
          <div className="flex items-center">
            <PieChart className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">
              Department Breakdown
              {selectedMonth && ` - ${getMonthName(selectedMonth)} ${selectedYear}`}
            </h3>
          </div>
        </div>
        <div className="p-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
            {departmentExpenses.map((dept) => (
              <div key={dept.department} className="border border-gray-200 rounded-lg p-4">
                <h4 className="font-medium text-gray-900 mb-3">{dept.department || 'Unassigned'}</h4>
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-gray-500">Employees:</span>
                    <span className="font-medium">{dept.employeeCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Total Gross:</span>
                    <span className="font-medium">{formatCurrency(dept.totalGrossSalary)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Total Net:</span>
                    <span className="font-medium">{formatCurrency(dept.totalNetSalary)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Avg Salary:</span>
                    <span className="font-medium">{formatCurrency(dept.averageSalary)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Employer PF:</span>
                    <span className="font-medium">{formatCurrency(dept.totalPFEmployer)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
          {departmentExpenses.length === 0 && (
            <div className="text-center py-8 text-gray-500">
              No department data found for the selected period
            </div>
          )}
        </div>
      </div>

      {/* Top Departments & Key Stats */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow">
          <div className="border-b border-gray-200 px-6 py-4 flex items-center">
            <Building className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Top Departments by Gross Salary</h3>
          </div>
          <div className="p-6">
            {topDepartments.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Department</th>
                      <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Employees</th>
                      <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Gross</th>
                      <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Net</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {topDepartments.map((dept) => (
                      <tr key={dept.department}>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">{dept.department || 'Unassigned'}</td>
                        <td className="px-4 py-3 text-right text-sm text-gray-900">{dept.employeeCount}</td>
                        <td className="px-4 py-3 text-right text-sm text-gray-900">{formatCurrency(dept.totalGrossSalary)}</td>
                        <td className="px-4 py-3 text-right text-sm text-gray-900">{formatCurrency(dept.totalNetSalary)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">No department data available.</div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-lg shadow">
          <div className="border-b border-gray-200 px-6 py-4 flex items-center">
            <TrendingUp className="w-5 h-5 text-gray-500 mr-2" />
            <h3 className="text-lg font-medium text-gray-900">Highlights</h3>
          </div>
          <div className="p-6 space-y-3 text-sm text-gray-800">
            <div className="flex justify-between">
              <span>Total Employees (current month)</span>
              <span className="font-semibold">{currentMonthData?.employeeCount || 0}</span>
            </div>
            <div className="flex justify-between">
              <span>Avg Gross / Employee (current month)</span>
              <span className="font-semibold">
                {currentMonthData && currentMonthData.employeeCount
                  ? formatCurrency(currentMonthData.totalGrossSalary / currentMonthData.employeeCount)
                  : formatCurrency(0)}
              </span>
            </div>
            <div className="flex justify-between">
              <span>Avg Net / Employee (current month)</span>
              <span className="font-semibold">
                {currentMonthData && currentMonthData.employeeCount
                  ? formatCurrency(currentMonthData.totalNetSalary / currentMonthData.employeeCount)
                  : formatCurrency(0)}
              </span>
            </div>
            <div className="flex justify-between">
              <span>Yearly PF (Employer)</span>
              <span className="font-semibold">{formatCurrency(yearlyData?.totalPFEmployer || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span>Yearly Total Deductions</span>
              <span className="font-semibold">{formatCurrency(yearlyData?.totalDeductions || 0)}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Month Details */}
      {selectedMonth && (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg shadow">
            <div className="border-b border-gray-200 px-6 py-4 flex items-center justify-between">
              <div className="flex items-center">
                <DollarSign className="w-5 h-5 text-gray-500 mr-2" />
                <h3 className="text-lg font-medium text-gray-900">Income - {getMonthName(selectedMonth)} {selectedYear}</h3>
              </div>
              <div className="text-sm text-gray-600">Rows: {monthInvoices.length}</div>
            </div>
            <div className="p-6">
              <div className="flex justify-between text-sm mb-3">
                <span className="text-gray-600">Total Income</span>
                <span className="font-semibold">
                  {formatCurrency(
                    monthInvoices.reduce((s, i) => s + toInr(i.totalAmount || 0, i.currency), 0)
                  )}
                </span>
              </div>
              <div className="max-h-72 overflow-y-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50 text-xs text-gray-500">
                    <tr>
                      <th className="px-3 py-2 text-left">Invoice</th>
                      <th className="px-3 py-2 text-right">Amount</th>
                      <th className="px-3 py-2 text-right">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 text-sm">
                    {monthInvoices.map((inv) => (
                      <tr key={inv.id}>
                        <td className="px-3 py-2">
                          <div className="font-medium text-gray-900">{inv.invoiceNumber}</div>
                          <div className="text-gray-500 text-xs">{new Date(inv.invoiceDate).toLocaleDateString()}</div>
                        </td>
                        <td className="px-3 py-2 text-right">{formatCurrency(toInr(inv.totalAmount || 0, inv.currency))}</td>
                        <td className="px-3 py-2 text-right text-xs">
                          <span className="px-2 py-1 rounded-full bg-gray-100 text-gray-700 capitalize">{inv.status || 'pending'}</span>
                        </td>
                      </tr>
                    ))}
                    {monthInvoices.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-3 py-4 text-center text-gray-500">No invoices</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow">
            <div className="border-b border-gray-200 px-6 py-4 flex items-center justify-between">
              <div className="flex items-center">
                <Users className="w-5 h-5 text-gray-500 mr-2" />
                <h3 className="text-lg font-medium text-gray-900">Expenses (Salaries) - {getMonthName(selectedMonth)} {selectedYear}</h3>
              </div>
              <div className="text-sm text-gray-600">Rows: {monthSalaryTx.length}</div>
            </div>
            <div className="p-6">
              <div className="flex justify-between text-sm mb-3">
                <span className="text-gray-600">Total Expense</span>
                <span className="font-semibold">{formatCurrency(monthSalaryTx.reduce((s, t) => s + (t.grossSalary || 0), 0))}</span>
              </div>
              <div className="max-h-72 overflow-y-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50 text-xs text-gray-500">
                    <tr>
                      <th className="px-3 py-2 text-left">Employee</th>
                      <th className="px-3 py-2 text-right">Gross</th>
                      <th className="px-3 py-2 text-right">Net</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 text-sm">
                    {monthSalaryTx.map((tx) => (
                      <tr key={tx.id}>
                        <td className="px-3 py-2">
                          <div className="font-medium text-gray-900">{tx.employee?.employeeName || '—'}</div>
                          <div className="text-gray-500 text-xs">{tx.employee?.department || 'No department'}</div>
                        </td>
                        <td className="px-3 py-2 text-right">{formatCurrency(tx.grossSalary || 0)}</td>
                        <td className="px-3 py-2 text-right text-green-700">{formatCurrency(tx.netSalary || 0)}</td>
                      </tr>
                    ))}
                    {monthSalaryTx.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-3 py-4 text-center text-gray-500">No salary records</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default ExpenseDashboard

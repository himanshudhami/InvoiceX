import { useMemo } from 'react'
import { EmployeeSalaryTransaction } from '@/services/api/types'

type MonthlyExpense = {
  month: number
  year: number
  totalGrossSalary: number
  totalNetSalary: number
  totalPFEmployer: number
  totalDeductions: number
  employeeCount: number
}

type YearlyExpenseSummary = {
  year: number
  totalGrossSalary: number
  totalNetSalary: number
  totalPFEmployer: number
  totalDeductions: number
  monthlyBreakdown: MonthlyExpense[]
}

type DepartmentExpense = {
  department: string
  employeeCount: number
  totalGrossSalary: number
  totalNetSalary: number
  totalPFEmployer: number
  totalDeductions: number
  averageSalary: number
}

const clampNumber = (n: number | undefined | null) => (Number.isFinite(n as number) ? Number(n) : 0)

const buildMonthly = (txns: EmployeeSalaryTransaction[], year: number): MonthlyExpense[] => {
  const map = new Map<number, { gross: number; net: number; pfEmployer: number; deductions: number; employees: Set<string> }>()
  txns.filter(t => t.salaryYear === year).forEach(t => {
    const key = t.salaryMonth
    const entry = map.get(key) || { gross: 0, net: 0, pfEmployer: 0, deductions: 0, employees: new Set<string>() }
    entry.gross += clampNumber(t.grossSalary)
    entry.net += clampNumber(t.netSalary)
    entry.pfEmployer += clampNumber(t.pfEmployer)
    const deductions = clampNumber(t.pfEmployee) + clampNumber(t.pt) + clampNumber(t.incomeTax) + clampNumber(t.otherDeductions)
    entry.deductions += deductions
    if (t.employeeId) entry.employees.add(t.employeeId)
    map.set(key, entry)
  })

  const result: MonthlyExpense[] = []
  for (let m = 1; m <= 12; m++) {
    const entry = map.get(m)
    if (entry) {
      result.push({
        month: m,
        year,
        totalGrossSalary: entry.gross,
        totalNetSalary: entry.net,
        totalPFEmployer: entry.pfEmployer,
        totalDeductions: entry.deductions,
        employeeCount: entry.employees.size,
      })
    }
  }
  return result.sort((a, b) => a.month - b.month)
}

const buildYearly = (monthly: MonthlyExpense[], year: number): YearlyExpenseSummary => {
  const totalGrossSalary = monthly.reduce((s, m) => s + m.totalGrossSalary, 0)
  const totalNetSalary = monthly.reduce((s, m) => s + m.totalNetSalary, 0)
  const totalPFEmployer = monthly.reduce((s, m) => s + m.totalPFEmployer, 0)
  const totalDeductions = monthly.reduce((s, m) => s + m.totalDeductions, 0)
  return { year, totalGrossSalary, totalNetSalary, totalPFEmployer, totalDeductions, monthlyBreakdown: monthly }
}

const buildDepartments = (txns: EmployeeSalaryTransaction[], year: number, month?: number): DepartmentExpense[] => {
  const filtered = txns.filter(t => t.salaryYear === year && (month ? t.salaryMonth === month : true))
  const map = new Map<string, { gross: number; net: number; pfEmployer: number; deductions: number; employees: Set<string> }>()

  filtered.forEach(t => {
    const dept = t.employee?.department || 'Unassigned'
    const entry = map.get(dept) || { gross: 0, net: 0, pfEmployer: 0, deductions: 0, employees: new Set<string>() }
    entry.gross += clampNumber(t.grossSalary)
    entry.net += clampNumber(t.netSalary)
    entry.pfEmployer += clampNumber(t.pfEmployer)
    entry.deductions += clampNumber(t.pfEmployee) + clampNumber(t.pt) + clampNumber(t.incomeTax) + clampNumber(t.otherDeductions)
    if (t.employeeId) entry.employees.add(t.employeeId)
    map.set(dept, entry)
  })

  const result: DepartmentExpense[] = []
  map.forEach((entry, dept) => {
    const empCount = entry.employees.size || 1
    result.push({
      department: dept,
      employeeCount: entry.employees.size,
      totalGrossSalary: entry.gross,
      totalNetSalary: entry.net,
      totalPFEmployer: entry.pfEmployer,
      totalDeductions: entry.deductions,
      averageSalary: entry.net / empCount,
    })
  })

  return result.sort((a, b) => b.totalGrossSalary - a.totalGrossSalary)
}

export const useMonthlyExpenses = (year: number, transactions?: EmployeeSalaryTransaction[]) => {
  const txns = transactions || []
  const data = useMemo(() => (txns ? buildMonthly(txns, year) : []), [txns, year])
  return { data, isLoading: false }
}

export const useYearlyExpenseSummary = (year: number, transactions?: EmployeeSalaryTransaction[]) => {
  const txns = transactions || []
  const monthly = useMemo(() => (txns ? buildMonthly(txns, year) : []), [txns, year])
  const data = useMemo(() => buildYearly(monthly, year), [monthly, year])
  return { data, isLoading: false }
}

export const useDepartmentExpenses = (year: number, month?: number, transactions?: EmployeeSalaryTransaction[]) => {
  const txns = transactions || []
  const data = useMemo(() => (txns ? buildDepartments(txns, year, month) : []), [txns, year, month])
  return { data, isLoading: false }
}

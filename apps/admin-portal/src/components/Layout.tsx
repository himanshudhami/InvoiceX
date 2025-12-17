import { FC, ReactNode } from 'react'
import { ThemeSwitcher } from './ThemeSwitcher'
import { NavGroup, SingleNavItem } from './ui/NavGroup'
import { useSidebarState } from '@/hooks/useSidebarState'
import {
  LayoutDashboard,
  FileText,
  FileEdit,
  Users,
  Package,
  CreditCard,
  UserCircle,
  Wallet,
  Laptop,
  Building2,
  Landmark,
  Receipt,
  CircleDollarSign,
  BarChart3,
  FileSpreadsheet,
  Settings,
  CalendarDays,
  ClipboardCheck,
  CalendarClock,
  Calendar,
  Shield,
  UserCog,
} from 'lucide-react'

interface Props {
  children: ReactNode
}

const Layout: FC<Props> = ({ children }) => {
  const { expandedGroups, toggleGroup } = useSidebarState()

  const navigationGroups = [
    {
      type: 'single' as const,
      name: 'Dashboard',
      href: '/dashboard',
      icon: LayoutDashboard,
    },
    {
      type: 'group' as const,
      name: 'Sales & Billing',
      icon: FileText,
      items: [
        { name: 'Invoices', href: '/invoices', icon: FileText },
        { name: 'Quotes', href: '/quotes', icon: FileEdit },
        { name: 'Customers', href: '/customers', icon: Users },
        { name: 'Products', href: '/products', icon: Package },
        { name: 'Invoice Payments', href: '/payments', icon: CreditCard },
      ],
    },
    {
      type: 'group' as const,
      name: 'People & Payroll',
      icon: UserCircle,
      items: [
        { name: 'Employees', href: '/employees', icon: UserCircle },
        { name: 'Payroll', href: '/payroll', icon: Wallet },
        { name: 'Salary Structures', href: '/payroll/salary-structures', icon: FileSpreadsheet },
        { name: 'Tax Declarations', href: '/payroll/tax-declarations', icon: Receipt },
        { name: 'Contractors', href: '/payroll/contractors', icon: Users },
      ],
    },
    {
      type: 'group' as const,
      name: 'Leave Management',
      icon: CalendarDays,
      items: [
        { name: 'Leave Types', href: '/leave/types', icon: CalendarClock },
        { name: 'Leave Balances', href: '/leave/balances', icon: BarChart3 },
        { name: 'Applications', href: '/leave/applications', icon: ClipboardCheck },
        { name: 'Holidays', href: '/leave/holidays', icon: Calendar },
      ],
    },
    {
      type: 'group' as const,
      name: 'Assets & Subscriptions',
      icon: Laptop,
      items: [
        { name: 'Assets', href: '/assets', icon: Laptop },
        { name: 'Subscriptions', href: '/subscriptions', icon: CircleDollarSign },
      ],
    },
    {
      type: 'group' as const,
      name: 'Finance',
      icon: Landmark,
      items: [
        { name: 'Bank Accounts', href: '/bank/accounts', icon: Building2 },
        { name: 'Loans', href: '/loans', icon: Landmark },
        { name: 'EMI Payments', href: '/emi-payments', icon: CreditCard },
        { name: 'TDS Receivables', href: '/tds-receivables', icon: Receipt },
        { name: 'Expense Reports', href: '/expense-dashboard', icon: BarChart3 },
        { name: 'Financial Report', href: '/financial-report', icon: FileSpreadsheet },
      ],
    },
    {
      type: 'group' as const,
      name: 'Administration',
      icon: Shield,
      items: [
        { name: 'Users', href: '/users', icon: UserCog },
        { name: 'Settings', href: '/settings', icon: Settings },
      ],
    },
  ]

  return (
    <div className="flex min-h-screen bg-gray-50">
      <nav className="fixed w-64 h-full bg-white border-r border-gray-200 shadow-sm overflow-y-auto">
        <div className="p-6 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">Invoice System</h2>
            <ThemeSwitcher />
          </div>
        </div>
        <div className="py-2">
          {navigationGroups.map((item) =>
            item.type === 'single' ? (
              <SingleNavItem
                key={item.name}
                name={item.name}
                href={item.href}
                icon={item.icon}
              />
            ) : (
              <NavGroup
                key={item.name}
                name={item.name}
                icon={item.icon}
                items={item.items}
                isExpanded={expandedGroups.has(item.name)}
                onToggle={() => toggleGroup(item.name)}
              />
            )
          )}
        </div>
      </nav>

      <main className="flex-1 ml-64">
        <div className="w-full px-4 py-6 md:px-6 lg:px-8">
          {children}
        </div>
      </main>
    </div>
  )
}

export default Layout

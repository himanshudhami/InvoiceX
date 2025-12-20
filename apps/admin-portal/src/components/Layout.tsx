import { FC, ReactNode } from 'react'
import { ThemeSwitcher } from './ThemeSwitcher'
import { HeaderCompanySelector } from './HeaderCompanySelector'
import { NavGroup, SingleNavItem } from './ui/NavGroup'
import { useSidebarState } from '@/hooks/useSidebarState'
import { useSidebarCollapse } from '@/hooks/useSidebarCollapse'
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
  Megaphone,
  HelpCircle,
  FolderOpen,
  GitBranch,
  Network,
  Calculator,
  PackageSearch,
  PanelLeftClose,
  PanelLeftOpen,
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface Props {
  children: ReactNode
}

const Layout: FC<Props> = ({ children }) => {
  const { expandedGroups, toggleGroup } = useSidebarState()
  const { isCollapsed, toggle: toggleSidebar } = useSidebarCollapse()

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
        { name: 'Calculation Rules', href: '/payroll/calculation-rules', icon: Calculator },
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
        { name: 'Asset Requests', href: '/asset-requests', icon: PackageSearch },
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
      name: 'Employee Portal',
      icon: UserCircle,
      items: [
        { name: 'Announcements', href: '/announcements', icon: Megaphone },
        { name: 'Support Tickets', href: '/support-tickets', icon: HelpCircle },
        { name: 'Documents', href: '/employee-documents', icon: FolderOpen },
      ],
    },
    {
      type: 'group' as const,
      name: 'Administration',
      icon: Shield,
      items: [
        { name: 'Users', href: '/users', icon: UserCog },
        { name: 'Organization Chart', href: '/org-chart', icon: Network },
        { name: 'Approval Workflows', href: '/workflows', icon: GitBranch },
        { name: 'Settings', href: '/settings', icon: Settings },
      ],
    },
  ]

  return (
    <div className="flex min-h-screen bg-gray-50">
      <nav
        className={cn(
          'fixed h-full bg-white border-r border-gray-200 shadow-sm overflow-y-auto transition-all duration-300 ease-in-out',
          isCollapsed ? 'w-16' : 'w-64'
        )}
      >
        <div className={cn('border-b border-gray-200 dark:border-gray-700', isCollapsed ? 'p-4' : 'p-6')}>
          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between">
              {!isCollapsed && (
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Invoice System</h2>
              )}
              <div className="flex items-center gap-2">
                {isCollapsed && (
                  <button
                    onClick={toggleSidebar}
                    className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                    aria-label="Expand sidebar"
                  >
                    <PanelLeftOpen className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                  </button>
                )}
                <ThemeSwitcher />
                {!isCollapsed && (
                  <button
                    onClick={toggleSidebar}
                    className="p-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                    aria-label="Collapse sidebar"
                  >
                    <PanelLeftClose className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                  </button>
                )}
              </div>
            </div>
            {!isCollapsed && <HeaderCompanySelector />}
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
                isCollapsed={isCollapsed}
              />
            ) : (
              <NavGroup
                key={item.name}
                name={item.name}
                icon={item.icon}
                items={item.items}
                isExpanded={expandedGroups.has(item.name)}
                onToggle={() => {
                  if (isCollapsed) {
                    // When collapsed, just expand the sidebar
                    toggleSidebar()
                  } else {
                    toggleGroup(item.name)
                  }
                }}
                isCollapsed={isCollapsed}
              />
            )
          )}
        </div>
      </nav>

      <main
        className={cn(
          'flex-1 transition-all duration-300 ease-in-out',
          isCollapsed ? 'ml-16' : 'ml-64'
        )}
      >
        <div className="w-full px-4 py-6 md:px-6 lg:px-8">
          {children}
        </div>
      </main>
    </div>
  )
}

export default Layout

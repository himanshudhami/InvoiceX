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
  BookOpen,
  BookText,
  Scale,
  TrendingUp,
  PieChart,
  ScrollText,
  FileCheck,
  BookMarked,
  Globe,
  DollarSign,
  FileSearch,
  ShieldCheck,
  Clock,
  Tags,
  RotateCcw,
  Award,
  Coins,
  FileX,
  ArrowDownUp,
  ClipboardList,
  FileBarChart,
  IndianRupee,
  BadgeIndianRupee,
  Truck,
  Banknote,
  Warehouse,
  FolderTree,
  Ruler,
  Boxes,
  ArrowRightLeft,
  Factory,
  FileText as FileTextIcon,
  ClipboardList as ClipboardListIcon,
  Hash,
  DatabaseZap,
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
      name: 'Inventory',
      icon: Boxes,
      items: [
        { name: 'Warehouses', href: '/inventory/warehouses', icon: Warehouse },
        { name: 'Stock Groups', href: '/inventory/stock-groups', icon: FolderTree },
        { name: 'Units of Measure', href: '/inventory/units', icon: Ruler },
        { name: 'Stock Items', href: '/inventory/items', icon: Package },
        { name: 'Stock Movements', href: '/inventory/movements', icon: ArrowRightLeft },
        { name: 'Stock Transfers', href: '/inventory/transfers', icon: Truck },
      ],
    },
    {
      type: 'group' as const,
      name: 'Manufacturing',
      icon: Factory,
      items: [
        { name: 'Bill of Materials', href: '/manufacturing/bom', icon: FileText },
        { name: 'Production Orders', href: '/manufacturing/production', icon: ClipboardCheck },
        { name: 'Serial Numbers', href: '/manufacturing/serial-numbers', icon: Hash },
      ],
    },
    {
      type: 'group' as const,
      name: 'Accounts Payable',
      icon: Truck,
      items: [
        { name: 'Vendors', href: '/finance/ap/vendors', icon: Users },
        { name: 'Contractors', href: '/finance/ap/contractors', icon: UserCog },
        { name: 'Contractor Payments', href: '/finance/ap/contractor-payments', icon: CreditCard },
        { name: 'Bills & Invoices', href: '/finance/ap/vendor-invoices', icon: FileText },
        { name: 'Vendor Payments', href: '/finance/ap/vendor-payments', icon: Banknote },
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
      name: 'Expenses',
      icon: Receipt,
      items: [
        { name: 'Expense Claims', href: '/expense-claims', icon: FileText },
        { name: 'Categories', href: '/expense-categories', icon: Tags },
        { name: 'Reports', href: '/expense-dashboard', icon: BarChart3 },
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
        { name: 'Financial Report', href: '/financial-report', icon: FileSpreadsheet },
      ],
    },
    {
      type: 'group' as const,
      name: 'GST & TDS Compliance',
      icon: ShieldCheck,
      items: [
        { name: 'Compliance Dashboard', href: '/gst', icon: BarChart3 },
        { name: 'RCM Management', href: '/gst/rcm', icon: RotateCcw },
        { name: 'TDS Returns (26Q/24Q)', href: '/gst/tds-returns', icon: FileText },
        { name: 'LDC Certificates', href: '/gst/ldc', icon: Award },
        { name: 'TCS Management', href: '/gst/tcs', icon: Coins },
        { name: 'ITC Blocked', href: '/gst/itc-blocked', icon: FileX },
        { name: 'ITC Reversal', href: '/gst/itc-reversal', icon: ArrowDownUp },
      ],
    },
    {
      type: 'group' as const,
      name: 'Statutory (PF/ESI/TDS)',
      icon: IndianRupee,
      items: [
        { name: 'Statutory Dashboard', href: '/statutory', icon: BarChart3 },
        { name: 'Form 16 (TDS Cert)', href: '/statutory/form16', icon: FileBarChart },
        { name: 'TDS Challans', href: '/statutory/tds-challan', icon: ClipboardList },
        { name: 'PF ECR', href: '/statutory/pf-ecr', icon: BadgeIndianRupee },
        { name: 'ESI Returns', href: '/statutory/esi-return', icon: Receipt },
      ],
    },
    {
      type: 'group' as const,
      name: 'Exports & Forex',
      icon: Globe,
      items: [
        { name: 'Export Dashboard', href: '/exports', icon: BarChart3 },
        { name: 'FIRC Management', href: '/exports/firc', icon: DollarSign },
        { name: 'LUT Register', href: '/exports/lut', icon: FileSearch },
        { name: 'FEMA Compliance', href: '/exports/fema', icon: ShieldCheck },
        { name: 'Receivables Ageing', href: '/exports/ageing', icon: Clock },
      ],
    },
    {
      type: 'group' as const,
      name: 'Accounting',
      icon: BookOpen,
      items: [
        { name: 'Chart of Accounts', href: '/ledger/accounts', icon: BookText },
        { name: 'Journal Entries', href: '/ledger/journals', icon: FileText },
        { name: 'Trial Balance', href: '/ledger/trial-balance', icon: Scale },
        { name: 'Income Statement', href: '/ledger/income-statement', icon: TrendingUp },
        { name: 'Balance Sheet', href: '/ledger/balance-sheet', icon: PieChart },
        { name: 'Account Ledger', href: '/ledger/account-ledger', icon: ScrollText },
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
        { name: 'E-Invoice Settings', href: '/einvoice/settings', icon: FileCheck },
        { name: 'Tax Rule Packs', href: '/tax-rule-packs', icon: BookMarked },
        { name: 'Tally Migration', href: '/settings/migration/tally', icon: DatabaseZap },
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

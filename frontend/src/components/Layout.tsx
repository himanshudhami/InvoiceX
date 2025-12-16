import { FC, ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { ThemeSwitcher } from './ThemeSwitcher'

interface Props {
  children: ReactNode
}

const Layout: FC<Props> = ({ children }) => {
  const location = useLocation()

  const navigation = [
    { name: 'Dashboard', href: '/dashboard', icon: '📊' },
    { name: 'Invoices', href: '/invoices', icon: '🧾' },
    { name: 'Quotes', href: '/quotes', icon: '📝' },
    { name: 'Customers', href: '/customers', icon: '👥' },
    { name: 'Products', href: '/products', icon: '📦' },
    { name: 'Employees', href: '/employees', icon: '👤' },
    { name: 'Payroll', href: '/payroll', icon: '💰' },
    { name: 'Assets', href: '/assets', icon: '💻' },
    { name: 'Bank Accounts', href: '/bank/accounts', icon: '🏛️' },
    { name: 'Loans', href: '/loans', icon: '🏦' },
    { name: 'EMI Payments', href: '/emi-payments', icon: '💳' },
    { name: 'Invoice Payments', href: '/payments', icon: '💰' },
    { name: 'Subscriptions', href: '/subscriptions', icon: '🪙' },
    { name: 'Expense Reports', href: '/expense-dashboard', icon: '📈' },
    { name: 'Financial Report', href: '/financial-report', icon: '📊' },
    { name: 'Settings', href: '/settings', icon: '⚙️' },
  ]

  const isActive = (href: string) => {
    if (href === '/dashboard') {
      return location.pathname === href
    }
    return location.pathname.startsWith(href)
  }

  return (
    <div className="flex min-h-screen bg-gray-50">
      <nav className="fixed w-64 h-full bg-white border-r border-gray-200 shadow-sm">
        <div className="p-6 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">Invoice System</h2>
            <ThemeSwitcher />
          </div>
        </div>
        <ul className="py-2">
          {navigation.map((item) => (
            <li key={item.name}>
              <Link
                to={item.href}
                className={cn(
                  'flex items-center px-6 py-3 text-sm font-medium transition-colors',
                  isActive(item.href)
                    ? 'bg-primary/10 text-primary border-r-2 border-primary'
                    : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                )}
              >
                <span className="mr-3 text-lg">{item.icon}</span>
                {item.name}
              </Link>
            </li>
          ))}
        </ul>
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
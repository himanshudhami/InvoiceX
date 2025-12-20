import { NavLink, useNavigate } from 'react-router-dom'
import {
  Home,
  Calendar,
  FileText,
  Laptop,
  Receipt,
  FolderOpen,
  Megaphone,
  HelpCircle,
  LogOut,
  ChevronRight,
  Users,
  ClipboardCheck,
  Package,
  Wallet,
} from 'lucide-react'
import { useAuth } from '@/context/AuthContext'
import { cn, getInitials } from '@repo/ui'

interface NavItem {
  name: string
  href: string
  icon: React.ElementType
  badge?: number
}

const navItems: NavItem[] = [
  { name: 'Dashboard', href: '/', icon: Home },
  { name: 'Leave', href: '/leave', icon: Calendar },
  { name: 'Payslips', href: '/payslips', icon: FileText },
  { name: 'Assets', href: '/assets', icon: Laptop },
  { name: 'Expenses', href: '/expenses', icon: Wallet },
  { name: 'Tax Declarations', href: '/tax-declarations', icon: Receipt },
  { name: 'Documents', href: '/documents', icon: FolderOpen },
  { name: 'Announcements', href: '/announcements', icon: Megaphone },
  { name: 'Help Desk', href: '/help', icon: HelpCircle },
]

const managerNavItems: NavItem[] = [
  { name: 'My Team', href: '/manager/team', icon: Users },
  { name: 'Leave Approvals', href: '/manager/approvals', icon: ClipboardCheck },
  { name: 'Expense Approvals', href: '/manager/expense-approvals', icon: Wallet },
  { name: 'Asset Approvals', href: '/manager/asset-approvals', icon: Package },
]

export function Sidebar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <nav className="fixed left-0 top-0 w-64 h-full bg-white/80 backdrop-blur-xl border-r border-gray-200/50 shadow-sm flex flex-col z-40">
      {/* Header */}
      <div className="p-6 border-b border-gray-200/50">
        <h1 className="text-xl font-bold bg-gradient-to-r from-primary-600 to-primary-500 bg-clip-text text-transparent">
          Employee Portal
        </h1>
        <p className="text-xs text-gray-500 mt-1">Self-service hub</p>
      </div>

      {/* Navigation */}
      <div className="flex-1 py-4 overflow-y-auto">
        <div className="px-3 space-y-1">
          {navItems.map((item) => (
            <NavLink
              key={item.href}
              to={item.href}
              end={item.href === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200',
                  isActive
                    ? 'bg-primary-50 text-primary-700 shadow-sm'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                )
              }
            >
              {({ isActive }) => (
                <>
                  <item.icon
                    size={18}
                    className={cn(
                      'transition-colors',
                      isActive ? 'text-primary-600' : 'text-gray-400'
                    )}
                  />
                  <span className="flex-1">{item.name}</span>
                  {item.badge && item.badge > 0 && (
                    <span className="flex items-center justify-center min-w-5 h-5 px-1.5 text-xs font-semibold bg-red-500 text-white rounded-full">
                      {item.badge}
                    </span>
                  )}
                  {isActive && (
                    <ChevronRight size={14} className="text-primary-400" />
                  )}
                </>
              )}
            </NavLink>
          ))}
        </div>

        {/* Manager Section */}
        {user?.isManager && (
          <div className="mt-6 px-3">
            <p className="px-3 mb-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
              Manager
            </p>
            <div className="space-y-1">
              {managerNavItems.map((item) => (
                <NavLink
                  key={item.href}
                  to={item.href}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200',
                      isActive
                        ? 'bg-amber-50 text-amber-700 shadow-sm'
                        : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                    )
                  }
                >
                  {({ isActive }) => (
                    <>
                      <item.icon
                        size={18}
                        className={cn(
                          'transition-colors',
                          isActive ? 'text-amber-600' : 'text-gray-400'
                        )}
                      />
                      <span className="flex-1">{item.name}</span>
                      {isActive && (
                        <ChevronRight size={14} className="text-amber-400" />
                      )}
                    </>
                  )}
                </NavLink>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* User Profile Section */}
      <div className="border-t border-gray-200/50 p-4">
        <NavLink
          to="/profile"
          className={({ isActive }) =>
            cn(
              'flex items-center gap-3 p-3 rounded-xl transition-all duration-200',
              isActive
                ? 'bg-primary-50'
                : 'hover:bg-gray-50'
            )
          }
        >
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gradient-to-br from-primary-500 to-primary-600 text-white font-semibold text-sm">
            {user?.employeeName ? getInitials(user.employeeName) : 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900 truncate">
              {user?.employeeName || 'User'}
            </p>
            <p className="text-xs text-gray-500 truncate">
              {user?.email || 'View profile'}
            </p>
          </div>
        </NavLink>

        <button
          onClick={handleLogout}
          className="flex items-center gap-3 w-full mt-2 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-600 hover:bg-red-50 hover:text-red-600 transition-all duration-200"
        >
          <LogOut size={18} className="text-gray-400" />
          <span>Sign Out</span>
        </button>
      </div>
    </nav>
  )
}

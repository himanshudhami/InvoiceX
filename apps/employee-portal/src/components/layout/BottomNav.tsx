import React, { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import {
  Home,
  Calendar,
  FileText,
  Laptop,
  MoreHorizontal,
  Receipt,
  FolderOpen,
  Megaphone,
  HelpCircle,
  User,
  X,
  ChevronRight,
  Users,
  ClipboardCheck,
} from 'lucide-react'
import { useAuth } from '@/context/AuthContext'
import { cn } from '@/utils/cn'

interface NavItemProps {
  to: string
  icon: React.ReactNode
  label: string
  end?: boolean
}

function NavItem({ to, icon, label, end }: NavItemProps) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        cn(
          'flex flex-col items-center justify-center gap-0.5 py-2 px-3 transition-colors touch-feedback min-w-[64px]',
          isActive
            ? 'text-primary-600'
            : 'text-gray-500 hover:text-gray-700'
        )
      }
    >
      {({ isActive }) => (
        <>
          <div
            className={cn(
              'flex items-center justify-center w-6 h-6',
              isActive && 'scale-110'
            )}
          >
            {icon}
          </div>
          <span className={cn('text-[10px] font-medium', isActive && 'font-semibold')}>
            {label}
          </span>
        </>
      )}
    </NavLink>
  )
}

interface MoreMenuItem {
  to: string
  icon: React.ElementType
  label: string
}

const moreMenuItems: MoreMenuItem[] = [
  { to: '/tax-declarations', icon: Receipt, label: 'Tax Declarations' },
  { to: '/documents', icon: FolderOpen, label: 'Documents' },
  { to: '/announcements', icon: Megaphone, label: 'Announcements' },
  { to: '/help', icon: HelpCircle, label: 'Help Desk' },
  { to: '/profile', icon: User, label: 'Profile' },
]

const managerMenuItems: MoreMenuItem[] = [
  { to: '/manager/team', icon: Users, label: 'My Team' },
  { to: '/manager/approvals', icon: ClipboardCheck, label: 'Pending Approvals' },
]

export function BottomNav() {
  const [isMoreOpen, setIsMoreOpen] = useState(false)
  const navigate = useNavigate()
  const { user } = useAuth()

  const handleMoreItemClick = (to: string) => {
    setIsMoreOpen(false)
    navigate(to)
  }

  return (
    <>
      {/* More Menu Overlay */}
      {isMoreOpen && (
        <div
          className="fixed inset-0 bg-black/30 backdrop-blur-sm z-40 animate-fade-in"
          onClick={() => setIsMoreOpen(false)}
        />
      )}

      {/* More Menu Sheet */}
      <div
        className={cn(
          'fixed bottom-0 left-0 right-0 bg-white rounded-t-3xl shadow-2xl z-50 transition-transform duration-300 ease-out',
          isMoreOpen ? 'translate-y-0' : 'translate-y-full'
        )}
      >
        <div className="p-4 safe-bottom">
          {/* Handle bar */}
          <div className="flex justify-center mb-3">
            <div className="w-10 h-1 bg-gray-300 rounded-full" />
          </div>

          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900">More</h3>
            <button
              onClick={() => setIsMoreOpen(false)}
              className="p-2 rounded-full hover:bg-gray-100 transition-colors"
            >
              <X size={20} className="text-gray-500" />
            </button>
          </div>

          {/* Manager Items */}
          {user?.isManager && (
            <div className="space-y-1 mb-4">
              <p className="px-3 mb-2 text-xs font-semibold text-gray-400 uppercase tracking-wider">
                Manager
              </p>
              {managerMenuItems.map((item) => (
                <button
                  key={item.to}
                  onClick={() => handleMoreItemClick(item.to)}
                  className="flex items-center gap-4 w-full p-3 rounded-xl hover:bg-amber-50 transition-colors touch-feedback"
                >
                  <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-amber-50">
                    <item.icon size={20} className="text-amber-600" />
                  </div>
                  <span className="flex-1 text-left text-sm font-medium text-gray-900">
                    {item.label}
                  </span>
                  <ChevronRight size={18} className="text-gray-400" />
                </button>
              ))}
            </div>
          )}

          {/* Menu Items */}
          <div className="space-y-1">
            {moreMenuItems.map((item) => (
              <button
                key={item.to}
                onClick={() => handleMoreItemClick(item.to)}
                className="flex items-center gap-4 w-full p-3 rounded-xl hover:bg-gray-50 transition-colors touch-feedback"
              >
                <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-50">
                  <item.icon size={20} className="text-primary-600" />
                </div>
                <span className="flex-1 text-left text-sm font-medium text-gray-900">
                  {item.label}
                </span>
                <ChevronRight size={18} className="text-gray-400" />
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Bottom Navigation Bar */}
      <nav className="bottom-nav safe-bottom bg-white/80 backdrop-blur-xl border-t border-gray-200/50">
        <div className="flex items-center justify-around h-16 max-w-lg mx-auto">
          <NavItem to="/" icon={<Home size={22} />} label="Home" end />
          <NavItem to="/leave" icon={<Calendar size={22} />} label="Leave" />
          <NavItem to="/payslips" icon={<FileText size={22} />} label="Payslips" />
          <NavItem to="/assets" icon={<Laptop size={22} />} label="Assets" />
          <button
            onClick={() => setIsMoreOpen(true)}
            className={cn(
              'flex flex-col items-center justify-center gap-0.5 py-2 px-3 transition-colors touch-feedback min-w-[64px]',
              isMoreOpen ? 'text-primary-600' : 'text-gray-500 hover:text-gray-700'
            )}
          >
            <div className="flex items-center justify-center w-6 h-6">
              <MoreHorizontal size={22} />
            </div>
            <span className={cn('text-[10px] font-medium', isMoreOpen && 'font-semibold')}>
              More
            </span>
          </button>
        </div>
      </nav>
    </>
  )
}

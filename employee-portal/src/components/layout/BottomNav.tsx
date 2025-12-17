import React from 'react'
import { NavLink } from 'react-router-dom'
import { Home, Calendar, FileText, Laptop, User } from 'lucide-react'
import { cn } from '@/utils/cn'

interface NavItemProps {
  to: string
  icon: React.ReactNode
  label: string
}

function NavItem({ to, icon, label }: NavItemProps) {
  return (
    <NavLink
      to={to}
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

export function BottomNav() {
  return (
    <nav className="bottom-nav safe-bottom">
      <div className="flex items-center justify-around h-16 max-w-lg mx-auto">
        <NavItem to="/" icon={<Home size={22} />} label="Home" />
        <NavItem to="/leave" icon={<Calendar size={22} />} label="Leave" />
        <NavItem to="/payslips" icon={<FileText size={22} />} label="Payslips" />
        <NavItem to="/assets" icon={<Laptop size={22} />} label="Assets" />
        <NavItem to="/profile" icon={<User size={22} />} label="Profile" />
      </div>
    </nav>
  )
}

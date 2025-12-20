import { FC } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { ChevronDown, LucideIcon } from 'lucide-react'

interface NavItem {
  name: string
  href: string
  icon?: LucideIcon
}

interface NavGroupProps {
  name: string
  icon: LucideIcon
  items: NavItem[]
  isExpanded: boolean
  onToggle: () => void
  isCollapsed?: boolean
}

export const NavGroup: FC<NavGroupProps> = ({
  name,
  icon: Icon,
  items,
  isExpanded,
  onToggle,
  isCollapsed = false,
}) => {
  const location = useLocation()

  const isChildActive = items.some((item) => {
    if (item.href === '/dashboard') {
      return location.pathname === item.href
    }
    return location.pathname.startsWith(item.href)
  })

  if (isCollapsed) {
    return (
      <div className="py-1">
        <button
          onClick={onToggle}
          className={cn(
            'flex w-full items-center justify-center py-2.5 text-sm font-medium transition-colors',
            isChildActive
              ? 'text-primary bg-primary/5'
              : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
          )}
          title={name}
        >
          <Icon className="h-5 w-5" />
        </button>
      </div>
    )
  }

  return (
    <div className="py-1">
      <button
        onClick={onToggle}
        className={cn(
          'flex w-full items-center justify-between px-6 py-2.5 text-sm font-medium transition-colors',
          isChildActive
            ? 'text-primary bg-primary/5'
            : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
        )}
      >
        <div className="flex items-center">
          <Icon className="mr-3 h-5 w-5" />
          {name}
        </div>
        <ChevronDown
          className={cn(
            'h-4 w-4 transition-transform duration-200',
            isExpanded ? 'rotate-180' : ''
          )}
        />
      </button>

      <div
        className={cn(
          'overflow-hidden transition-all duration-200 ease-in-out',
          isExpanded ? 'max-h-96 opacity-100' : 'max-h-0 opacity-0'
        )}
      >
        <ul className="py-1 pl-4">
          {items.map((item) => {
            const isActive =
              item.href === '/dashboard'
                ? location.pathname === item.href
                : location.pathname.startsWith(item.href)

            return (
              <li key={item.href}>
                <Link
                  to={item.href}
                  className={cn(
                    'flex items-center px-6 py-2 text-sm transition-colors',
                    isActive
                      ? 'text-primary font-medium border-r-2 border-primary bg-primary/10'
                      : 'text-gray-500 hover:text-gray-900 hover:bg-gray-50'
                  )}
                >
                  {item.icon && <item.icon className="mr-3 h-4 w-4" />}
                  {item.name}
                </Link>
              </li>
            )
          })}
        </ul>
      </div>
    </div>
  )
}

interface SingleNavItemProps {
  name: string
  href: string
  icon: LucideIcon
  isCollapsed?: boolean
}

export const SingleNavItem: FC<SingleNavItemProps> = ({ name, href, icon: Icon, isCollapsed = false }) => {
  const location = useLocation()
  const isActive =
    href === '/dashboard'
      ? location.pathname === href
      : location.pathname.startsWith(href)

  if (isCollapsed) {
    return (
      <Link
        to={href}
        className={cn(
          'flex items-center justify-center py-3 text-sm font-medium transition-colors',
          isActive
            ? 'bg-primary/10 text-primary border-r-2 border-primary'
            : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
        )}
        title={name}
      >
        <Icon className="h-5 w-5" />
      </Link>
    )
  }

  return (
    <Link
      to={href}
      className={cn(
        'flex items-center px-6 py-3 text-sm font-medium transition-colors',
        isActive
          ? 'bg-primary/10 text-primary border-r-2 border-primary'
          : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
      )}
    >
      <Icon className="mr-3 h-5 w-5" />
      {name}
    </Link>
  )
}

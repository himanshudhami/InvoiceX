import { FC, ReactNode } from 'react'
import { ThemeSwitcher } from './ThemeSwitcher'
import { HeaderCompanySelector } from './HeaderCompanySelector'
import { NavGroup, SingleNavItem } from './ui/NavGroup'
import { CommandPalette } from './CommandPalette'
import { useSidebarState } from '@/hooks/useSidebarState'
import { useSidebarCollapse } from '@/hooks/useSidebarCollapse'
import { useResizable } from '@/hooks/useResizable'
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { navigationGroups } from '@/data/navigation'
import { cn } from '@/lib/utils'

interface Props {
  children: ReactNode
}

const Layout: FC<Props> = ({ children }) => {
  const { expandedGroups, toggleGroup } = useSidebarState()
  const { isCollapsed, toggle: toggleSidebar } = useSidebarCollapse()
  const collapsedWidth = 64
  const {
    width: expandedWidth,
    isDragging,
    handleProps,
  } = useResizable({
    initialWidth: 300,
    minWidth: 260,
    maxWidth: typeof window !== 'undefined'
      ? Math.max(320, Math.min(560, window.innerWidth - 160))
      : 560,
    storageKey: 'sidebar-width',
    direction: 'right',
  })
  const sidebarWidth = isCollapsed ? collapsedWidth : expandedWidth

  return (
    <div className="flex min-h-screen bg-gray-50">
      <CommandPalette />
      <nav
        className={cn(
          'sticky top-0 h-screen self-start bg-white border-r border-gray-200 shadow-sm overflow-hidden relative shrink-0',
          isDragging ? 'transition-none' : 'transition-all duration-300 ease-in-out'
        )}
        style={{ width: `${sidebarWidth}px` }}
      >
        {!isCollapsed && (
          <div
            {...handleProps}
            className={cn(
              'group absolute right-0 top-0 h-full w-1.5 cursor-ew-resize',
              'bg-transparent hover:bg-blue-200/60',
              isDragging && 'bg-blue-300/70'
            )}
            title="Drag to resize"
          >
            <div
              className={cn(
                'absolute right-0 top-1/2 -translate-y-1/2',
                'h-12 w-0.5 bg-gray-300 opacity-0 transition-opacity',
                'group-hover:opacity-100',
                isDragging && 'opacity-100 bg-blue-500'
              )}
            />
          </div>
        )}
        <div className="h-full overflow-y-auto">
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
        </div>
      </nav>

      <main
        className={cn(
          'flex-1',
          isDragging ? 'transition-none' : 'transition-all duration-300 ease-in-out'
        )}
      >
        <div className="w-full py-6 pr-4 md:pr-6 lg:pr-8">
          {children}
        </div>
      </main>
    </div>
  )
}

export default Layout

import { Outlet } from 'react-router-dom'
import { BottomNav } from './BottomNav'

export function MainLayout() {
  return (
    <div className="min-h-screen bg-gray-50 pb-20">
      <main className="max-w-lg mx-auto">
        <Outlet />
      </main>
      <BottomNav />
    </div>
  )
}

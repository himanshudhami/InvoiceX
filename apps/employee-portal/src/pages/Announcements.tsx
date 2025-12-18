import { useQuery } from '@tanstack/react-query'
import { Megaphone, Pin, Calendar, ChevronRight } from 'lucide-react'
import { portalApi } from '@/api'
import { EmptyState } from '@/components/layout'
import { Badge, GlassCard, PageLoader } from '@/components/ui'
import { formatDate } from '@/utils/format'
import type { AnnouncementSummary } from '@/types'

const getCategoryBadge = (category: string) => {
  switch (category) {
    case 'holiday':
      return { variant: 'glass-warning' as const, label: 'Holiday' }
    case 'hr':
      return { variant: 'glass-primary' as const, label: 'HR' }
    case 'policy':
      return { variant: 'glass-error' as const, label: 'Policy' }
    case 'celebration':
      return { variant: 'glass-success' as const, label: 'Celebration' }
    default:
      return { variant: 'glass' as const, label: 'General' }
  }
}

const getPriorityColor = (priority: string) => {
  switch (priority) {
    case 'urgent':
      return 'from-red-100 to-red-50'
    case 'high':
      return 'from-orange-100 to-orange-50'
    default:
      return 'from-primary-100 to-primary-50'
  }
}

export function AnnouncementsPage() {
  const { data: announcements, isLoading, error } = useQuery<AnnouncementSummary[]>({
    queryKey: ['announcements'],
    queryFn: () => portalApi.getAnnouncements(),
  })

  if (isLoading) {
    return <PageLoader />
  }

  if (error) {
    console.error('Failed to fetch announcements:', error)
  }

  const unreadCount = announcements?.filter(a => !a.isRead).length || 0

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-2">
          <h1 className="text-2xl font-bold text-gray-900">Announcements</h1>
          {unreadCount > 0 && (
            <Badge variant="gradient" glow="primary">
              {unreadCount} unread
            </Badge>
          )}
        </div>
        <p className="text-sm text-gray-500">
          Stay updated with company news and updates
        </p>
      </div>

      {/* Announcements List */}
      {!announcements || announcements.length === 0 ? (
        <EmptyState
          icon={<Megaphone className="text-gray-400" size={24} />}
          title="No announcements"
          description="Check back later for company updates"
        />
      ) : (
        <div className="space-y-3">
          {announcements.map((announcement) => (
            <AnnouncementCard key={announcement.id} announcement={announcement} />
          ))}
        </div>
      )}
    </div>
  )
}

function AnnouncementCard({ announcement }: { announcement: AnnouncementSummary }) {
  const categoryBadge = getCategoryBadge(announcement.category)

  return (
    <GlassCard
      className={`p-4 ${!announcement.isRead ? 'ring-2 ring-primary-200' : ''}`}
      hoverEffect
    >
      <div className="flex items-start gap-3">
        <div className={`flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br ${getPriorityColor(announcement.priority)}`}>
          {announcement.isPinned ? (
            <Pin size={18} className="text-orange-600" />
          ) : (
            <Megaphone size={18} className="text-primary-600" />
          )}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2 mb-1">
            <h3 className={`text-sm font-semibold text-gray-900 ${!announcement.isRead ? '' : ''}`}>
              {announcement.title}
            </h3>
            {!announcement.isRead && (
              <div className="w-2 h-2 rounded-full bg-primary-500 mt-1.5 flex-shrink-0" />
            )}
          </div>
          <p className="text-xs text-gray-600 line-clamp-2 mb-2">
            {announcement.content}
          </p>
          <div className="flex items-center gap-2">
            <Badge variant={categoryBadge.variant} size="sm">
              {categoryBadge.label}
            </Badge>
            {announcement.publishedAt && (
              <span className="text-[10px] text-gray-400 flex items-center gap-1">
                <Calendar size={10} />
                {formatDate(announcement.publishedAt, 'dd MMM yyyy')}
              </span>
            )}
          </div>
        </div>
        <ChevronRight size={16} className="text-gray-400 flex-shrink-0 mt-1" />
      </div>
    </GlassCard>
  )
}

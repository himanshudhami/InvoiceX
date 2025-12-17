export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}

export function getStatusBadgeVariant(
  status: string
): 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn' | 'default' {
  const statusLower = status.toLowerCase()
  if (statusLower === 'pending' || statusLower === 'draft' || statusLower === 'submitted') {
    return 'pending'
  }
  if (statusLower === 'approved' || statusLower === 'verified' || statusLower === 'active') {
    return 'approved'
  }
  if (statusLower === 'rejected') {
    return 'rejected'
  }
  if (statusLower === 'cancelled') {
    return 'cancelled'
  }
  if (statusLower === 'withdrawn') {
    return 'withdrawn'
  }
  return 'default'
}

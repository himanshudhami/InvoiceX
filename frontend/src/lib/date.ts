import { format } from 'date-fns';

/**
 * Format a date string for display in lists and tables
 */
export function formatDate(dateString: string | undefined): string {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString();
}

/**
 * Format a date string for display in forms and details
 */
export function formatDateDetailed(dateString: string | undefined): string {
  if (!dateString) return '';
  return format(new Date(dateString), 'MMM dd, yyyy');
}

/**
 * Format a date string for display with time
 */
export function formatDateTime(dateString: string | undefined): string {
  if (!dateString) return '';
  return format(new Date(dateString), 'MMM dd, yyyy hh:mm a');
}

/**
 * Format a date string for file names
 */
export function formatDateForFile(date: Date = new Date()): string {
  return format(date, 'yyyy-MM-dd');
}

/**
 * Calculate days between two dates
 */
export function daysBetween(startDate: string, endDate: string): number {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diffTime = Math.abs(end.getTime() - start.getTime());
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

/**
 * Check if a date is overdue
 */
export function isOverdue(dueDate: string): boolean {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const due = new Date(dueDate);
  due.setHours(0, 0, 0, 0);
  return due < today;
}

/**
 * Get relative time description
 */
export function getRelativeTime(dateString: string): string {
  const now = new Date();
  const date = new Date(dateString);
  const diffInMs = now.getTime() - date.getTime();
  const diffInDays = Math.floor(diffInMs / (1000 * 60 * 60 * 24));

  if (diffInDays === 0) return 'Today';
  if (diffInDays === 1) return 'Yesterday';
  if (diffInDays < 7) return `${diffInDays} days ago`;
  if (diffInDays < 30) return `${Math.floor(diffInDays / 7)} weeks ago`;
  if (diffInDays < 365) return `${Math.floor(diffInDays / 30)} months ago`;
  return `${Math.floor(diffInDays / 365)} years ago`;
}

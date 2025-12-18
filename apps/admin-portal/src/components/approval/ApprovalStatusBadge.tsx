import { ApprovalStatus, ApprovalStepStatus } from 'shared-types';
import { Badge } from '@/components/ui/badge';
import { Clock, CheckCircle, XCircle, Ban, Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ApprovalStatusBadgeProps {
  status: ApprovalStatus | ApprovalStepStatus;
  size?: 'sm' | 'md';
  showIcon?: boolean;
}

const statusConfig: Record<ApprovalStatus | ApprovalStepStatus, {
  label: string;
  variant: 'default' | 'secondary' | 'destructive' | 'outline';
  className: string;
  icon: React.ComponentType<{ className?: string }>;
}> = {
  pending: {
    label: 'Pending',
    variant: 'outline',
    className: 'border-yellow-500 text-yellow-600 bg-yellow-50',
    icon: Clock,
  },
  in_progress: {
    label: 'In Progress',
    variant: 'outline',
    className: 'border-blue-500 text-blue-600 bg-blue-50',
    icon: Loader2,
  },
  approved: {
    label: 'Approved',
    variant: 'outline',
    className: 'border-green-500 text-green-600 bg-green-50',
    icon: CheckCircle,
  },
  rejected: {
    label: 'Rejected',
    variant: 'destructive',
    className: 'border-red-500 text-red-600 bg-red-50',
    icon: XCircle,
  },
  cancelled: {
    label: 'Cancelled',
    variant: 'secondary',
    className: 'border-gray-400 text-gray-500 bg-gray-50',
    icon: Ban,
  },
  skipped: {
    label: 'Skipped',
    variant: 'secondary',
    className: 'border-gray-400 text-gray-500 bg-gray-50',
    icon: Ban,
  },
};

export function ApprovalStatusBadge({
  status,
  size = 'md',
  showIcon = true,
}: ApprovalStatusBadgeProps) {
  const config = statusConfig[status];
  const Icon = config.icon;

  return (
    <Badge
      variant={config.variant}
      className={cn(
        config.className,
        size === 'sm' && 'text-xs py-0 px-1.5',
        size === 'md' && 'text-xs py-0.5 px-2'
      )}
    >
      {showIcon && <Icon className={cn('mr-1', size === 'sm' ? 'h-3 w-3' : 'h-3.5 w-3.5')} />}
      {config.label}
    </Badge>
  );
}

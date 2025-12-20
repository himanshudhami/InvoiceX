import { Badge } from '@/components/ui/badge';
import { CheckCircle, XCircle, Clock, AlertCircle, FileX, Ban } from 'lucide-react';

interface EInvoiceStatusBadgeProps {
  status: string | null | undefined;
  irn?: string | null;
  className?: string;
}

export function EInvoiceStatusBadge({ status, irn, className }: EInvoiceStatusBadgeProps) {
  const normalizedStatus = (status || 'not_applicable').toLowerCase();

  const getStatusConfig = () => {
    switch (normalizedStatus) {
      case 'generated':
        return {
          icon: CheckCircle,
          label: 'IRN Generated',
          variant: 'default' as const,
          className: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100',
        };
      case 'cancelled':
        return {
          icon: Ban,
          label: 'IRN Cancelled',
          variant: 'destructive' as const,
          className: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-100',
        };
      case 'pending':
        return {
          icon: Clock,
          label: 'Pending',
          variant: 'secondary' as const,
          className: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-100',
        };
      case 'error':
      case 'failed':
        return {
          icon: XCircle,
          label: 'Error',
          variant: 'destructive' as const,
          className: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-100',
        };
      case 'not_applicable':
        return {
          icon: FileX,
          label: 'N/A',
          variant: 'outline' as const,
          className: 'text-gray-500',
        };
      case 'required':
        return {
          icon: AlertCircle,
          label: 'IRN Required',
          variant: 'secondary' as const,
          className: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-100',
        };
      default:
        return {
          icon: AlertCircle,
          label: status || 'Unknown',
          variant: 'outline' as const,
          className: '',
        };
    }
  };

  const config = getStatusConfig();
  const Icon = config.icon;

  return (
    <div className={className}>
      <Badge variant={config.variant} className={config.className}>
        <Icon className="h-3 w-3 mr-1" />
        {config.label}
      </Badge>
      {irn && normalizedStatus === 'generated' && (
        <div className="text-xs text-gray-500 mt-1 font-mono truncate max-w-[200px]" title={irn}>
          {irn}
        </div>
      )}
    </div>
  );
}

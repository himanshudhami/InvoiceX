import { PendingApproval } from 'shared-types';
import { ApprovalActionButtons } from './ApprovalActionButtons';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { format, formatDistanceToNow } from 'date-fns';
import { FileText, User, Building, Calendar } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

interface ApprovalRequestListProps {
  approvals: PendingApproval[];
  approverId: string;
  isLoading?: boolean;
  onActionComplete?: () => void;
  emptyMessage?: string;
}

const activityTypeLabels: Record<string, string> = {
  leave: 'Leave Request',
  asset_request: 'Asset Request',
  expense: 'Expense Claim',
  travel: 'Travel Request',
};

export function ApprovalRequestList({
  approvals,
  approverId,
  isLoading = false,
  onActionComplete,
  emptyMessage = 'No pending approvals',
}: ApprovalRequestListProps) {
  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Pending Approvals</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center gap-4 p-4 border rounded-lg">
                <Skeleton className="h-10 w-10 rounded-full" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-4 w-1/2" />
                  <Skeleton className="h-3 w-1/4" />
                </div>
                <Skeleton className="h-9 w-24" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!approvals.length) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center justify-center py-12">
          <FileText className="h-12 w-12 text-gray-300 mb-4" />
          <p className="text-gray-500">{emptyMessage}</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>Pending Approvals</span>
          <Badge variant="secondary">{approvals.length}</Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Request</TableHead>
              <TableHead>Requestor</TableHead>
              <TableHead>Step</TableHead>
              <TableHead>Requested</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {approvals.map((approval) => (
              <TableRow key={`${approval.requestId}-${approval.stepId}`}>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline">
                      {activityTypeLabels[approval.activityType] || approval.activityType}
                    </Badge>
                  </div>
                  <p className="text-sm text-gray-600 mt-1">{approval.activityTitle}</p>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <User className="h-4 w-4 text-gray-400" />
                    <div>
                      <p className="font-medium">{approval.requestorName}</p>
                      {approval.requestorDepartment && (
                        <p className="text-xs text-gray-500 flex items-center gap-1">
                          <Building className="h-3 w-3" />
                          {approval.requestorDepartment}
                        </p>
                      )}
                    </div>
                  </div>
                </TableCell>
                <TableCell>
                  <div>
                    <p className="font-medium">{approval.stepName}</p>
                    <p className="text-xs text-gray-500">
                      Step {approval.stepOrder} of {approval.totalSteps}
                    </p>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1 text-sm text-gray-500">
                    <Calendar className="h-4 w-4" />
                    <span title={format(new Date(approval.requestedAt), 'PPpp')}>
                      {formatDistanceToNow(new Date(approval.requestedAt), { addSuffix: true })}
                    </span>
                  </div>
                </TableCell>
                <TableCell className="text-right">
                  <ApprovalActionButtons
                    requestId={approval.requestId}
                    approverId={approverId}
                    onSuccess={onActionComplete}
                    size="sm"
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

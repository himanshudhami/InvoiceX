import { ApprovalRequestStep, ApprovalStepStatus } from 'shared-types';
import { ApprovalStatusBadge } from './ApprovalStatusBadge';
import { CheckCircle, XCircle, Clock, User } from 'lucide-react';
import { cn } from '@/lib/utils';
import { format } from 'date-fns';

interface ApprovalTimelineProps {
  steps: ApprovalRequestStep[];
  currentStep: number;
}

export function ApprovalTimeline({ steps, currentStep }: ApprovalTimelineProps) {
  const sortedSteps = [...steps].sort((a, b) => a.stepOrder - b.stepOrder);

  return (
    <div className="relative">
      {sortedSteps.map((step, idx) => {
        const isLast = idx === sortedSteps.length - 1;
        const isCurrent = step.stepOrder === currentStep;
        const isCompleted = step.status === 'approved';
        const isRejected = step.status === 'rejected';

        return (
          <div key={step.id} className="relative flex gap-4">
            {/* Timeline connector */}
            {!isLast && (
              <div
                className={cn(
                  'absolute left-4 top-8 w-0.5 h-full -translate-x-1/2',
                  isCompleted ? 'bg-green-500' : 'bg-gray-200'
                )}
              />
            )}

            {/* Step indicator */}
            <div className="relative z-10 flex-shrink-0">
              <div
                className={cn(
                  'w-8 h-8 rounded-full flex items-center justify-center',
                  isCompleted && 'bg-green-500 text-white',
                  isRejected && 'bg-red-500 text-white',
                  isCurrent && !isCompleted && !isRejected && 'bg-blue-500 text-white',
                  !isCurrent && !isCompleted && !isRejected && 'bg-gray-200 text-gray-500'
                )}
              >
                {isCompleted ? (
                  <CheckCircle className="h-4 w-4" />
                ) : isRejected ? (
                  <XCircle className="h-4 w-4" />
                ) : isCurrent ? (
                  <Clock className="h-4 w-4" />
                ) : (
                  <span className="text-sm">{step.stepOrder}</span>
                )}
              </div>
            </div>

            {/* Step content */}
            <div className={cn('flex-1 pb-6', isLast && 'pb-0')}>
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h4 className="font-medium text-gray-900">{step.stepName}</h4>
                  <p className="text-sm text-gray-500">Step {step.stepOrder}</p>
                </div>
                <ApprovalStatusBadge status={step.status} size="sm" />
              </div>

              {/* Assignee info */}
              {step.assignedToName && (
                <div className="mt-2 flex items-center gap-2 text-sm text-gray-600">
                  <User className="h-4 w-4" />
                  <span>
                    {step.status === 'pending' ? 'Assigned to: ' : 'Reviewed by: '}
                    {step.actionByName || step.assignedToName}
                  </span>
                </div>
              )}

              {/* Action info */}
              {step.actionAt && (
                <p className="mt-1 text-xs text-gray-500">
                  {step.status === 'approved' ? 'Approved' : 'Rejected'} on{' '}
                  {format(new Date(step.actionAt), 'MMM dd, yyyy HH:mm')}
                </p>
              )}

              {/* Comments */}
              {step.comments && (
                <div className="mt-2 p-2 bg-gray-50 rounded-md text-sm text-gray-600">
                  <p className="font-medium text-xs text-gray-500 mb-1">Comments:</p>
                  {step.comments}
                </div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

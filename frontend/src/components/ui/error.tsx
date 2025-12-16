import { AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ErrorMessageProps {
  title?: string;
  message: string;
  className?: string;
  showIcon?: boolean;
}

export const ErrorMessage = ({
  title = 'Error',
  message,
  className,
  showIcon = true
}: ErrorMessageProps) => {
  return (
    <div className={cn('rounded-md bg-red-50 p-4', className)}>
      <div className="flex">
        {showIcon && (
          <div className="flex-shrink-0">
            <AlertCircle className="h-5 w-5 text-red-400" />
          </div>
        )}
        <div className={cn('ml-3', !showIcon && 'ml-0')}>
          <h3 className="text-sm font-medium text-red-800">{title}</h3>
          <div className="mt-2 text-sm text-red-700">
            <p>{message}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

interface ErrorPageProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  retryText?: string;
  className?: string;
}

export const ErrorPage = ({
  title = 'Something went wrong',
  message,
  onRetry,
  retryText = 'Try again',
  className
}: ErrorPageProps) => {
  return (
    <div className={cn('flex flex-col items-center justify-center h-64 space-y-4', className)}>
      <ErrorMessage
        title={title}
        message={message}
        className="max-w-md"
      />
      {onRetry && (
        <button
          onClick={onRetry}
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          <RefreshCw className="h-4 w-4 mr-2" />
          {retryText}
        </button>
      )}
    </div>
  );
};

interface FormErrorProps {
  error: string;
  className?: string;
}

export const FormError = ({ error, className }: FormErrorProps) => {
  if (!error) return null;

  return (
    <p className={cn('mt-1 text-sm text-red-600', className)}>
      {error}
    </p>
  );
};

interface FieldErrorProps {
  error?: string;
  className?: string;
}

export const FieldError = ({ error, className }: FieldErrorProps) => {
  if (!error) return null;

  return (
    <p className={cn('mt-1 text-sm text-red-600', className)}>
      {error}
    </p>
  );
};

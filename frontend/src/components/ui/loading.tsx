import { cn } from '@/lib/utils';

interface LoadingProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  text?: string;
}

export const Loading = ({
  size = 'md',
  className,
  text = 'Loading...'
}: LoadingProps) => {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-8 w-8',
    lg: 'h-12 w-12'
  };

  return (
    <div className={cn('flex flex-col items-center justify-center space-y-2', className)}>
      <div
        className={cn(
          'animate-spin rounded-full border-b-2 border-blue-600',
          sizeClasses[size]
        )}
      />
      {text && (
        <p className="text-sm text-gray-600">{text}</p>
      )}
    </div>
  );
};

interface LoadingPageProps {
  text?: string;
  className?: string;
}

export const LoadingPage = ({
  text = 'Loading...',
  className
}: LoadingPageProps) => {
  return (
    <div className={cn('flex items-center justify-center h-64', className)}>
      <Loading size="lg" text={text} />
    </div>
  );
};

interface LoadingButtonProps {
  loading: boolean;
  children: React.ReactNode;
  loadingText?: string;
  className?: string;
  disabled?: boolean;
  onClick?: () => void;
}

export const LoadingButton = ({
  loading,
  children,
  loadingText = 'Saving...',
  className,
  disabled,
  onClick
}: LoadingButtonProps) => {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={loading || disabled}
      className={cn(
        'inline-flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm',
        'text-white bg-blue-600 hover:bg-blue-700',
        'focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500',
        'disabled:opacity-50 disabled:cursor-not-allowed',
        className
      )}
    >
      {loading ? (
        <>
          <Loading size="sm" className="mr-2" />
          {loadingText}
        </>
      ) : (
        children
      )}
    </button>
  );
};

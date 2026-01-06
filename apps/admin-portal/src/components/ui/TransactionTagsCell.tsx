import { useTransactionTags } from '@/features/tags/hooks';
import { TagDisplay } from './TagPicker';

interface TransactionTagsCellProps {
  transactionId: string;
  transactionType: 'invoice' | 'payment' | 'vendor_invoice' | 'vendor_payment' | 'journal_entry';
  maxDisplay?: number;
}

/**
 * Cell component for displaying tags in transaction list tables.
 * Lazily fetches tags for the transaction and displays them.
 */
export const TransactionTagsCell = ({
  transactionId,
  transactionType,
  maxDisplay = 2,
}: TransactionTagsCellProps) => {
  const { data: tags = [], isLoading } = useTransactionTags(
    transactionId,
    transactionType,
    true
  );

  if (isLoading) {
    return (
      <div className="flex items-center gap-1">
        <div className="h-4 w-12 bg-gray-100 animate-pulse rounded" />
      </div>
    );
  }

  if (tags.length === 0) {
    return <span className="text-xs text-gray-400">—</span>;
  }

  return <TagDisplay tags={tags} maxDisplay={maxDisplay} />;
};

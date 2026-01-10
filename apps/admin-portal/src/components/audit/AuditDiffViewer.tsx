interface AuditDiffViewerProps {
  oldValues?: Record<string, unknown>;
  newValues?: Record<string, unknown>;
  changedFields?: string[];
  operation: 'create' | 'update' | 'delete';
}

const HIDDEN_FIELDS = ['id', 'createdAt', 'updatedAt', 'companyId'];

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '-';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (typeof value === 'object') return JSON.stringify(value, null, 2);
  return String(value);
}

function formatFieldName(field: string): string {
  return field
    .replace(/([A-Z])/g, ' $1')
    .replace(/_/g, ' ')
    .trim()
    .split(' ')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}

function getSortedFields(
  oldValues?: Record<string, unknown>,
  newValues?: Record<string, unknown>,
  changedFields?: string[]
): string[] {
  const allFields = new Set<string>([
    ...Object.keys(oldValues || {}),
    ...Object.keys(newValues || {}),
  ]);

  return Array.from(allFields)
    .filter((field) => !field.startsWith('_') && !HIDDEN_FIELDS.includes(field))
    .sort((a, b) => {
      const aChanged = changedFields?.includes(a);
      const bChanged = changedFields?.includes(b);
      if (aChanged && !bChanged) return -1;
      if (!aChanged && bChanged) return 1;
      return a.localeCompare(b);
    });
}

interface SingleColumnTableProps {
  title: string;
  fields: string[];
  values?: Record<string, unknown>;
  colorScheme: 'green' | 'red';
  strikethrough?: boolean;
}

function SingleColumnTable({ title, fields, values, colorScheme, strikethrough }: SingleColumnTableProps) {
  const colors = {
    green: { bg: 'bg-green-50', border: 'border-green-200', row: 'border-green-100', header: 'text-green-800', code: 'text-green-700 bg-green-100' },
    red: { bg: 'bg-red-50', border: 'border-red-200', row: 'border-red-100', header: 'text-red-800', code: 'text-red-700 bg-red-100' },
  }[colorScheme];

  return (
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-gray-700">{title}</h4>
      <div className={`${colors.bg} rounded-lg p-4 max-h-96 overflow-auto`}>
        <table className="min-w-full text-sm">
          <thead>
            <tr className={`border-b ${colors.border}`}>
              <th className={`text-left py-2 px-2 font-medium ${colors.header}`}>Field</th>
              <th className={`text-left py-2 px-2 font-medium ${colors.header}`}>Value</th>
            </tr>
          </thead>
          <tbody>
            {fields.map((field) => (
              <tr key={field} className={`border-b ${colors.row} last:border-0`}>
                <td className="py-2 px-2 font-medium text-gray-700">{formatFieldName(field)}</td>
                <td className="py-2 px-2">
                  <code className={`${colors.code} px-1 rounded text-xs ${strikethrough ? 'line-through' : ''}`}>
                    {formatValue(values?.[field])}
                  </code>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export function AuditDiffViewer({ oldValues, newValues, changedFields, operation }: AuditDiffViewerProps) {
  const sortedFields = getSortedFields(oldValues, newValues, changedFields);

  if (operation === 'create') {
    return <SingleColumnTable title="Created Values" fields={sortedFields} values={newValues} colorScheme="green" />;
  }

  if (operation === 'delete') {
    return <SingleColumnTable title="Deleted Values" fields={sortedFields} values={oldValues} colorScheme="red" strikethrough />;
  }

  return (
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-gray-700">
        Changes ({changedFields?.length || 0} fields modified)
      </h4>
      <div className="bg-white border rounded-lg max-h-96 overflow-auto">
        <table className="min-w-full text-sm">
          <thead className="bg-gray-50 sticky top-0">
            <tr>
              <th className="text-left py-2 px-3 font-medium text-gray-600 w-1/4">Field</th>
              <th className="text-left py-2 px-3 font-medium text-gray-600 w-5/12">Before</th>
              <th className="text-left py-2 px-3 font-medium text-gray-600 w-5/12">After</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {sortedFields.map((field) => {
              const isChanged = changedFields?.includes(field);
              const oldVal = oldValues?.[field];
              const newVal = newValues?.[field];

              return (
                <tr key={field} className={isChanged ? 'bg-yellow-50' : 'hover:bg-gray-50'}>
                  <td className="py-2 px-3 font-medium text-gray-700">
                    {formatFieldName(field)}
                    {isChanged && <span className="ml-1 text-yellow-600 text-xs">*</span>}
                  </td>
                  <td className="py-2 px-3">
                    {oldVal !== undefined ? (
                      <code className={`px-1 rounded text-xs ${isChanged ? 'text-red-700 bg-red-50' : 'text-gray-600 bg-gray-100'}`}>
                        {formatValue(oldVal)}
                      </code>
                    ) : (
                      <span className="text-gray-400">-</span>
                    )}
                  </td>
                  <td className="py-2 px-3">
                    {newVal !== undefined ? (
                      <code className={`px-1 rounded text-xs ${isChanged ? 'text-green-700 bg-green-50' : 'text-gray-600 bg-gray-100'}`}>
                        {formatValue(newVal)}
                      </code>
                    ) : (
                      <span className="text-gray-400">-</span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

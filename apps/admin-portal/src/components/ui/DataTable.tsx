import { useState, useMemo, ReactNode, useEffect, useRef } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  flexRender,
  SortingState,
  ColumnFiltersState,
  VisibilityState,
  ColumnDef,
  Table,
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import { Loader2 } from 'lucide-react'

interface ServerPaginationConfig {
  pageIndex: number
  pageSize: number
  totalCount: number
  onPageChange: (pageIndex: number) => void
  onPageSizeChange: (pageSize: number) => void
}

/** Extended column meta for summary and alignment */
export interface DataTableColumnMeta {
  /** Auto-compute summary in footer: 'sum', 'count', 'avg', or custom function */
  summary?: 'sum' | 'count' | 'avg' | ((rows: unknown[]) => ReactNode)
  /** Cell alignment */
  align?: 'left' | 'center' | 'right'
  /** Shorthand for align: 'right' (for numeric columns) */
  numeric?: boolean
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  searchPlaceholder?: string
  searchValue?: string
  initialSearch?: string
  onSearchChange?: (value: string) => void
  onAdd?: () => void
  addButtonText?: string
  /** Hide the default toolbar entirely and render your own via renderToolbar */
  showToolbar?: boolean
  /** Provide a custom toolbar; receive the table instance */
  renderToolbar?: (table: Table<TData>) => React.ReactNode
  /** Render a uniform footer; receives the table instance */
  footer?: (table: Table<TData>) => React.ReactNode
  /** Optional totals row helper: label text and key/value pairs to render */
  totalsFooter?: {
    label?: string
    values: { label: string; value: React.ReactNode }[]
  }
  /** For server-side mode: pre-computed totals from API (keyed by column accessorKey) */
  serverTotals?: Record<string, ReactNode>
  /** For client-side tables: override initial page size */
  pageSizeOverride?: number
  /**
   * When provided, DataTable will behave in server-side pagination mode:
   * - Pagination UI is driven by this object
   * - Page/pageSize changes call back to the parent instead of changing React Table's internal state only
   */
  pagination?: ServerPaginationConfig
  hidePaginationControls?: boolean
  /** Show loading state with spinner overlay and disabled controls */
  isLoading?: boolean
  /** ID of row to highlight (will scroll into view and apply highlight styling) */
  highlightedRowId?: string
  /** Key accessor to get row ID from data (defaults to 'id') */
  rowIdAccessor?: keyof TData | ((row: TData) => string)
}

export function DataTable<TData, TValue>({
  columns,
  data,
  searchPlaceholder = "Search...",
  searchValue,
  initialSearch = '',
  onSearchChange,
  onAdd,
  addButtonText = "Add New",
  showToolbar = true,
  renderToolbar,
  footer,
  totalsFooter,
  serverTotals,
  pageSizeOverride,
  pagination,
  hidePaginationControls = false,
  isLoading = false,
  highlightedRowId,
  rowIdAccessor = 'id' as keyof TData,
}: DataTableProps<TData, TValue>) {
  // Ref for scrolling to highlighted row
  const highlightedRowRef = useRef<HTMLTableRowElement>(null)

  // Helper to get row ID (only called when highlighting is needed)
  const getRowId = (row: TData): string => {
    if (!row) return ''
    if (typeof rowIdAccessor === 'function') {
      return rowIdAccessor(row)
    }
    const value = (row as Record<string, unknown>)[rowIdAccessor as string]
    return value != null ? String(value) : ''
  }

  // Scroll to highlighted row when data loads
  useEffect(() => {
    if (highlightedRowId && highlightedRowRef.current) {
      setTimeout(() => {
        highlightedRowRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })
      }, 100)
    }
  }, [highlightedRowId, data])

  // Detect server-side mode
  const isServerMode = !!pagination
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [globalFilter, setGlobalFilter] = useState(initialSearch)

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: isServerMode ? undefined : getSortedRowModel(),
    getPaginationRowModel: isServerMode ? undefined : getPaginationRowModel(),
    getFilteredRowModel: searchValue === undefined ? getFilteredRowModel() : undefined,
    manualPagination: isServerMode,
    pageCount: isServerMode && pagination ? Math.ceil(pagination.totalCount / pagination.pageSize) : undefined,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onGlobalFilterChange: setGlobalFilter,
    globalFilterFn: 'includesString',
    initialState: {
      pagination: {
        pageSize: pagination?.pageSize || pageSizeOverride || 100,
        pageIndex: pagination?.pageIndex ?? 0,
      },
    },
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      ...(pagination
        ? {
            pagination: {
              pageIndex: pagination.pageIndex,
              pageSize: pagination.pageSize,
            },
          }
        : {}),
      globalFilter: searchValue === undefined ? globalFilter : '',
    },
  })

  // Compute summary footer values from column meta
  const summaryFooterValues = useMemo(() => {
    // Skip if custom footer is provided or no columns have summary meta
    const summaryColumns = columns.filter(col => {
      const meta = col.meta as DataTableColumnMeta | undefined
      return meta?.summary
    })
    if (summaryColumns.length === 0) return null

    const rows = isServerMode ? data : table.getFilteredRowModel().rows.map(r => r.original)

    return columns.map(col => {
      const meta = col.meta as DataTableColumnMeta | undefined
      const accessorKey = (col as any).accessorKey as string | undefined

      if (!meta?.summary || !accessorKey) return null

      // Use server totals if provided
      if (serverTotals && accessorKey in serverTotals) {
        return { key: accessorKey, value: serverTotals[accessorKey] }
      }

      // Compute client-side
      const values = rows.map(row => {
        const val = (row as any)[accessorKey]
        return typeof val === 'number' ? val : parseFloat(val) || 0
      })

      let computed: ReactNode
      if (typeof meta.summary === 'function') {
        computed = meta.summary(rows)
      } else if (meta.summary === 'sum') {
        computed = values.reduce((a, b) => a + b, 0)
      } else if (meta.summary === 'count') {
        computed = values.length
      } else if (meta.summary === 'avg') {
        computed = values.length > 0 ? values.reduce((a, b) => a + b, 0) / values.length : 0
      }

      return { key: accessorKey, value: computed }
    }).filter(Boolean) as { key: string; value: ReactNode }[]
  }, [columns, data, table, isServerMode, serverTotals])

  // Keep table page size in sync with explicit overrides when not in server-side mode
  if (!pagination && pageSizeOverride && table.getState().pagination.pageSize !== pageSizeOverride) {
    table.setPageSize(pageSizeOverride)
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      {showToolbar && (
        renderToolbar ? (
          <div className="flex items-center justify-between">
            {renderToolbar(table)}
          </div>
        ) : (
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <input
                placeholder={searchPlaceholder}
                value={searchValue ?? globalFilter ?? ''}
                onChange={(event) => {
                  const val = event.target.value
                  // Only update globalFilter for client-side filtering if searchValue is not provided
                  if (searchValue === undefined) {
                    setGlobalFilter(val)
                  }
                  // Always call onSearchChange for server-side search
                  onSearchChange?.(val)
                }}
                className="max-w-sm px-3 py-2 border border-gray-200 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            {onAdd && (
              <button
                onClick={onAdd}
                className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 transition-colors"
              >
                {addButtonText}
              </button>
            )}
          </div>
        )
      )}

      {/* Table */}
      <div className="rounded-md border overflow-hidden relative">
        {/* Loading overlay */}
        {isLoading && (
          <div className="absolute inset-0 bg-white/60 z-10 flex items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        )}
        <div className="max-h-[70vh] overflow-auto">
        <table className="w-full">
          <thead className="bg-gray-50 sticky top-0 z-[5]">
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  const meta = header.column.columnDef.meta as DataTableColumnMeta | undefined
                  const align = meta?.align || (meta?.numeric ? 'right' : 'left')
                  return (
                    <th
                      key={header.id}
                      className={cn(
                        "px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 bg-gray-50",
                        align === 'right' && "text-right",
                        align === 'center' && "text-center",
                        align === 'left' && "text-left"
                      )}
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      <div className={cn(
                        "flex items-center space-x-1",
                        align === 'right' && "justify-end",
                        align === 'center' && "justify-center"
                      )}>
                        <span>
                          {header.isPlaceholder
                            ? null
                            : flexRender(header.column.columnDef.header, header.getContext())}
                        </span>
                        <span className="text-gray-400">
                          {header.column.getIsSorted() === 'desc' ? '↓' :
                           header.column.getIsSorted() === 'asc' ? '↑' :
                           header.column.getCanSort() ? '↕' : null}
                        </span>
                      </div>
                    </th>
                  )
                })}
              </tr>
            ))}
          </thead>
          <tbody className={cn("bg-white divide-y divide-gray-200", isLoading && "opacity-50")}>
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => {
                // Only compute highlight if highlightedRowId is provided
                const isHighlighted = highlightedRowId
                  ? getRowId(row.original) === highlightedRowId
                  : false

                return (
                <tr
                  key={row.id}
                  ref={isHighlighted ? highlightedRowRef : undefined}
                  className={cn(
                    "hover:bg-gray-50 transition-colors",
                    isHighlighted && "bg-blue-50 ring-2 ring-blue-400 ring-inset"
                  )}
                >
                  {row.getVisibleCells().map((cell) => {
                    const meta = cell.column.columnDef.meta as DataTableColumnMeta | undefined
                    const align = meta?.align || (meta?.numeric ? 'right' : 'left')
                    return (
                      <td
                        key={cell.id}
                        className={cn(
                          "px-6 py-4 whitespace-nowrap text-sm text-gray-900",
                          align === 'right' && "text-right",
                          align === 'center' && "text-center"
                        )}
                      >
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    )
                  })}
                </tr>
              )})
            ) : (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                  No results found.
                </td>
              </tr>
            )}
          </tbody>
          {footer && footer(table)}
          {totalsFooter && (
            <tfoot className="bg-gray-50 text-sm font-medium text-gray-800">
              <tr>
                <td className="px-6 py-3" colSpan={Math.max(1, columns.length - totalsFooter.values.length)}>
                  {totalsFooter.label ?? 'Totals'}
                </td>
                {totalsFooter.values.map((item, idx) => (
                  <td key={idx} className="px-6 py-3 whitespace-nowrap">
                    <div className="flex flex-col">
                      <span className="text-xs text-gray-500">{item.label}</span>
                      <span>{item.value}</span>
                    </div>
                  </td>
                ))}
              </tr>
            </tfoot>
          )}
          {/* Auto-computed summary footer from column meta */}
          {!footer && !totalsFooter && summaryFooterValues && summaryFooterValues.length > 0 && (
            <tfoot className="bg-gray-100 border-t-2 border-gray-300 text-sm font-semibold text-gray-900">
              <tr>
                {columns.map((col, idx) => {
                  const accessorKey = (col as any).accessorKey as string | undefined
                  const summaryItem = summaryFooterValues.find(s => s.key === accessorKey)
                  const meta = col.meta as DataTableColumnMeta | undefined
                  const align = meta?.align || (meta?.numeric ? 'right' : 'left')

                  if (idx === 0 && !summaryItem) {
                    return (
                      <td key={idx} className="px-6 py-3">
                        Totals ({isServerMode ? pagination?.totalCount : table.getFilteredRowModel().rows.length} rows)
                      </td>
                    )
                  }

                  return (
                    <td
                      key={idx}
                      className={cn(
                        "px-6 py-3",
                        align === 'right' && "text-right",
                        align === 'center' && "text-center"
                      )}
                    >
                      {summaryItem?.value ?? ''}
                    </td>
                  )
                })}
              </tr>
            </tfoot>
          )}
        </table>
        </div>
      </div>

      {!hidePaginationControls && (
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <span className="text-sm text-gray-700">
              Page {(pagination?.pageIndex ?? table.getState().pagination.pageIndex) + 1}{' '}
              of {pagination ? Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize)) : table.getPageCount()}
            </span>
            <span className="text-sm text-gray-500">
              ({pagination?.totalCount ?? table.getFilteredRowModel().rows.length} total rows)
            </span>
            <PageSizeSelect
              value={pagination?.pageSize ?? table.getState().pagination.pageSize}
              onChange={(size) => {
                if (pagination) {
                  pagination.onPageSizeChange(size)
                } else {
                  table.setPageSize(size)
                }
              }}
            />
          </div>
          
          <div className="flex items-center space-x-2">
            <button
              onClick={() => {
                if (pagination) {
                  if (pagination.pageIndex > 0) {
                    pagination.onPageChange(pagination.pageIndex - 1)
                  }
                } else {
                  table.previousPage()
                }
              }}
              disabled={
                isLoading || (pagination ? pagination.pageIndex <= 0 : !table.getCanPreviousPage())
              }
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                isLoading
                  ? "bg-gray-100 text-gray-400 cursor-not-allowed"
                  : pagination
                  ? pagination.pageIndex > 0
                    ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    : "bg-gray-100 text-gray-400 cursor-not-allowed"
                  : table.getCanPreviousPage()
                  ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                  : "bg-gray-100 text-gray-400 cursor-not-allowed"
              )}
            >
              Previous
            </button>

            <div className="flex items-center space-x-1">
              {Array.from(
                {
                  length: Math.min(
                    5,
                    pagination
                      ? Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize))
                      : table.getPageCount()
                  ),
                },
                (_, i) => {
                  const currentIndex =
                    pagination?.pageIndex ?? table.getState().pagination.pageIndex
                  const pageCount = pagination
                    ? Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize))
                    : table.getPageCount()
                  const startPage = Math.max(0, currentIndex - 2)
                  const page = startPage + i

                  if (page >= pageCount) return null

                  return (
                    <button
                      key={page}
                      disabled={isLoading}
                      onClick={() => {
                        if (pagination) {
                          pagination.onPageChange(page)
                        } else {
                          table.setPageIndex(page)
                        }
                      }}
                      className={cn(
                        "w-8 h-8 rounded text-sm transition-colors",
                        isLoading
                          ? "bg-gray-100 text-gray-400 cursor-not-allowed"
                          : page === currentIndex
                          ? "bg-primary text-primary-foreground"
                          : "bg-gray-200 hover:bg-gray-300 text-gray-700"
                      )}
                    >
                      {page + 1}
                    </button>
                  );
                }
              )}
            </div>
            
            <button
              onClick={() => {
                if (pagination) {
                  const pageCount = Math.max(
                    1,
                    Math.ceil(pagination.totalCount / pagination.pageSize)
                  )
                  if (pagination.pageIndex < pageCount - 1) {
                    pagination.onPageChange(pagination.pageIndex + 1)
                  }
                } else {
                  table.nextPage()
                }
              }}
              disabled={
                isLoading || (pagination
                  ? pagination.pageIndex >=
                    Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize)) - 1
                  : !table.getCanNextPage())
              }
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                isLoading
                  ? "bg-gray-100 text-gray-400 cursor-not-allowed"
                  : pagination
                  ? pagination.pageIndex <
                    Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize)) - 1
                    ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    : "bg-gray-100 text-gray-400 cursor-not-allowed"
                  : table.getCanNextPage()
                  ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                  : "bg-gray-100 text-gray-400 cursor-not-allowed"
              )}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

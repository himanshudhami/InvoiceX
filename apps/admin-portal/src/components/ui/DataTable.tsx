import { useState } from 'react'
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

interface ServerPaginationConfig {
  pageIndex: number
  pageSize: number
  totalCount: number
  onPageChange: (pageIndex: number) => void
  onPageSizeChange: (pageSize: number) => void
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
  /** For client-side tables: override initial page size */
  pageSizeOverride?: number
  /**
   * When provided, DataTable will behave in server-side pagination mode:
   * - Pagination UI is driven by this object
   * - Page/pageSize changes call back to the parent instead of changing React Table's internal state only
   */
  pagination?: ServerPaginationConfig
  hidePaginationControls?: boolean
  /** Optional footer row renderer - receives the table instance and should return a tfoot element */
  footer?: (table: Table<TData>) => React.ReactNode
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
  pageSizeOverride,
  pagination,
  hidePaginationControls = false,
}: DataTableProps<TData, TValue>) {
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [globalFilter, setGlobalFilter] = useState(initialSearch)

  const tableConfig: any = {
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
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
      // Only use globalFilter for client-side filtering when searchValue is not provided
      globalFilter: searchValue === undefined ? globalFilter : '',
    },
  }
  
  // Only use client-side filtering if we're not using server-side search
  if (searchValue === undefined) {
    tableConfig.getFilteredRowModel = getFilteredRowModel()
  }
  
  const table = useReactTable(tableConfig)

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
      <div className="rounded-md border overflow-hidden">
        <div className="max-h-[70vh] overflow-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <th
                    key={header.id}
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                    onClick={header.column.getToggleSortingHandler()}
                  >
                    <div className="flex items-center space-x-1">
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
                ))}
              </tr>
            ))}
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <tr
                  key={row.id}
                  className="hover:bg-gray-50 transition-colors"
                >
                  {row.getVisibleCells().map((cell) => (
                    <td key={cell.id} className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
              ))
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
                pagination ? pagination.pageIndex <= 0 : !table.getCanPreviousPage()
              }
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                pagination
                  ? pagination.pageIndex > 0
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
                      onClick={() => {
                        if (pagination) {
                          pagination.onPageChange(page)
                        } else {
                          table.setPageIndex(page)
                        }
                      }}
                      className={cn(
                        "w-8 h-8 rounded text-sm transition-colors",
                        page === currentIndex
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
                pagination
                  ? pagination.pageIndex >=
                    Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize)) - 1
                  : !table.getCanNextPage()
              }
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                pagination
                  ? pagination.pageIndex <
                    Math.max(1, Math.ceil(pagination.totalCount / pagination.pageSize)) - 1
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

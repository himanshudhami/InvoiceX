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
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  searchPlaceholder?: string
  searchValue?: string
  initialSearch?: string
  onSearchChange?: (value: string) => void
  onAdd?: () => void
  addButtonText?: string
  pageSizeOverride?: number
  hidePaginationControls?: boolean
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
  pageSizeOverride,
  hidePaginationControls = false
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
    initialState: { pagination: { pageSize: pageSizeOverride || 20 } },
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      // Only use globalFilter for client-side filtering when searchValue is not provided
      globalFilter: searchValue === undefined ? globalFilter : '',
    },
  }
  
  // Only use client-side filtering if we're not using server-side search
  if (searchValue === undefined) {
    tableConfig.getFilteredRowModel = getFilteredRowModel()
  }
  
  const table = useReactTable(tableConfig)

  // Keep table page size in sync with server-driven page size when provided
  if (pageSizeOverride && table.getState().pagination.pageSize !== pageSizeOverride) {
    table.setPageSize(pageSizeOverride)
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
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
            className="max-w-sm px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
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
        </table>
        </div>
      </div>

      {!hidePaginationControls && (
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <span className="text-sm text-gray-700">
              Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
            </span>
            <span className="text-sm text-gray-500">
              ({table.getFilteredRowModel().rows.length} total rows)
            </span>
            <PageSizeSelect
              value={table.getState().pagination.pageSize}
              onChange={(size) => table.setPageSize(size)}
            />
          </div>
          
          <div className="flex items-center space-x-2">
            <button
              onClick={() => table.previousPage()}
              disabled={!table.getCanPreviousPage()}
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                table.getCanPreviousPage()
                  ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                  : "bg-gray-100 text-gray-400 cursor-not-allowed"
              )}
            >
              Previous
            </button>
            
            <div className="flex items-center space-x-1">
              {Array.from({ length: Math.min(5, table.getPageCount()) }, (_, i) => {
                const pageIndex = table.getState().pagination.pageIndex;
                const startPage = Math.max(0, pageIndex - 2);
                const page = startPage + i;
                
                if (page >= table.getPageCount()) return null;
                
                return (
                  <button
                    key={page}
                    onClick={() => table.setPageIndex(page)}
                    className={cn(
                      "w-8 h-8 rounded text-sm transition-colors",
                      page === pageIndex
                        ? "bg-primary text-primary-foreground"
                        : "bg-gray-200 hover:bg-gray-300 text-gray-700"
                    )}
                  >
                    {page + 1}
                  </button>
                );
              })}
            </div>
            
            <button
              onClick={() => table.nextPage()}
              disabled={!table.getCanNextPage()}
              className={cn(
                "px-3 py-1 rounded-md text-sm transition-colors",
                table.getCanNextPage()
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

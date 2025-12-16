import { useMemo, useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import { useEmployeesPaged, useDeleteEmployee, useEmployees, useRejoinEmployee } from '@/hooks/api/useEmployees'
import { Employee, PagedResponse } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { EmployeeForm } from '@/components/forms/EmployeeForm'
import { EmployeeBulkUploadModal } from '@/components/forms/EmployeeBulkUploadModal'
import { ResignEmployeeModal } from '@/components/modals/ResignEmployeeModal'
import { EmployeeSidePanel } from '@/components/employee/EmployeeSidePanel'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Edit, Trash2, User, Phone, Mail, Calendar, MapPin, Badge, Upload, Eye, UserMinus, UserPlus } from 'lucide-react'
import { format } from 'date-fns'
import { PageSizeSelect } from '@/components/ui/PageSizeSelect'
import { cn } from '@/lib/utils'

// Reusable filter select component to avoid code duplication
const FilterSelect = ({
  label,
  value,
  options,
  onChange,
  allOptionLabel = 'All',
  getOptionValue,
  getOptionLabel,
}: {
  label: string
  value: string
  options: string[]
  onChange: (value: string) => void
  allOptionLabel?: string
  getOptionValue?: (option: string) => string
  getOptionLabel?: (option: string) => string
}) => {
  const getValue = getOptionValue || ((opt: string) => opt)
  const getLabel = getOptionLabel || ((opt: string) => opt)
  
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="all">{allOptionLabel}</option>
        {options.map((option) => (
          <option key={getValue(option)} value={getValue(option)}>
            {getLabel(option)}
          </option>
        ))}
      </select>
    </div>
  )
}

const EmployeesManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [isBulkUploadOpen, setIsBulkUploadOpen] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null)
  const [deletingEmployee, setDeletingEmployee] = useState<Employee | null>(null)
  const [resigningEmployee, setResigningEmployee] = useState<Employee | null>(null)
  const [sortBy] = useState('employeeName')
  const [sortDescending] = useState(false)

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(10),
      search: parseAsString.withDefault(''),
      status: parseAsString.withDefault('all'),
      company: parseAsString.withDefault(''),
      department: parseAsString.withDefault('all'),
      contractType: parseAsString.withDefault('all'),
      selectedEmployee: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  // Get all employees once to populate department filter options
  const { data: allEmployees = [] } = useEmployees()

  // Debounced search term
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(urlState.search)
  
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(urlState.search)
    }, 300)
    
    return () => clearTimeout(timer)
  }, [urlState.search])

  const { data, error, refetch } = useEmployeesPaged({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined, // Don't send empty string
    sortBy,
    sortDescending,
    status: urlState.status !== 'all' ? urlState.status : undefined,
    companyId: urlState.company || undefined,
    department: urlState.department !== 'all' ? urlState.department : undefined,
    contractType: urlState.contractType !== 'all' ? urlState.contractType : undefined,
  })

  const deleteEmployee = useDeleteEmployee()
  const rejoinEmployee = useRejoinEmployee()

  const statusOptions = ['all', 'active', 'inactive', 'terminated', 'resigned', 'permanent']
  
  // Get department options from all employees, not just current page
  const departmentOptions = useMemo(
    () => {
      const departments = Array.from(
        new Set(allEmployees.map((e) => e.department).filter(Boolean))
      ) as string[]
      return departments.sort()
    },
    [allEmployees]
  )

  // Get contract type options from all employees, not just current page
  const contractTypeOptions = useMemo(
    () => {
      const contractTypes = Array.from(
        new Set(allEmployees.map((e) => e.contractType).filter(Boolean))
      ) as string[]
      return contractTypes.sort()
    },
    [allEmployees]
  )

  const handleEdit = (employee: Employee) => {
    setEditingEmployee(employee)
  }

  const handleDelete = (employee: Employee) => {
    setDeletingEmployee(employee)
  }

  const handleDeleteConfirm = async () => {
    if (deletingEmployee) {
      try {
        await deleteEmployee.mutateAsync(deletingEmployee.id)
        setDeletingEmployee(null)
        refetch()
      } catch (error) {
        console.error('Failed to delete employee:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingEmployee(null)
    refetch()
  }

  const handleResign = (employee: Employee) => {
    setResigningEmployee(employee)
  }

  const handleRejoin = async (employee: Employee) => {
    try {
      await rejoinEmployee.mutateAsync({ id: employee.id })
      refetch()
    } catch (error) {
      console.error('Failed to rejoin employee:', error)
    }
  }

  const getStatusBadge = (status: string) => {
    const colors = {
      active: 'bg-green-100 text-green-800',
      inactive: 'bg-yellow-100 text-yellow-800',
      terminated: 'bg-red-100 text-red-800',
      resigned: 'bg-orange-100 text-orange-800'
    }
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${colors[status as keyof typeof colors] || 'bg-gray-100 text-gray-800'}`}>
        {status?.charAt(0).toUpperCase() + status?.slice(1)}
      </span>
    )
  }

  const columns: ColumnDef<Employee>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => {
        const employee = row.original
        return (
          <div className="flex items-start space-x-3">
            <div className="flex-shrink-0 w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
              <User className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{employee.employeeName}</div>
              {employee.employeeId && (
                <div className="text-sm text-gray-500 flex items-center mt-1">
                  <Badge className="w-3 h-3 mr-1" />
                  ID: {employee.employeeId}
                </div>
              )}
              {employee.email && (
                <div className="text-sm text-gray-500 flex items-center mt-1">
                  <Mail className="w-3 h-3 mr-1" />
                  {employee.email}
                </div>
              )}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'department',
      header: 'Department',
      cell: ({ row }) => {
        const employee = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">
              {employee.department || 'N/A'}
            </div>
            {employee.designation && (
              <div className="text-sm text-gray-500">{employee.designation}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'contact',
      header: 'Contact',
      cell: ({ row }) => {
        const employee = row.original
        return (
          <div className="space-y-1">
            {employee.phone && (
              <div className="text-sm text-gray-900 flex items-center">
                <Phone className="w-3 h-3 mr-1" />
                {employee.phone}
              </div>
            )}
            {employee.city && (
              <div className="text-sm text-gray-500 flex items-center">
                <MapPin className="w-3 h-3 mr-1" />
                {employee.city}, {employee.state}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'hireDate',
      header: 'Hire Date',
      cell: ({ row }) => {
        const hireDate = row.original.hireDate
        if (!hireDate) return 'N/A'
        
        try {
          return (
            <div className="text-sm text-gray-900 flex items-center">
              <Calendar className="w-3 h-3 mr-1" />
              {format(new Date(hireDate), 'MMM dd, yyyy')}
            </div>
          )
        } catch {
          return 'N/A'
        }
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      accessorKey: 'contractType',
      header: 'Contract Type',
      cell: ({ row }) => {
        const contractType = row.original.contractType
        return (
          <div className="text-sm text-gray-900">
            {contractType || 'N/A'}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const employee = row.original
        return (
          <div className="flex items-center justify-end space-x-2">
            <button
              onClick={() => setUrlState({ selectedEmployee: employee.id })}
              className="text-indigo-600 hover:text-indigo-800 p-1 rounded hover:bg-indigo-50 transition-colors"
              title="View employee"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => handleEdit(employee)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit employee"
            >
              <Edit size={16} />
            </button>
            {employee.status === 'active' && (
              <button
                onClick={() => handleResign(employee)}
                className="text-orange-600 hover:text-orange-800 p-1 rounded hover:bg-orange-50 transition-colors"
                title="Resign employee"
              >
                <UserMinus size={16} />
              </button>
            )}
            {employee.status === 'resigned' && (
              <button
                onClick={() => handleRejoin(employee)}
                disabled={rejoinEmployee.isPending}
                className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors disabled:opacity-50"
                title="Rejoin employee"
              >
                <UserPlus size={16} />
              </button>
            )}
            <button
              onClick={() => handleDelete(employee)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete employee"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-600 mb-4">Failed to load employees</p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            Try Again
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className={cn(
      "space-y-6 transition-all duration-300",
      urlState.selectedEmployee && "mr-[520px]"
    )}>
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Employee Management</h1>
          <p className="text-gray-600 mt-1">Manage your employees and their information</p>
        </div>
        <div className="flex items-center space-x-3">
          <button
            onClick={() => setIsBulkUploadOpen(true)}
            className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors flex items-center space-x-2"
          >
            <Upload className="w-4 h-4" />
            <span>Bulk Upload</span>
          </button>
          <button
            onClick={() => setIsCreateDrawerOpen(true)}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center space-x-2"
          >
            <User className="w-4 h-4" />
            <span>Add Employee</span>
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow px-4 py-4 space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Search</label>
            <input
              type="text"
              value={urlState.search}
              onChange={(e) => {
                setUrlState({ search: e.target.value, page: 1 })
              }}
              placeholder="Search name, email, ID..."
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <FilterSelect
            label="Status"
            value={urlState.status}
            options={statusOptions.filter((s) => s !== 'all')}
            onChange={(value) => {
              setUrlState({ status: value, page: 1 })
            }}
            getOptionLabel={(opt) => opt.charAt(0).toUpperCase() + opt.slice(1)}
          />
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
            <CompanyFilterDropdown
              value={urlState.company}
              onChange={(val) => {
                setUrlState({ company: val || '', page: 1 })
              }}
            />
          </div>
          <FilterSelect
            label="Department"
            value={urlState.department}
            options={departmentOptions}
            onChange={(value) => {
              setUrlState({ department: value, page: 1 })
            }}
          />
          <FilterSelect
            label="Contract Type"
            value={urlState.contractType}
            options={contractTypeOptions}
            onChange={(value) => {
              setUrlState({ contractType: value, page: 1 })
            }}
          />
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow overflow-x-auto">
        <DataTable
          columns={columns}
          data={(data as PagedResponse<Employee> | undefined)?.items || []}
          searchPlaceholder="Search employees..."
          pageSizeOverride={urlState.pageSize}
          hidePaginationControls
        />
      </div>

      {/* Server Pagination */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 bg-white rounded-lg shadow px-4 py-3">
        <div className="text-sm text-gray-700">
          {(() => {
            const typedData = data as PagedResponse<Employee> | undefined
            return `${typedData?.totalCount || 0} employees • Page ${typedData?.pageNumber || urlState.page} of ${typedData?.totalPages || Math.max(1, Math.ceil((typedData?.totalCount || 0) / urlState.pageSize))}`
          })()}
        </div>
        <div className="flex items-center space-x-3">
          <PageSizeSelect
            value={urlState.pageSize}
            onChange={(size) => {
              setUrlState({ pageSize: size, page: 1 })
            }}
          />
          <button
            onClick={() => setUrlState({ page: Math.max(1, urlState.page - 1) })}
            disabled={urlState.page <= 1}
            className={`px-3 py-1 rounded-md text-sm ${urlState.page > 1 ? 'bg-gray-200 hover:bg-gray-300 text-gray-700' : 'bg-gray-100 text-gray-400 cursor-not-allowed'}`}
          >
            Previous
          </button>
          <button
            onClick={() => setUrlState({ page: urlState.page + 1 })}
            disabled={(() => {
              const typedData = data as PagedResponse<Employee> | undefined
              return !!typedData?.totalPages && urlState.page >= (typedData.totalPages || 1)
            })()}
            className={`px-3 py-1 rounded-md text-sm ${(() => {
              const typedData = data as PagedResponse<Employee> | undefined
              return (!typedData?.totalPages || urlState.page < (typedData.totalPages || 1)) 
                ? 'bg-gray-200 hover:bg-gray-300 text-gray-700' 
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            })()}`}
          >
            Next
          </button>
        </div>
      </div>

      {/* Create Employee Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add New Employee"
        size="lg"
      >
        <EmployeeForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Employee Drawer */}
      <Drawer
        isOpen={!!editingEmployee}
        onClose={() => setEditingEmployee(null)}
        title="Edit Employee"
        size="lg"
      >
        {editingEmployee && (
          <EmployeeForm
            employee={editingEmployee}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingEmployee(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingEmployee}
        onClose={() => setDeletingEmployee(null)}
        title="Delete Employee"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to delete <span className="font-semibold">{deletingEmployee?.employeeName}</span>?
            This action cannot be undone.
          </p>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setDeletingEmployee(null)}
              className="px-4 py-2 text-gray-600 hover:text-gray-800 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleDeleteConfirm}
              disabled={deleteEmployee.isPending}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
            >
              {deleteEmployee.isPending ? 'Deleting...' : 'Delete'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Employee Side Panel */}
      <EmployeeSidePanel
        employeeId={urlState.selectedEmployee || null}
        onClose={() => setUrlState({ selectedEmployee: '' })}
        onEdit={(id) => {
          const emp = (data as PagedResponse<Employee> | undefined)?.items?.find(e => e.id === id)
          if (emp) {
            setEditingEmployee(emp)
            setUrlState({ selectedEmployee: '' })
          }
        }}
        onResign={(id) => {
          const emp = (data as PagedResponse<Employee> | undefined)?.items?.find(e => e.id === id)
          if (emp) {
            setResigningEmployee(emp)
            setUrlState({ selectedEmployee: '' })
          }
        }}
      />

      <EmployeeBulkUploadModal
        isOpen={isBulkUploadOpen}
        onClose={() => setIsBulkUploadOpen(false)}
        onSuccess={refetch}
      />

      {/* Resign Employee Modal */}
      {resigningEmployee && (
        <ResignEmployeeModal
          employee={resigningEmployee}
          isOpen={!!resigningEmployee}
          onClose={() => setResigningEmployee(null)}
          onSuccess={() => {
            setResigningEmployee(null)
            refetch()
          }}
        />
      )}
    </div>
  )
}

export default EmployeesManagement

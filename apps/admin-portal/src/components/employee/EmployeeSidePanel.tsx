import { FC, useState } from 'react'
import { SidePanel } from '@/components/ui/SidePanel'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useEmployee } from '@/hooks/api/useEmployees'
import {
  OverviewTab,
  PayrollTab,
  TaxDeclarationsTab,
  AssetsTab,
  SubscriptionsTab,
} from './tabs'
import {
  User,
  Edit,
  UserMinus,
  Loader2,
  AlertCircle,
} from 'lucide-react'

interface EmployeeSidePanelProps {
  employeeId: string | null
  onClose: () => void
  onEdit?: (employeeId: string) => void
  onResign?: (employeeId: string) => void
}

const getStatusBadgeVariant = (status: string) => {
  switch (status) {
    case 'active':
      return 'default'
    case 'inactive':
      return 'secondary'
    case 'resigned':
      return 'outline'
    case 'terminated':
      return 'destructive'
    default:
      return 'outline'
  }
}

export const EmployeeSidePanel: FC<EmployeeSidePanelProps> = ({
  employeeId,
  onClose,
  onEdit,
  onResign,
}) => {
  const [activeTab, setActiveTab] = useState('overview')
  const { data: employee, isLoading, isError } = useEmployee(employeeId || '', !!employeeId)

  const handleEdit = () => {
    if (employeeId && onEdit) {
      onEdit(employeeId)
    }
  }

  const handleResign = () => {
    if (employeeId && onResign) {
      onResign(employeeId)
    }
  }

  const renderHeader = () => {
    if (isLoading) {
      return (
        <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-gray-200 rounded-full animate-pulse" />
            <div className="flex-1 space-y-2">
              <div className="h-5 bg-gray-200 rounded w-32 animate-pulse" />
              <div className="h-4 bg-gray-200 rounded w-24 animate-pulse" />
            </div>
          </div>
        </div>
      )
    }

    if (isError || !employee) {
      return (
        <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
          <div className="flex items-center gap-3 text-red-600">
            <AlertCircle className="w-5 h-5" />
            <span>Failed to load employee</span>
          </div>
        </div>
      )
    }

    return (
      <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0 w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
            <User className="w-6 h-6 text-blue-600" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold text-gray-900 truncate">{employee.employeeName}</h2>
              <Badge variant={getStatusBadgeVariant(employee.status)}>
                {employee.status}
              </Badge>
            </div>
            <div className="text-sm text-gray-500">
              {[employee.designation, employee.department].filter(Boolean).join(' • ')}
            </div>
            {employee.employeeId && (
              <div className="text-xs text-gray-400 mt-0.5">ID: {employee.employeeId}</div>
            )}
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="text-gray-400 hover:text-gray-500">
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </Button>
        </div>

        {/* Quick Actions */}
        <div className="flex gap-2 mt-3">
          <Button variant="outline" size="sm" onClick={handleEdit}>
            <Edit className="w-3 h-3 mr-1" />
            Edit
          </Button>
          {employee.status === 'active' && (
            <Button variant="outline" size="sm" onClick={handleResign} className="text-orange-600 hover:text-orange-700">
              <UserMinus className="w-3 h-3 mr-1" />
              Resign
            </Button>
          )}
        </div>
      </div>
    )
  }

  return (
    <SidePanel
      isOpen={!!employeeId}
      onClose={onClose}
      width="xl"
      header={renderHeader()}
    >
      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
        </div>
      ) : isError || !employee ? (
        <div className="flex flex-col items-center justify-center py-12 text-gray-500">
          <AlertCircle className="w-10 h-10 mb-3" />
          <p>Unable to load employee details</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={onClose}>
            Close
          </Button>
        </div>
      ) : (
        <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
          <TabsList className="px-4 pt-2 pb-0 bg-white border-b rounded-none justify-start gap-1">
            <TabsTrigger value="overview" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Overview
            </TabsTrigger>
            <TabsTrigger value="payroll" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Payroll
            </TabsTrigger>
            <TabsTrigger value="tax" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Tax
            </TabsTrigger>
            <TabsTrigger value="assets" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Assets
            </TabsTrigger>
            <TabsTrigger value="subscriptions" className="data-[state=active]:shadow-none data-[state=active]:bg-gray-100 rounded-t-md rounded-b-none">
              Subs
            </TabsTrigger>
          </TabsList>

          <div className="flex-1 overflow-y-auto">
            <TabsContent value="overview" className="mt-0 h-full">
              <OverviewTab employee={employee} />
            </TabsContent>
            <TabsContent value="payroll" className="mt-0 h-full">
              <PayrollTab employeeId={employee.id} />
            </TabsContent>
            <TabsContent value="tax" className="mt-0 h-full">
              <TaxDeclarationsTab employeeId={employee.id} />
            </TabsContent>
            <TabsContent value="assets" className="mt-0 h-full">
              <AssetsTab employeeId={employee.id} />
            </TabsContent>
            <TabsContent value="subscriptions" className="mt-0 h-full">
              <SubscriptionsTab employeeId={employee.id} />
            </TabsContent>
          </div>
        </Tabs>
      )}
    </SidePanel>
  )
}

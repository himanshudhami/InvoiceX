import { useState, useMemo } from 'react'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useEmployees } from '@/hooks/api/useEmployees'
import {
  useOrgTree,
  useHierarchyStats,
  useUpdateManager,
} from '@/hooks/api/useHierarchy'
import { OrgTreeNode } from 'shared-types'
import { Employee } from '@/services/api/types'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { SidePanel } from '@/components/ui/SidePanel'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Users,
  UserCircle,
  ChevronDown,
  ChevronRight,
  Building2,
  User,
  Edit,
  Loader2,
  Network,
  Crown,
  X,
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface OrgNodeProps {
  node: OrgTreeNode
  level: number
  onEditManager: (node: OrgTreeNode) => void
  expandedNodes: Set<string>
  toggleExpand: (id: string) => void
}

function OrgNode({ node, level, onEditManager, expandedNodes, toggleExpand }: OrgNodeProps) {
  const isExpanded = expandedNodes.has(node.id)
  const hasChildren = node.children && node.children.length > 0

  return (
    <div className={cn('relative', level > 0 && 'ml-8 border-l border-gray-200 pl-4')}>
      <div className="group flex items-start gap-3 py-2">
        {/* Expand/Collapse button */}
        <button
          onClick={() => hasChildren && toggleExpand(node.id)}
          className={cn(
            'flex h-6 w-6 items-center justify-center rounded-md transition-colors',
            hasChildren ? 'hover:bg-gray-100 cursor-pointer' : 'cursor-default'
          )}
        >
          {hasChildren ? (
            isExpanded ? (
              <ChevronDown className="h-4 w-4 text-gray-500" />
            ) : (
              <ChevronRight className="h-4 w-4 text-gray-500" />
            )
          ) : (
            <div className="h-4 w-4" />
          )}
        </button>

        {/* Employee card */}
        <div className="flex-1 rounded-lg border bg-white p-3 shadow-sm transition-shadow hover:shadow-md">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-100">
                <UserCircle className="h-6 w-6 text-blue-600" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">{node.employeeName}</span>
                  {node.reportingLevel === 0 && (
                    <Crown className="h-4 w-4 text-yellow-500" title="Top Level" />
                  )}
                  {node.directReportsCount > 0 && (
                    <Badge variant="secondary" className="text-xs">
                      {node.directReportsCount} direct
                    </Badge>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-gray-500">
                  {node.designation && <span>{node.designation}</span>}
                  {node.designation && node.department && <span>•</span>}
                  {node.department && (
                    <span className="flex items-center gap-1">
                      <Building2 className="h-3 w-3" />
                      {node.department}
                    </span>
                  )}
                </div>
                {node.email && <p className="text-xs text-gray-400">{node.email}</p>}
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="opacity-0 group-hover:opacity-100 transition-opacity"
              onClick={() => onEditManager(node)}
            >
              <Edit className="h-4 w-4 mr-1" />
              Edit
            </Button>
          </div>
        </div>
      </div>

      {/* Children */}
      {hasChildren && isExpanded && (
        <div className="mt-1">
          {node.children.map((child) => (
            <OrgNode
              key={child.id}
              node={child}
              level={level + 1}
              onEditManager={onEditManager}
              expandedNodes={expandedNodes}
              toggleExpand={toggleExpand}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export default function OrgChartManagement() {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set())
  const [editingEmployee, setEditingEmployee] = useState<OrgTreeNode | null>(null)
  const [selectedManagerId, setSelectedManagerId] = useState<string>('')

  const { data: companies = [], isLoading: companiesLoading } = useCompanies()
  const { data: orgTree = [], isLoading: treeLoading, refetch: refetchTree } = useOrgTree(
    selectedCompanyId,
    undefined,
    !!selectedCompanyId
  )
  const { data: stats, isLoading: statsLoading } = useHierarchyStats(
    selectedCompanyId,
    !!selectedCompanyId
  )
  const { data: allEmployees = [], isLoading: employeesLoading } = useEmployees()

  // Filter employees by selected company for manager dropdown
  const potentialManagers = useMemo(() => {
    if (!selectedCompanyId) return []
    return allEmployees.filter(
      (emp) => emp.companyId === selectedCompanyId && emp.status === 'active'
    )
  }, [allEmployees, selectedCompanyId])

  const updateManagerMutation = useUpdateManager()

  // Set default company when loaded
  if (companies.length > 0 && !selectedCompanyId) {
    setSelectedCompanyId(companies[0].id)
  }

  const toggleExpand = (id: string) => {
    setExpandedNodes((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const expandAll = () => {
    const allIds = new Set<string>()
    const collectIds = (nodes: OrgTreeNode[]) => {
      nodes.forEach((node) => {
        allIds.add(node.id)
        if (node.children) {
          collectIds(node.children)
        }
      })
    }
    collectIds(orgTree)
    setExpandedNodes(allIds)
  }

  const collapseAll = () => {
    setExpandedNodes(new Set())
  }

  const handleEditManager = (node: OrgTreeNode) => {
    setEditingEmployee(node)
    setSelectedManagerId(node.managerId || 'none')
  }

  const handleClosePanel = () => {
    setEditingEmployee(null)
    setSelectedManagerId('')
  }

  const handleSaveManager = async () => {
    if (!editingEmployee) return

    await updateManagerMutation.mutateAsync({
      employeeId: editingEmployee.id,
      dto: { managerId: selectedManagerId === 'none' ? null : selectedManagerId },
    })

    handleClosePanel()
    refetchTree()
  }

  // Filter potential managers to exclude the employee being edited
  const getAvailableManagers = (employeeId: string): Employee[] => {
    // Exclude the employee themselves
    // The backend validation will catch circular references
    return potentialManagers.filter((emp) => emp.id !== employeeId)
  }

  // Get the company name for display
  const selectedCompanyName = companies.find(c => c.id === selectedCompanyId)?.name

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Organization Chart</h1>
          <p className="text-gray-500 mt-1">
            View and manage the organizational hierarchy
          </p>
        </div>
      </div>

      {/* Company Selector & Stats */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-6">
            <Label>Company</Label>
            <Select
              value={selectedCompanyId}
              onValueChange={setSelectedCompanyId}
              disabled={companiesLoading}
            >
              <SelectTrigger className="mt-1">
                <SelectValue placeholder="Select company" />
              </SelectTrigger>
              <SelectContent>
                {companies.map((company) => (
                  <SelectItem key={company.id} value={company.id}>
                    {company.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </CardContent>
        </Card>

        {statsLoading ? (
          <>
            <Skeleton className="h-24" />
            <Skeleton className="h-24" />
            <Skeleton className="h-24" />
          </>
        ) : stats ? (
          <>
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100">
                    <Users className="h-5 w-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Total Employees</p>
                    <p className="text-2xl font-bold">{stats.totalEmployees}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-green-100">
                    <User className="h-5 w-5 text-green-600" />
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Managers</p>
                    <p className="text-2xl font-bold">{stats.totalManagers}</p>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-100">
                    <Network className="h-5 w-5 text-purple-600" />
                  </div>
                  <div>
                    <p className="text-sm text-gray-500">Max Depth</p>
                    <p className="text-2xl font-bold">{stats.maxDepth}</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </>
        ) : null}
      </div>

      {/* Org Tree */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <Network className="h-5 w-5" />
              Organizational Structure
            </CardTitle>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={expandAll}>
                Expand All
              </Button>
              <Button variant="outline" size="sm" onClick={collapseAll}>
                Collapse All
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {treeLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
            </div>
          ) : orgTree.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              <Network className="h-12 w-12 mx-auto text-gray-300 mb-4" />
              <p>No organizational structure found</p>
              <p className="text-sm mt-1">
                Assign managers to employees to build the hierarchy
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              {orgTree.map((node) => (
                <OrgNode
                  key={node.id}
                  node={node}
                  level={0}
                  onEditManager={handleEditManager}
                  expandedNodes={expandedNodes}
                  toggleExpand={toggleExpand}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Edit Manager Side Panel */}
      <SidePanel
        isOpen={!!editingEmployee}
        onClose={handleClosePanel}
        width="md"
        header={
          <div className="bg-gray-50 px-4 py-4 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-100">
                  <UserCircle className="h-6 w-6 text-blue-600" />
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">
                    Edit Manager Assignment
                  </h2>
                  <p className="text-sm text-gray-500">
                    {editingEmployee?.employeeName}
                  </p>
                </div>
              </div>
              <Button variant="ghost" size="icon" onClick={handleClosePanel}>
                <X className="h-5 w-5" />
              </Button>
            </div>
          </div>
        }
      >
        <div className="p-4 space-y-6">
          {/* Employee Info */}
          <div>
            <Label className="text-sm font-medium text-gray-700">Employee</Label>
            <div className="mt-2 p-4 bg-gray-50 rounded-lg">
              <div className="flex items-center gap-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-blue-100">
                  <UserCircle className="h-7 w-7 text-blue-600" />
                </div>
                <div>
                  <div className="font-medium text-gray-900">{editingEmployee?.employeeName}</div>
                  <div className="text-sm text-gray-500">
                    {editingEmployee?.designation}
                    {editingEmployee?.department && ` • ${editingEmployee.department}`}
                  </div>
                  {editingEmployee?.email && (
                    <div className="text-xs text-gray-400">{editingEmployee.email}</div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Company Info */}
          <div>
            <Label className="text-sm font-medium text-gray-700">Company</Label>
            <div className="mt-2 p-3 bg-gray-50 rounded-lg flex items-center gap-2">
              <Building2 className="h-4 w-4 text-gray-400" />
              <span className="text-sm text-gray-900">{selectedCompanyName}</span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              Only employees from this company can be selected as manager
            </p>
          </div>

          {/* Manager Selection */}
          <div>
            <Label className="text-sm font-medium text-gray-700">Reporting Manager</Label>
            <Select
              value={selectedManagerId}
              onValueChange={setSelectedManagerId}
              disabled={employeesLoading}
            >
              <SelectTrigger className="mt-2">
                <SelectValue placeholder="Select manager" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">
                  <div className="flex items-center gap-2">
                    <Crown className="h-4 w-4 text-yellow-500" />
                    <span>No Manager (Top Level)</span>
                  </div>
                </SelectItem>
                {editingEmployee &&
                  getAvailableManagers(editingEmployee.id).map((manager) => (
                    <SelectItem key={manager.id} value={manager.id}>
                      <div className="flex flex-col">
                        <span className="font-medium">{manager.employeeName}</span>
                        <span className="text-xs text-gray-500">
                          {manager.designation}
                          {manager.department && ` • ${manager.department}`}
                        </span>
                      </div>
                    </SelectItem>
                  ))}
              </SelectContent>
            </Select>
            {employeesLoading && (
              <p className="text-xs text-gray-500 mt-1">Loading employees...</p>
            )}
            {!employeesLoading && editingEmployee && getAvailableManagers(editingEmployee.id).length === 0 && (
              <p className="text-xs text-amber-600 mt-1">
                No other employees found in this company
              </p>
            )}
          </div>

          {/* Action Buttons */}
          <div className="flex gap-3 pt-4 border-t">
            <Button
              variant="outline"
              className="flex-1"
              onClick={handleClosePanel}
            >
              Cancel
            </Button>
            <Button
              className="flex-1"
              onClick={handleSaveManager}
              disabled={updateManagerMutation.isPending}
            >
              {updateManagerMutation.isPending && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              Save Changes
            </Button>
          </div>
        </div>
      </SidePanel>
    </div>
  )
}

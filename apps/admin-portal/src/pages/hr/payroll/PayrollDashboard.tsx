import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryState, parseAsString } from 'nuqs'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatINR } from '@/lib/currency'
import {
  usePayrollRuns,
  usePayrollRunSummary,
} from '@/features/payroll/hooks'
import { useCompanies } from '@/hooks/api/useCompanies'
import {
  DollarSign,
  Users,
  TrendingUp,
  FileText,
  Plus,
  Eye,
  Calendar,
  CheckCircle,
  Clock,
  XCircle,
} from 'lucide-react'
import { format } from 'date-fns'

const PayrollDashboard = () => {
  const navigate = useNavigate()
  const [selectedCompanyId, setSelectedCompanyId] = useQueryState('company', parseAsString.withDefault(''))
  const { data: companies = [] } = useCompanies()

  const { data: payrollRunsData, isLoading } = usePayrollRuns({
    companyId: selectedCompanyId || undefined,
    pageSize: 10,
  })

  const handleCompanyChange = (companyId: string) => {
    setSelectedCompanyId(companyId || null)
  }

  const payrollRuns = payrollRunsData?.items || []

  // Calculate KPIs from recent payroll runs
  const kpis = useMemo(() => {
    const recentRuns = payrollRuns.slice(0, 12) // Last 12 months
    const paidRuns = recentRuns.filter(run => run.status === 'paid')
    
    const totalPayrollCost = paidRuns.reduce((sum, run) => sum + run.totalNetSalary, 0)
    const totalEmployerCost = paidRuns.reduce((sum, run) => sum + run.totalEmployerCost, 0)
    const totalEmployees = paidRuns.reduce((sum, run) => sum + run.totalEmployees, 0)
    const totalTds = paidRuns.reduce((sum, run) => {
      // TDS would need to be calculated from transactions
      return sum
    }, 0)
    const totalPf = paidRuns.reduce((sum, run) => sum + run.totalEmployerPf, 0)
    const totalEsi = paidRuns.reduce((sum, run) => sum + run.totalEmployerEsi, 0)

    const avgSalary = totalEmployees > 0 ? totalPayrollCost / totalEmployees / paidRuns.length : 0

    return {
      totalPayrollCost,
      totalEmployerCost,
      avgSalary,
      totalEmployees: totalEmployees / Math.max(paidRuns.length, 1),
      totalTds,
      totalPf,
      totalEsi,
    }
  }, [payrollRuns])

  const getStatusBadge = (status: string) => {
    const statusConfig = {
      draft: { label: 'Draft', color: 'bg-gray-100 text-gray-800', icon: FileText },
      processing: { label: 'Processing', color: 'bg-blue-100 text-blue-800', icon: Clock },
      computed: { label: 'Computed', color: 'bg-yellow-100 text-yellow-800', icon: CheckCircle },
      approved: { label: 'Approved', color: 'bg-green-100 text-green-800', icon: CheckCircle },
      paid: { label: 'Paid', color: 'bg-green-100 text-green-800', icon: CheckCircle },
      cancelled: { label: 'Cancelled', color: 'bg-red-100 text-red-800', icon: XCircle },
    }

    const config = statusConfig[status as keyof typeof statusConfig] || statusConfig.draft
    const Icon = config.icon

    return (
      <Badge className={config.color}>
        <Icon className="w-3 h-3 mr-1" />
        {config.label}
      </Badge>
    )
  }

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMM yyyy')
  }

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Payroll Dashboard</h1>
          <p className="text-gray-600 mt-2">Overview of payroll operations and statistics</p>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader>
                <div className="h-4 bg-gray-200 rounded animate-pulse"></div>
              </CardHeader>
              <CardContent>
                <div className="h-8 bg-gray-200 rounded animate-pulse"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Payroll Dashboard</h1>
          <p className="text-gray-600 mt-2">Overview of payroll operations and statistics</p>
        </div>
        <div className="flex gap-3">
          <CompanyFilterDropdown
            value={selectedCompanyId}
            onChange={handleCompanyChange}
          />
          <Button onClick={() => navigate('/payroll/process')}>
            <Plus className="w-4 h-4 mr-2" />
            Process Payroll
          </Button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Payroll Cost</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(kpis.totalPayrollCost)}</div>
            <p className="text-xs text-muted-foreground mt-1">Last 12 months (paid)</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Salary</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(kpis.avgSalary)}</div>
            <p className="text-xs text-muted-foreground mt-1">Per employee per month</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total PF</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(kpis.totalPf)}</div>
            <p className="text-xs text-muted-foreground mt-1">Employer contribution</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total TDS</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatINR(kpis.totalTds)}</div>
            <p className="text-xs text-muted-foreground mt-1">Collected YTD</p>
          </CardContent>
        </Card>
      </div>

      {/* Recent Payroll Runs */}
      <Card>
        <CardHeader>
          <div className="flex justify-between items-center">
            <div>
              <CardTitle>Recent Payroll Runs</CardTitle>
              <CardDescription>Latest payroll processing activities</CardDescription>
            </div>
            <Button variant="outline" onClick={() => navigate('/payroll/runs')}>
              View All
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {payrollRuns.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              No payroll runs found. Start by processing your first payroll.
            </div>
          ) : (
            <div className="space-y-4">
              {payrollRuns.map((run) => (
                <div
                  key={run.id}
                  className="flex items-center justify-between p-4 border rounded-lg hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/payroll/runs/${run.id}`)}
                >
                  <div className="flex items-center gap-4">
                    <div className="flex flex-col">
                      <div className="font-semibold text-gray-900">
                        {getMonthYear(run.payrollMonth, run.payrollYear)}
                      </div>
                      <div className="text-sm text-gray-500">
                        {run.companyName || 'Company'}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Users className="w-4 h-4 text-gray-400" />
                      <span className="text-sm text-gray-600">{run.totalEmployees} employees</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <DollarSign className="w-4 h-4 text-gray-400" />
                      <span className="text-sm text-gray-600">{formatINR(run.totalNetSalary)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {getStatusBadge(run.status)}
                    <Button variant="ghost" size="sm">
                      <Eye className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card className="cursor-pointer hover:bg-gray-50" onClick={() => navigate('/payroll/runs')}>
          <CardHeader>
            <CardTitle className="text-lg">Payroll Runs</CardTitle>
            <CardDescription>View and manage all payroll processing runs</CardDescription>
          </CardHeader>
        </Card>

        <Card className="cursor-pointer hover:bg-gray-50" onClick={() => navigate('/payroll/salary-structures')}>
          <CardHeader>
            <CardTitle className="text-lg">Salary Structures</CardTitle>
            <CardDescription>Manage employee CTC and salary breakdowns</CardDescription>
          </CardHeader>
        </Card>

        <Card className="cursor-pointer hover:bg-gray-50" onClick={() => navigate('/payroll/tax-declarations')}>
          <CardHeader>
            <CardTitle className="text-lg">Tax Declarations</CardTitle>
            <CardDescription>View and manage employee tax declarations</CardDescription>
          </CardHeader>
        </Card>

        <Card className="cursor-pointer hover:bg-gray-50" onClick={() => navigate('/payroll/contractors')}>
          <CardHeader>
            <CardTitle className="text-lg">Contractor Payments</CardTitle>
            <CardDescription>Manage contractor and freelancer payments</CardDescription>
          </CardHeader>
        </Card>

        <Card className="cursor-pointer hover:bg-gray-50" onClick={() => navigate('/payroll/settings')}>
          <CardHeader>
            <CardTitle className="text-lg">Statutory Settings</CardTitle>
            <CardDescription>Configure PF, ESI, PT, and TDS settings</CardDescription>
          </CardHeader>
        </Card>
      </div>
    </div>
  )
}

export default PayrollDashboard





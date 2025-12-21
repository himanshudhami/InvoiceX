import { useMemo } from 'react'
import { useQueryStates, parseAsString } from 'nuqs'
import { useExportDashboard, useFemaComplianceDashboard, useRealizationTrend } from '@/hooks/api/useExportReports'
import { useActiveLut, useLutExpiryAlerts } from '@/hooks/api/useLuts'
import { useRealizationAlerts } from '@/hooks/api/useFircs'
import { useCompanies } from '@/hooks/api/useCompanies'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Link } from 'react-router-dom'
import {
  DollarSign,
  TrendingUp,
  TrendingDown,
  AlertTriangle,
  CheckCircle,
  Clock,
  FileText,
  Shield,
  ArrowRight,
  Users,
  Globe,
  Receipt,
} from 'lucide-react'
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
} from 'recharts'

const ExportDashboard = () => {
  // URL-backed filter state
  const [urlState, setUrlState] = useQueryStates(
    {
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const selectedCompanyId = urlState.company
  const { data: companies = [] } = useCompanies()

  // Get selected company or default to first one
  const companyId = selectedCompanyId || companies[0]?.id || ''

  // Fetch dashboard data
  const { data: dashboard, isLoading: isLoadingDashboard } = useExportDashboard(companyId, !!companyId)
  const { data: femaCompliance, isLoading: isLoadingFema } = useFemaComplianceDashboard(companyId, !!companyId)
  const { data: realizationTrend = [], isLoading: isLoadingTrend } = useRealizationTrend(companyId, 12, !!companyId)
  const { data: activeLut } = useActiveLut(companyId, !!companyId)
  const { data: realizationAlerts = [] } = useRealizationAlerts(companyId, !!companyId)
  const { data: lutAlerts = [] } = useLutExpiryAlerts(companyId, !!companyId)

  const isLoading = isLoadingDashboard || isLoadingFema

  // Format helpers
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    if (currency === 'INR') {
      return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount)
    }
    return new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount)
  }

  const formatPercentage = (value: number) => `${value.toFixed(1)}%`

  // Compliance score color
  const getComplianceColor = (score: number) => {
    if (score >= 90) return 'text-green-600'
    if (score >= 70) return 'text-yellow-600'
    if (score >= 50) return 'text-orange-600'
    return 'text-red-600'
  }

  const getComplianceBadge = (status: string) => {
    const variants: Record<string, { variant: 'default' | 'secondary' | 'destructive' | 'outline'; text: string }> = {
      compliant: { variant: 'default', text: 'Compliant' },
      warning: { variant: 'secondary', text: 'Warning' },
      critical: { variant: 'destructive', text: 'Critical' },
      non_compliant: { variant: 'destructive', text: 'Non-Compliant' },
    }
    const config = variants[status] || { variant: 'outline' as const, text: status }
    return <Badge variant={config.variant}>{config.text}</Badge>
  }

  // Currency breakdown for pie chart
  const currencyData = useMemo(() => {
    if (!dashboard?.receivablesByCurrency) return []
    return Object.entries(dashboard.receivablesByCurrency).map(([currency, amount]) => ({
      name: currency,
      value: amount,
    }))
  }, [dashboard?.receivablesByCurrency])

  const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8']

  if (!companyId && companies.length === 0) {
    return (
      <div className="p-6">
        <div className="text-center text-muted-foreground">
          No companies found. Please create a company first.
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold">Export Command Center</h1>
          <p className="text-muted-foreground">
            Comprehensive view of export receivables, forex, and compliance
          </p>
        </div>
        <CompanyFilterDropdown
          value={selectedCompanyId}
          onChange={(value) => setUrlState({ company: value })}
          companies={companies}
        />
      </div>

      {/* Alert Banner */}
      {femaCompliance && (femaCompliance.criticalAlerts > 0 || femaCompliance.warningAlerts > 0) && (
        <Card className="border-destructive bg-destructive/5">
          <CardContent className="pt-4">
            <div className="flex items-center gap-4">
              <AlertTriangle className="h-8 w-8 text-destructive" />
              <div className="flex-1">
                <p className="font-semibold text-destructive">
                  {femaCompliance.criticalAlerts} Critical Alerts, {femaCompliance.warningAlerts} Warnings
                </p>
                <p className="text-sm text-muted-foreground">
                  {femaCompliance.overdueCount > 0 && `${femaCompliance.overdueCount} invoices past FEMA deadline. `}
                  {!femaCompliance.hasActiveLut && 'No active LUT found. '}
                </p>
              </div>
              <Button variant="destructive" size="sm" asChild>
                <Link to="/exports/fema-compliance">View Alerts</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* KPI Cards Row 1 */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Total Export Receivables */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Total Receivables</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className="text-2xl font-bold">
                  {formatCurrency(dashboard?.totalExportReceivables || 0)}
                </div>
                <p className="text-xs text-muted-foreground">
                  {dashboard?.totalCustomers || 0} customers
                </p>
              </>
            )}
          </CardContent>
        </Card>

        {/* FY Revenue */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">FY Export Revenue</CardTitle>
            <Globe className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className="text-2xl font-bold">
                  {formatCurrency(dashboard?.totalExportRevenueFy || 0)}
                </div>
                <p className="text-xs text-muted-foreground">
                  {dashboard?.totalInvoicesFy || 0} invoices this FY
                </p>
              </>
            )}
          </CardContent>
        </Card>

        {/* Forex Gain/Loss */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Net Forex P&L</CardTitle>
            {(dashboard?.netForexGainLossFy || 0) >= 0 ? (
              <TrendingUp className="h-4 w-4 text-green-600" />
            ) : (
              <TrendingDown className="h-4 w-4 text-red-600" />
            )}
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className={`text-2xl font-bold ${(dashboard?.netForexGainLossFy || 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                  {formatCurrency(Math.abs(dashboard?.netForexGainLossFy || 0), 'INR')}
                  {(dashboard?.netForexGainLossFy || 0) >= 0 ? ' Gain' : ' Loss'}
                </div>
                <p className="text-xs text-muted-foreground">
                  Unrealized: {formatCurrency(dashboard?.unrealizedForexPosition || 0, 'INR')}
                </p>
              </>
            )}
          </CardContent>
        </Card>

        {/* FEMA Compliance Score */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">FEMA Compliance</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className={`text-2xl font-bold ${getComplianceColor(femaCompliance?.complianceScore || 0)}`}>
                  {femaCompliance?.complianceScore || 0}%
                </div>
                <div className="flex items-center gap-2">
                  {getComplianceBadge(femaCompliance?.overallStatus || 'unknown')}
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {/* KPI Cards Row 2 */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Realization Rate */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Realization Rate</CardTitle>
            <Receipt className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className="text-2xl font-bold">
                  {((dashboard?.totalRealizedFy || 0) / (dashboard?.totalExportRevenueFy || 1) * 100).toFixed(1)}%
                </div>
                <p className="text-xs text-muted-foreground">
                  Avg {dashboard?.avgRealizationDays?.toFixed(0) || 0} days to realize
                </p>
              </>
            )}
          </CardContent>
        </Card>

        {/* Overdue Invoices */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">FEMA Overdue</CardTitle>
            <Clock className="h-4 w-4 text-destructive" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className={`text-2xl font-bold ${(dashboard?.overdueInvoices || 0) > 0 ? 'text-red-600' : ''}`}>
                  {dashboard?.overdueInvoices || 0}
                </div>
                <p className="text-xs text-muted-foreground">
                  invoices past 270-day deadline
                </p>
              </>
            )}
          </CardContent>
        </Card>

        {/* LUT Status */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">LUT Status</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : activeLut ? (
              <>
                <div className="text-lg font-semibold flex items-center gap-2">
                  <CheckCircle className="h-4 w-4 text-green-600" />
                  Active
                </div>
                <p className="text-xs text-muted-foreground">
                  {activeLut.lutNumber} (expires {new Date(activeLut.validTo).toLocaleDateString()})
                </p>
              </>
            ) : (
              <>
                <div className="text-lg font-semibold flex items-center gap-2 text-red-600">
                  <AlertTriangle className="h-4 w-4" />
                  No Active LUT
                </div>
                <Button variant="link" size="sm" className="p-0 h-auto" asChild>
                  <Link to="/exports/lut-register">Register LUT</Link>
                </Button>
              </>
            )}
          </CardContent>
        </Card>

        {/* Pending FIRCs */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Pending FIRCs</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-8 w-32" />
            ) : (
              <>
                <div className={`text-2xl font-bold ${(dashboard?.pendingFircs || 0) > 0 ? 'text-yellow-600' : ''}`}>
                  {dashboard?.pendingFircs || 0}
                </div>
                <p className="text-xs text-muted-foreground">
                  awaiting reconciliation
                </p>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Charts Row */}
      <div className="grid gap-4 lg:grid-cols-2">
        {/* Realization Trend Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Realization Trend (12 Months)</CardTitle>
            <CardDescription>Monthly invoiced vs realized amounts</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoadingTrend ? (
              <Skeleton className="h-[300px] w-full" />
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={realizationTrend}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="monthName" tick={{ fontSize: 12 }} />
                  <YAxis tickFormatter={(value) => `$${(value / 1000).toFixed(0)}K`} />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    labelFormatter={(label) => `Month: ${label}`}
                  />
                  <Legend />
                  <Area
                    type="monotone"
                    dataKey="invoiced"
                    name="Invoiced"
                    stroke="#8884d8"
                    fill="#8884d8"
                    fillOpacity={0.3}
                  />
                  <Area
                    type="monotone"
                    dataKey="realized"
                    name="Realized"
                    stroke="#82ca9d"
                    fill="#82ca9d"
                    fillOpacity={0.3}
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Currency Breakdown Pie Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Receivables by Currency</CardTitle>
            <CardDescription>Outstanding amounts by currency</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-[300px] w-full" />
            ) : currencyData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={currencyData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, percent }) => `${name} (${(percent * 100).toFixed(0)}%)`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {currencyData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => formatCurrency(value)} />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[300px] flex items-center justify-center text-muted-foreground">
                No receivables data
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Quick Links */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card className="hover:bg-accent/50 transition-colors cursor-pointer" asChild>
          <Link to="/exports/receivables-ageing">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm flex items-center justify-between">
                Receivables Ageing
                <ArrowRight className="h-4 w-4" />
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">
                View detailed ageing analysis with FEMA deadlines
              </p>
            </CardContent>
          </Link>
        </Card>

        <Card className="hover:bg-accent/50 transition-colors cursor-pointer" asChild>
          <Link to="/exports/firc-management">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm flex items-center justify-between">
                FIRC Management
                <ArrowRight className="h-4 w-4" />
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">
                Track and reconcile foreign inward remittances
              </p>
            </CardContent>
          </Link>
        </Card>

        <Card className="hover:bg-accent/50 transition-colors cursor-pointer" asChild>
          <Link to="/exports/lut-register">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm flex items-center justify-between">
                LUT Register
                <ArrowRight className="h-4 w-4" />
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">
                Manage Letters of Undertaking for GST exports
              </p>
            </CardContent>
          </Link>
        </Card>

        <Card className="hover:bg-accent/50 transition-colors cursor-pointer" asChild>
          <Link to="/exports/fema-compliance">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm flex items-center justify-between">
                FEMA Compliance
                <ArrowRight className="h-4 w-4" />
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">
                View compliance status and violation alerts
              </p>
            </CardContent>
          </Link>
        </Card>
      </div>

      {/* Recent Alerts */}
      {femaCompliance?.topAlerts && femaCompliance.topAlerts.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Recent Alerts</CardTitle>
            <CardDescription>Critical issues requiring attention</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {femaCompliance.topAlerts.slice(0, 5).map((alert, index) => (
                <div
                  key={index}
                  className={`flex items-start gap-3 p-3 rounded-lg ${
                    alert.severity === 'critical' ? 'bg-red-50 dark:bg-red-950' :
                    alert.severity === 'warning' ? 'bg-yellow-50 dark:bg-yellow-950' :
                    'bg-blue-50 dark:bg-blue-950'
                  }`}
                >
                  <AlertTriangle className={`h-5 w-5 mt-0.5 ${
                    alert.severity === 'critical' ? 'text-red-600' :
                    alert.severity === 'warning' ? 'text-yellow-600' :
                    'text-blue-600'
                  }`} />
                  <div className="flex-1">
                    <p className="font-medium text-sm">{alert.title}</p>
                    <p className="text-xs text-muted-foreground">{alert.description}</p>
                    {alert.amount && (
                      <p className="text-xs font-medium mt-1">
                        {formatCurrency(alert.amount, alert.currency || 'USD')}
                      </p>
                    )}
                  </div>
                  <Badge variant={alert.severity === 'critical' ? 'destructive' : 'secondary'}>
                    {alert.severity}
                  </Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

export default ExportDashboard

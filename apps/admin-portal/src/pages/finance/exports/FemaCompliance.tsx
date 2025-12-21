import { useMemo } from 'react'
import { useQueryStates, parseAsString } from 'nuqs'
import {
  useFemaComplianceDashboard,
  useFemaViolationAlerts,
  useExportRealizationReport,
} from '@/hooks/api/useExportReports'
import { useRealizationAlerts } from '@/hooks/api/useFircs'
import { useActiveLut, useLutExpiryAlerts } from '@/hooks/api/useLuts'
import { useCompanies } from '@/hooks/api/useCompanies'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Progress } from '@/components/ui/progress'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Link } from 'react-router-dom'
import {
  Shield,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Clock,
  FileText,
  DollarSign,
  TrendingUp,
  ArrowRight,
  AlertCircle,
} from 'lucide-react'
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
} from 'recharts'

const FemaCompliance = () => {
  // URL-backed filter state
  const [urlState, setUrlState] = useQueryStates(
    {
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const { data: companies = [] } = useCompanies()
  const selectedCompanyId = urlState.company || companies[0]?.id || ''

  // Fetch data
  const { data: femaCompliance, isLoading } = useFemaComplianceDashboard(selectedCompanyId, !!selectedCompanyId)
  const { data: violationAlerts = [] } = useFemaViolationAlerts(selectedCompanyId, !!selectedCompanyId)
  const { data: realizationReport } = useExportRealizationReport(selectedCompanyId, undefined, !!selectedCompanyId)
  const { data: realizationAlerts = [] } = useRealizationAlerts(selectedCompanyId, !!selectedCompanyId)
  const { data: activeLut } = useActiveLut(selectedCompanyId, !!selectedCompanyId)
  const { data: lutAlerts = [] } = useLutExpiryAlerts(selectedCompanyId, !!selectedCompanyId)

  // Format helpers
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    if (currency === 'INR') {
      return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount)
    }
    return new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: 0 }).format(amount)
  }

  // Compliance score visualization
  const getScoreColor = (score: number) => {
    if (score >= 90) return 'text-green-600'
    if (score >= 70) return 'text-yellow-600'
    if (score >= 50) return 'text-orange-600'
    return 'text-red-600'
  }

  const getScoreGradient = (score: number) => {
    if (score >= 90) return 'from-green-500 to-green-600'
    if (score >= 70) return 'from-yellow-500 to-yellow-600'
    if (score >= 50) return 'from-orange-500 to-orange-600'
    return 'from-red-500 to-red-600'
  }

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: 'default' | 'secondary' | 'destructive' | 'outline'; text: string }> = {
      compliant: { variant: 'default', text: 'Compliant' },
      warning: { variant: 'secondary', text: 'Warning' },
      critical: { variant: 'destructive', text: 'Critical' },
      non_compliant: { variant: 'destructive', text: 'Non-Compliant' },
    }
    const config = variants[status] || { variant: 'outline' as const, text: status }
    return <Badge variant={config.variant}>{config.text}</Badge>
  }

  // Realization status data for pie chart
  const realizationStatusData = useMemo(() => {
    if (!femaCompliance) return []
    return [
      { name: 'Fully Realized', value: femaCompliance.fullyRealizedCount, color: '#22c55e' },
      { name: 'Partially Realized', value: femaCompliance.partiallyRealizedCount, color: '#eab308' },
      { name: 'Pending', value: femaCompliance.pendingRealizationCount, color: '#3b82f6' },
      { name: 'Overdue', value: femaCompliance.overdueCount, color: '#ef4444' },
    ].filter(item => item.value > 0)
  }, [femaCompliance])

  // Checklist items
  const checklistItems = useMemo(() => {
    if (!femaCompliance) return []
    return [
      {
        label: 'Active LUT',
        status: femaCompliance.hasActiveLut ? 'pass' : 'fail',
        description: femaCompliance.hasActiveLut
          ? `LUT ${femaCompliance.activeLutNumber} valid for ${femaCompliance.daysToLutExpiry} days`
          : 'No active LUT found',
        action: femaCompliance.hasActiveLut ? undefined : { label: 'Register LUT', link: '/exports/lut-register' },
      },
      {
        label: 'FEMA Realization',
        status: femaCompliance.overdueCount === 0 ? 'pass' : 'fail',
        description: femaCompliance.overdueCount === 0
          ? 'All invoices within 270-day deadline'
          : `${femaCompliance.overdueCount} invoices past FEMA deadline`,
        action: femaCompliance.overdueCount > 0 ? { label: 'View Overdue', link: '/exports/receivables-ageing' } : undefined,
      },
      {
        label: 'FIRC Coverage',
        status: femaCompliance.fircsCoverage >= 90 ? 'pass' : femaCompliance.fircsCoverage >= 70 ? 'warning' : 'fail',
        description: `${femaCompliance.fircsCoverage.toFixed(1)}% of payments have FIRCs`,
        action: femaCompliance.fircsPending > 0 ? { label: 'Manage FIRCs', link: '/exports/firc-management' } : undefined,
      },
      {
        label: 'EDPMS Reporting',
        status: femaCompliance.edpmsPending === 0 ? 'pass' : femaCompliance.edpmsPending <= 3 ? 'warning' : 'fail',
        description: femaCompliance.edpmsPending === 0
          ? 'All FIRCs reported to EDPMS'
          : `${femaCompliance.edpmsPending} FIRCs pending EDPMS reporting`,
        action: femaCompliance.edpmsPending > 0 ? { label: 'Report Now', link: '/exports/firc-management' } : undefined,
      },
    ]
  }, [femaCompliance])

  if (!selectedCompanyId && companies.length === 0) {
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
          <h1 className="text-2xl font-bold">FEMA Compliance Dashboard</h1>
          <p className="text-muted-foreground">
            Monitor export realization and regulatory compliance
          </p>
        </div>
        <CompanyFilterDropdown
          value={urlState.company}
          onChange={(value) => setUrlState({ company: value })}
          companies={companies}
        />
      </div>

      {/* Compliance Score Card */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-col md:flex-row items-center gap-8">
            {/* Score Circle */}
            <div className="relative">
              {isLoading ? (
                <Skeleton className="h-40 w-40 rounded-full" />
              ) : (
                <div className="relative h-40 w-40">
                  <svg className="h-40 w-40 transform -rotate-90" viewBox="0 0 100 100">
                    <circle
                      cx="50"
                      cy="50"
                      r="45"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="8"
                      className="text-muted"
                    />
                    <circle
                      cx="50"
                      cy="50"
                      r="45"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="8"
                      strokeDasharray={`${(femaCompliance?.complianceScore || 0) * 2.83} 283`}
                      className={getScoreColor(femaCompliance?.complianceScore || 0)}
                    />
                  </svg>
                  <div className="absolute inset-0 flex flex-col items-center justify-center">
                    <span className={`text-4xl font-bold ${getScoreColor(femaCompliance?.complianceScore || 0)}`}>
                      {femaCompliance?.complianceScore || 0}
                    </span>
                    <span className="text-sm text-muted-foreground">Score</span>
                  </div>
                </div>
              )}
            </div>

            {/* Status & Summary */}
            <div className="flex-1 space-y-4">
              <div className="flex items-center gap-2">
                <Shield className={`h-6 w-6 ${getScoreColor(femaCompliance?.complianceScore || 0)}`} />
                <h2 className="text-xl font-semibold">
                  Overall Status: {getStatusBadge(femaCompliance?.overallStatus || 'unknown')}
                </h2>
              </div>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="text-center p-4 bg-muted/50 rounded-lg">
                  <p className="text-2xl font-bold">{femaCompliance?.totalOpenInvoices || 0}</p>
                  <p className="text-sm text-muted-foreground">Open Invoices</p>
                </div>
                <div className="text-center p-4 bg-muted/50 rounded-lg">
                  <p className="text-2xl font-bold">{formatCurrency(femaCompliance?.totalExportReceivables || 0)}</p>
                  <p className="text-sm text-muted-foreground">Receivables</p>
                </div>
                <div className="text-center p-4 bg-red-50 dark:bg-red-950 rounded-lg">
                  <p className="text-2xl font-bold text-red-600">{femaCompliance?.criticalAlerts || 0}</p>
                  <p className="text-sm text-muted-foreground">Critical Alerts</p>
                </div>
                <div className="text-center p-4 bg-yellow-50 dark:bg-yellow-950 rounded-lg">
                  <p className="text-2xl font-bold text-yellow-600">{femaCompliance?.warningAlerts || 0}</p>
                  <p className="text-sm text-muted-foreground">Warnings</p>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Compliance Checklist & Alerts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Compliance Checklist */}
        <Card>
          <CardHeader>
            <CardTitle>Compliance Checklist</CardTitle>
            <CardDescription>Key compliance requirements status</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {isLoading ? (
                [1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-16 w-full" />)
              ) : (
                checklistItems.map((item, index) => (
                  <div
                    key={index}
                    className={`flex items-start gap-3 p-4 rounded-lg ${
                      item.status === 'pass' ? 'bg-green-50 dark:bg-green-950' :
                      item.status === 'warning' ? 'bg-yellow-50 dark:bg-yellow-950' :
                      'bg-red-50 dark:bg-red-950'
                    }`}
                  >
                    {item.status === 'pass' ? (
                      <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
                    ) : item.status === 'warning' ? (
                      <AlertCircle className="h-5 w-5 text-yellow-600 mt-0.5" />
                    ) : (
                      <XCircle className="h-5 w-5 text-red-600 mt-0.5" />
                    )}
                    <div className="flex-1">
                      <p className="font-medium">{item.label}</p>
                      <p className="text-sm text-muted-foreground">{item.description}</p>
                    </div>
                    {item.action && (
                      <Button variant="outline" size="sm" asChild>
                        <Link to={item.action.link}>
                          {item.action.label}
                          <ArrowRight className="h-4 w-4 ml-1" />
                        </Link>
                      </Button>
                    )}
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {/* Realization Status Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Realization Status</CardTitle>
            <CardDescription>Invoice realization breakdown</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-[250px] w-full" />
            ) : realizationStatusData.length > 0 ? (
              <ResponsiveContainer width="100%" height={250}>
                <PieChart>
                  <Pie
                    data={realizationStatusData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={90}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {realizationStatusData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => `${value} invoices`} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[250px] flex items-center justify-center text-muted-foreground">
                No realization data available
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Violation Alerts */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-destructive" />
            Violation Alerts
          </CardTitle>
          <CardDescription>Issues requiring immediate attention</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full" />)}
            </div>
          ) : violationAlerts.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <CheckCircle className="h-12 w-12 mx-auto mb-2 text-green-600" />
              <p>No violation alerts. Great job!</p>
            </div>
          ) : (
            <div className="space-y-3">
              {violationAlerts.map((alert, index) => (
                <div
                  key={index}
                  className={`flex items-start gap-3 p-4 rounded-lg border ${
                    alert.severity === 'critical' ? 'border-red-200 bg-red-50 dark:bg-red-950' :
                    alert.severity === 'warning' ? 'border-yellow-200 bg-yellow-50 dark:bg-yellow-950' :
                    'border-blue-200 bg-blue-50 dark:bg-blue-950'
                  }`}
                >
                  <AlertTriangle className={`h-5 w-5 mt-0.5 ${
                    alert.severity === 'critical' ? 'text-red-600' :
                    alert.severity === 'warning' ? 'text-yellow-600' :
                    'text-blue-600'
                  }`} />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <p className="font-medium">{alert.title}</p>
                      <Badge variant={alert.severity === 'critical' ? 'destructive' : 'secondary'}>
                        {alert.severity}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">{alert.description}</p>
                    {alert.amount && (
                      <p className="text-sm font-medium mt-1">
                        Amount: {formatCurrency(alert.amount, alert.currency || 'USD')}
                      </p>
                    )}
                    {alert.daysOverdue && (
                      <p className="text-sm text-red-600 mt-1">
                        {alert.daysOverdue} days overdue
                      </p>
                    )}
                  </div>
                  {alert.documentNumber && (
                    <Button variant="outline" size="sm" asChild>
                      <Link to={`/invoices/${alert.relatedEntityId}`}>
                        View {alert.documentNumber}
                      </Link>
                    </Button>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* At Risk Invoices */}
      {realizationReport?.atRiskInvoices && realizationReport.atRiskInvoices.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-yellow-600" />
              At-Risk Invoices
            </CardTitle>
            <CardDescription>Invoices approaching or past FEMA deadline</CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Invoice</TableHead>
                  <TableHead>Customer</TableHead>
                  <TableHead>Invoice Date</TableHead>
                  <TableHead>FEMA Deadline</TableHead>
                  <TableHead>Days</TableHead>
                  <TableHead>Outstanding</TableHead>
                  <TableHead>Risk Level</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {realizationReport.atRiskInvoices.slice(0, 10).map((invoice) => (
                  <TableRow key={invoice.invoiceId}>
                    <TableCell>
                      <Link
                        to={`/invoices/${invoice.invoiceId}`}
                        className="font-medium text-primary hover:underline"
                      >
                        {invoice.invoiceNumber}
                      </Link>
                    </TableCell>
                    <TableCell>{invoice.customerName}</TableCell>
                    <TableCell>{new Date(invoice.invoiceDate).toLocaleDateString()}</TableCell>
                    <TableCell>{new Date(invoice.femaDeadline).toLocaleDateString()}</TableCell>
                    <TableCell className={invoice.daysToDeadline < 0 ? 'text-red-600 font-medium' : ''}>
                      {invoice.daysToDeadline < 0 ? `${Math.abs(invoice.daysToDeadline)} overdue` : invoice.daysToDeadline}
                    </TableCell>
                    <TableCell>{formatCurrency(invoice.outstandingAmount, invoice.currency)}</TableCell>
                    <TableCell>
                      <Badge variant={
                        invoice.riskLevel === 'critical' ? 'destructive' :
                        invoice.riskLevel === 'high' ? 'destructive' :
                        invoice.riskLevel === 'medium' ? 'secondary' :
                        'outline'
                      }>
                        {invoice.riskLevel}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            {realizationReport.atRiskInvoices.length > 10 && (
              <div className="mt-4 text-center">
                <Button variant="outline" asChild>
                  <Link to="/exports/receivables-ageing">
                    View All {realizationReport.atRiskInvoices.length} At-Risk Invoices
                  </Link>
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}

export default FemaCompliance

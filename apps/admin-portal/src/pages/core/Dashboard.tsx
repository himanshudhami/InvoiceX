import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useDashboard } from '@/hooks/api/useDashboard'
import { useCompanies } from '@/hooks/api/useCompanies'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { formatCurrency } from '@/lib/currency'
import { useNavigate } from 'react-router-dom'

const Dashboard = () => {
  const [companyFilter, setCompanyFilter] = useState<string>('')
  const { data: dashboardData, isLoading, error } = useDashboard()
  const { data: companies = [] } = useCompanies()
  const navigate = useNavigate()
  
  // Note: Backend API may need to support companyId filter parameter
  // For now, we'll filter client-side on recent invoices
  const filteredRecentInvoices = companyFilter
    ? dashboardData?.recentInvoices?.filter(inv => {
        // We'd need invoice.companyId to filter properly
        // This is a placeholder - backend should support company filtering
        return true
      })
    : dashboardData?.recentInvoices



  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'paid':
        return 'default'
      case 'sent':
      case 'viewed':
        return 'secondary'
      case 'overdue':
        return 'destructive'
      case 'draft':
        return 'outline'
      default:
        return 'outline'
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600 mt-2">Welcome to your invoice management system</p>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader>
                <div className="h-4 bg-gray-200 rounded animate-pulse"></div>
              </CardHeader>
              <CardContent>
                <div className="h-8 bg-gray-200 rounded animate-pulse mb-2"></div>
                <div className="h-3 bg-gray-200 rounded animate-pulse w-3/4"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600 mt-2">Welcome to your invoice management system</p>
        </div>
        <Card>
          <CardContent className="p-6">
            <div className="text-center text-red-600">
              Failed to load dashboard data. Please try again later.
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  const stats = dashboardData?.stats

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600 mt-2">Welcome to your invoice management system</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
          <CompanyFilterDropdown
            value={companyFilter}
            onChange={setCompanyFilter}
          />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card className="cursor-pointer hover:shadow-lg transition-shadow" onClick={() => navigate('/invoices')}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Revenue</CardTitle>
            <span className="text-2xl">💰</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(stats?.totalRevenue || 0)}</div>
            <p className="text-xs text-muted-foreground">From paid invoices</p>
          </CardContent>
        </Card>

        <Card className="cursor-pointer hover:shadow-lg transition-shadow" onClick={() => navigate('/invoices')}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Outstanding</CardTitle>
            <span className="text-2xl">⏳</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(stats?.outstandingAmount || 0)}</div>
            <p className="text-xs text-muted-foreground">{stats?.outstandingCount || 0} unpaid invoices</p>
          </CardContent>
        </Card>

        <Card className="cursor-pointer hover:shadow-lg transition-shadow" onClick={() => navigate('/invoices')}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">This Month</CardTitle>
            <span className="text-2xl">📅</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(stats?.thisMonthAmount || 0)}</div>
            <p className="text-xs text-muted-foreground">{stats?.thisMonthCount || 0} invoices this month</p>
          </CardContent>
        </Card>

        <Card className="cursor-pointer hover:shadow-lg transition-shadow" onClick={() => navigate('/invoices')}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Overdue</CardTitle>
            <span className="text-2xl">⚠️</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{formatCurrency(stats?.overdueAmount || 0)}</div>
            <p className="text-xs text-muted-foreground">{stats?.overdueCount || 0} overdue invoices</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Recent Invoices</CardTitle>
              <CardDescription>Latest invoices from your business</CardDescription>
            </div>
            <button
              onClick={() => navigate('/invoices')}
              className="text-sm text-blue-600 hover:text-blue-800 font-medium"
            >
              View All →
            </button>
          </CardHeader>
          <CardContent>
            {filteredRecentInvoices && filteredRecentInvoices.length > 0 ? (
              <div className="space-y-4">
                {filteredRecentInvoices.map((invoice) => (
                  <div 
                    key={invoice.id} 
                    className="flex items-center justify-between p-2 rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
                    onClick={() => navigate(`/invoices/${invoice.id}`)}
                  >
                    <div>
                      <p className="font-medium">{invoice.invoiceNumber}</p>
                      <p className="text-sm text-gray-600">{invoice.customerName}</p>
                      {invoice.daysOverdue && invoice.daysOverdue > 0 && (
                        <p className="text-xs text-red-600">
                          {invoice.daysOverdue} day{invoice.daysOverdue > 1 ? 's' : ''} overdue
                        </p>
                      )}
                    </div>
                    <div className="text-right">
                      <p className="font-medium">{formatCurrency(invoice.totalAmount)}</p>
                      <Badge variant={getStatusColor(invoice.status)} className="text-xs">
                        {invoice.status.charAt(0).toUpperCase() + invoice.status.slice(1)}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center text-gray-500 py-4">
                No recent invoices found
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick Actions</CardTitle>
            <CardDescription>Common tasks to get you started</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div 
              className="flex items-center justify-between p-3 border rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
              onClick={() => navigate('/invoices/new')}
            >
              <div className="flex items-center space-x-3">
                <span className="text-xl">🧾</span>
                <span className="font-medium">Create New Invoice</span>
              </div>
              <span className="text-gray-400">→</span>
            </div>
            <div 
              className="flex items-center justify-between p-3 border rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
              onClick={() => navigate('/customers/new')}
            >
              <div className="flex items-center space-x-3">
                <span className="text-xl">👥</span>
                <span className="font-medium">Add New Customer</span>
              </div>
              <span className="text-gray-400">→</span>
            </div>
            <div 
              className="flex items-center justify-between p-3 border rounded-lg hover:bg-gray-50 cursor-pointer transition-colors"
              onClick={() => navigate('/products/new')}
            >
              <div className="flex items-center space-x-3">
                <span className="text-xl">📦</span>
                <span className="font-medium">Add Product/Service</span>
              </div>
              <span className="text-gray-400">→</span>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default Dashboard
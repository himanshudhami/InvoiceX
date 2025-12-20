import { lazy, Suspense } from 'react'
import { Routes, Route, Navigate, Outlet } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { NuqsAdapter } from 'nuqs/adapters/react-router/v7'
import { Toaster } from 'react-hot-toast'
import { ThemeProvider } from './contexts/ThemeContext'
import { CompanyProvider } from './contexts/CompanyContext'
import { useAuth } from './contexts/AuthContext'
import Layout from './components/Layout'
import ErrorBoundary from './components/ErrorBoundary'
import { appQueryClient } from './lib/queryClient'

// Lazy load all page components for better initial load performance
const Login = lazy(() => import('./pages/Login'))
const Dashboard = lazy(() => import('./pages/Dashboard'))
const InvoiceCreate = lazy(() => import('./pages/InvoiceCreate'))
const InvoiceEdit = lazy(() => import('./pages/InvoiceEdit'))
const InvoiceView = lazy(() => import('./pages/InvoiceView'))
const QuoteList = lazy(() => import('./pages/QuoteList'))
const QuoteCreate = lazy(() => import('./pages/QuoteCreate'))
const QuoteEdit = lazy(() => import('./pages/QuoteEdit'))
const QuoteView = lazy(() => import('./pages/QuoteView'))
const InvoiceList = lazy(() => import('./pages/InvoiceList'))
const CustomerList = lazy(() => import('./pages/CustomerList'))
const CustomerCreate = lazy(() => import('./pages/CustomerCreate'))
const CustomerEdit = lazy(() => import('./pages/CustomerEdit'))
const CustomerView = lazy(() => import('./pages/CustomerView'))
const ProductList = lazy(() => import('./pages/ProductList'))
const ProductCreate = lazy(() => import('./pages/ProductCreate'))
const ProductEdit = lazy(() => import('./pages/ProductEdit'))
const ProductView = lazy(() => import('./pages/ProductView'))
const Settings = lazy(() => import('./pages/Settings'))
const ApiTest = lazy(() => import('./pages/ApiTest'))
const CompaniesManagement = lazy(() => import('./pages/CompaniesManagement'))
const CustomersManagement = lazy(() => import('./pages/CustomersManagement'))
const ProductsManagement = lazy(() => import('./pages/ProductsManagement'))
const InvoicesManagement = lazy(() => import('./pages/InvoicesManagement'))
const TaxRatesManagement = lazy(() => import('./pages/TaxRatesManagement'))
const EmployeesManagement = lazy(() => import('./pages/EmployeesManagement'))
const ExpenseDashboard = lazy(() => import('./pages/ExpenseDashboard'))
const AssetsManagement = lazy(() => import('./pages/AssetsManagement'))
const AssetAssignments = lazy(() => import('./pages/AssetAssignments'))
const SubscriptionsManagement = lazy(() => import('./pages/SubscriptionsManagement'))
const LoanManagement = lazy(() => import('./pages/LoanManagement'))
const EmiPaymentManagement = lazy(() => import('./pages/EmiPaymentManagement'))
const PaymentsManagement = lazy(() => import('./pages/PaymentsManagement'))
const CompanyFinancialReport = lazy(() => import('./pages/CompanyFinancialReport'))
const PayrollDashboard = lazy(() => import('./pages/PayrollDashboard'))
const PayrollRuns = lazy(() => import('./pages/PayrollRuns'))
const PayrollRunDetail = lazy(() => import('./pages/PayrollRunDetail'))
const PayrollProcess = lazy(() => import('./pages/PayrollProcess'))
const EmployeeSalaryStructures = lazy(() => import('./pages/EmployeeSalaryStructures'))
const EmployeeTaxDeclarations = lazy(() => import('./pages/EmployeeTaxDeclarations'))
const ContractorPaymentsPage = lazy(() => import('./pages/ContractorPaymentsPage'))
const PayrollSettings = lazy(() => import('./pages/PayrollSettings'))
const PayslipView = lazy(() => import('./pages/PayslipView'))
const ProfessionalTaxSlabsManagement = lazy(() => import('./pages/ProfessionalTaxSlabsManagement'))
const CalculationRulesPage = lazy(() => import('./pages/CalculationRulesPage'))
const BankAccountsManagement = lazy(() => import('./pages/BankAccountsManagement'))
const BankStatementImport = lazy(() => import('./pages/BankStatementImport'))
const BankTransactionsPage = lazy(() => import('./pages/BankTransactionsPage'))
const TdsReceivablesManagement = lazy(() => import('./pages/TdsReceivablesManagement'))

// Leave Management
const LeaveTypesManagement = lazy(() => import('./pages/LeaveTypesManagement'))
const LeaveBalancesManagement = lazy(() => import('./pages/LeaveBalancesManagement'))
const LeaveApplicationsManagement = lazy(() => import('./pages/LeaveApplicationsManagement'))
const HolidaysManagement = lazy(() => import('./pages/HolidaysManagement'))

// Asset Requests
const AssetRequestsManagement = lazy(() => import('./pages/AssetRequestsManagement'))

// Employee Portal Features (Admin Management)
const AnnouncementsManagement = lazy(() => import('./pages/AnnouncementsManagement'))
const SupportTicketsManagement = lazy(() => import('./pages/SupportTicketsManagement'))
const EmployeeDocumentsManagement = lazy(() => import('./pages/EmployeeDocumentsManagement'))

// Administration
const Users = lazy(() => import('./pages/Users'))

// Approval Workflows
const WorkflowTemplatesManagement = lazy(() => import('./pages/WorkflowTemplatesManagement'))
const WorkflowTemplateEditor = lazy(() => import('./pages/WorkflowTemplateEditor'))
const OrgChartManagement = lazy(() => import('./pages/OrgChartManagement'))

// Loading spinner for Suspense fallback
const PageLoader = () => (
  <div className="flex items-center justify-center min-h-[400px]">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
  </div>
)

// Full screen loader for auth check
const FullScreenLoader = () => (
  <div className="flex items-center justify-center min-h-screen bg-gray-50 dark:bg-gray-900">
    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
  </div>
)

// Protected route wrapper
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

// Public route wrapper (redirects to dashboard if already authenticated)
function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />
  }

  return <>{children}</>
}

// Protected layout wrapper that includes the main layout
function ProtectedLayout() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return <FullScreenLoader />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return (
    <CompanyProvider>
      <Layout>
        <ErrorBoundary>
          <Suspense fallback={<PageLoader />}>
            <Outlet />
          </Suspense>
        </ErrorBoundary>
      </Layout>
    </CompanyProvider>
  )
}

function AppRoutes() {
  return (
    <NuqsAdapter>
      <div className="app">
        <Suspense fallback={<FullScreenLoader />}>
          <Routes>
            {/* Public Routes */}
            <Route
              path="/login"
              element={
                <PublicRoute>
                  <Login />
                </PublicRoute>
              }
            />

            {/* Protected Routes with Layout */}
            <Route element={<ProtectedLayout />}>
              <Route index element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<Dashboard />} />

              {/* Invoice Routes */}
              <Route path="/invoices" element={<InvoiceList />} />
              <Route path="/invoices/new" element={<InvoiceCreate />} />
              <Route path="/invoices/:id" element={<InvoiceView />} />
              <Route path="/invoices/:id/edit" element={<InvoiceEdit />} />

              {/* Quote Routes */}
              <Route path="/quotes" element={<QuoteList />} />
              <Route path="/quotes/new" element={<QuoteCreate />} />
              <Route path="/quotes/:id" element={<QuoteView />} />
              <Route path="/quotes/:id/edit" element={<QuoteEdit />} />

              {/* Customer Routes */}
              <Route path="/customers" element={<CustomerList />} />
              <Route path="/customers/new" element={<CustomerCreate />} />
              <Route path="/customers/:id" element={<CustomerView />} />
              <Route path="/customers/:id/edit" element={<CustomerEdit />} />

              {/* Product Routes */}
              <Route path="/products" element={<ProductList />} />
              <Route path="/products/new" element={<ProductCreate />} />
              <Route path="/products/:id" element={<ProductView />} />
              <Route path="/products/:id/edit" element={<ProductEdit />} />

              {/* Settings */}
              <Route path="/settings" element={<Settings />} />

              {/* API Test - Development only */}
              <Route path="/api-test" element={<ApiTest />} />

              {/* Management Routes */}
              <Route path="/companies" element={<CompaniesManagement />} />
              <Route path="/customers-mgmt" element={<CustomersManagement />} />
              <Route path="/products-mgmt" element={<ProductsManagement />} />
              <Route path="/invoices-mgmt" element={<InvoicesManagement />} />
              <Route path="/tax-rates" element={<TaxRatesManagement />} />
              <Route path="/employees" element={<EmployeesManagement />} />
              <Route path="/assets" element={<AssetsManagement />} />
              <Route path="/asset-assignments" element={<AssetAssignments />} />
              <Route path="/subscriptions" element={<SubscriptionsManagement />} />
              <Route path="/loans" element={<LoanManagement />} />
              <Route path="/emi-payments" element={<EmiPaymentManagement />} />
              <Route path="/payments" element={<PaymentsManagement />} />
              <Route path="/expense-dashboard" element={<ExpenseDashboard />} />
              <Route path="/financial-report" element={<CompanyFinancialReport />} />

              {/* Payroll Routes */}
              <Route path="/payroll" element={<PayrollDashboard />} />
              <Route path="/payroll/runs" element={<PayrollRuns />} />
              <Route path="/payroll/runs/:id" element={<PayrollRunDetail />} />
              <Route path="/payroll/process" element={<PayrollProcess />} />
              <Route path="/payroll/salary-structures" element={<EmployeeSalaryStructures />} />
              <Route path="/payroll/tax-declarations" element={<EmployeeTaxDeclarations />} />
              <Route path="/payroll/contractors" element={<ContractorPaymentsPage />} />
              <Route path="/payroll/settings" element={<PayrollSettings />} />
              <Route path="/payroll/settings/pt-slabs" element={<ProfessionalTaxSlabsManagement />} />
              <Route path="/payroll/calculation-rules" element={<CalculationRulesPage />} />
              <Route path="/payroll/payslip/:transactionId" element={<PayslipView />} />

              {/* Bank Routes */}
              <Route path="/bank/accounts" element={<BankAccountsManagement />} />
              <Route path="/bank/import" element={<BankStatementImport />} />
              <Route path="/bank/transactions" element={<BankTransactionsPage />} />

              {/* Tax Compliance Routes */}
              <Route path="/tds-receivables" element={<TdsReceivablesManagement />} />

              {/* Leave Management Routes */}
              <Route path="/leave/types" element={<LeaveTypesManagement />} />
              <Route path="/leave/balances" element={<LeaveBalancesManagement />} />
              <Route path="/leave/applications" element={<LeaveApplicationsManagement />} />
              <Route path="/leave/holidays" element={<HolidaysManagement />} />

              {/* Asset Requests */}
              <Route path="/asset-requests" element={<AssetRequestsManagement />} />

              {/* Employee Portal Management Routes */}
              <Route path="/announcements" element={<AnnouncementsManagement />} />
              <Route path="/support-tickets" element={<SupportTicketsManagement />} />
              <Route path="/employee-documents" element={<EmployeeDocumentsManagement />} />

              {/* Administration Routes */}
              <Route path="/users" element={<Users />} />

              {/* Approval Workflow Routes */}
              <Route path="/workflows" element={<WorkflowTemplatesManagement />} />
              <Route path="/workflows/:id/edit" element={<WorkflowTemplateEditor />} />

              {/* Organization Chart */}
              <Route path="/org-chart" element={<OrgChartManagement />} />
            </Route>

            {/* Catch-all redirect */}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Suspense>
        <Toaster position="top-right" />
      </div>
      <ReactQueryDevtools initialIsOpen={false} />
    </NuqsAdapter>
  )
}

function App() {
  return (
    <QueryClientProvider client={appQueryClient}>
      <ThemeProvider>
        <AppRoutes />
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App

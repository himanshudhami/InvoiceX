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
const Login = lazy(() => import('./pages/auth/Login'))
const Dashboard = lazy(() => import('./pages/core/Dashboard'))
const InvoiceCreate = lazy(() => import('./pages/billing/invoices/InvoiceCreate'))
const InvoiceEdit = lazy(() => import('./pages/billing/invoices/InvoiceEdit'))
const InvoiceView = lazy(() => import('./pages/billing/invoices/InvoiceView'))
const QuoteList = lazy(() => import('./pages/billing/quotes/QuoteList'))
const QuoteCreate = lazy(() => import('./pages/billing/quotes/QuoteCreate'))
const QuoteEdit = lazy(() => import('./pages/billing/quotes/QuoteEdit'))
const QuoteView = lazy(() => import('./pages/billing/quotes/QuoteView'))
const InvoiceList = lazy(() => import('./pages/billing/invoices/InvoiceList'))
const CreditNoteList = lazy(() => import('./pages/billing/credit-notes/CreditNoteList'))
const CreditNoteView = lazy(() => import('./pages/billing/credit-notes/CreditNoteView'))
const CreditNoteFromInvoice = lazy(() => import('./pages/billing/credit-notes/CreditNoteFromInvoice'))
const CustomerList = lazy(() => import('./pages/crm/customers/CustomerList'))
const CustomerCreate = lazy(() => import('./pages/crm/customers/CustomerCreate'))
const CustomerEdit = lazy(() => import('./pages/crm/customers/CustomerEdit'))
const CustomerView = lazy(() => import('./pages/crm/customers/CustomerView'))
const ProductList = lazy(() => import('./pages/catalog/products/ProductList'))
const ProductCreate = lazy(() => import('./pages/catalog/products/ProductCreate'))
const ProductEdit = lazy(() => import('./pages/catalog/products/ProductEdit'))
const ProductView = lazy(() => import('./pages/catalog/products/ProductView'))
const Settings = lazy(() => import('./pages/admin/settings/Settings'))
const ApiTest = lazy(() => import('./pages/dev/ApiTest'))
const CompaniesManagement = lazy(() => import('./pages/admin/companies/CompaniesManagement'))
const CustomersManagement = lazy(() => import('./pages/crm/customers/CustomersManagement'))
const ProductsManagement = lazy(() => import('./pages/catalog/products/ProductsManagement'))
const InvoicesManagement = lazy(() => import('./pages/billing/invoices/InvoicesManagement'))
const TaxRatesManagement = lazy(() => import('./pages/finance/tax/TaxRatesManagement'))
const EmployeesManagement = lazy(() => import('./pages/hr/employees/EmployeesManagement'))
const ExpenseDashboard = lazy(() => import('./pages/finance/expenses/ExpenseDashboard'))
const AssetsManagement = lazy(() => import('./pages/assets/AssetsManagement'))
const AssetAssignments = lazy(() => import('./pages/assets/AssetAssignments'))
const SubscriptionsManagement = lazy(() => import('./pages/finance/subscriptions/SubscriptionsManagement'))
const LoanManagement = lazy(() => import('./pages/finance/loans/LoanManagement'))
const EmiPaymentManagement = lazy(() => import('./pages/finance/loans/EmiPaymentManagement'))
const PaymentsManagement = lazy(() => import('./pages/finance/payments/PaymentsManagement'))
const CompanyFinancialReport = lazy(() => import('./pages/finance/reports/CompanyFinancialReport'))
const PayrollDashboard = lazy(() => import('./pages/hr/payroll/PayrollDashboard'))
const PayrollRuns = lazy(() => import('./pages/hr/payroll/PayrollRuns'))
const PayrollRunDetail = lazy(() => import('./pages/hr/payroll/PayrollRunDetail'))
const PayrollProcess = lazy(() => import('./pages/hr/payroll/PayrollProcess'))
const EmployeeSalaryStructures = lazy(() => import('./pages/hr/payroll/EmployeeSalaryStructures'))
const EmployeeTaxDeclarations = lazy(() => import('./pages/hr/payroll/EmployeeTaxDeclarations'))
const ContractorPaymentsPage = lazy(() => import('./pages/hr/payroll/ContractorPaymentsPage'))
const PayrollSettings = lazy(() => import('./pages/hr/payroll/PayrollSettings'))
const PayslipView = lazy(() => import('./pages/hr/payroll/PayslipView'))
const ProfessionalTaxSlabsManagement = lazy(() => import('./pages/hr/payroll/ProfessionalTaxSlabsManagement'))
const CalculationRulesPage = lazy(() => import('./pages/hr/payroll/CalculationRulesPage'))
const BankAccountsManagement = lazy(() => import('./pages/finance/banking/BankAccountsManagement'))
const BankStatementImport = lazy(() => import('./pages/finance/banking/BankStatementImport'))
const BankTransactionsPage = lazy(() => import('./pages/finance/banking/BankTransactionsPage'))
const BankReconciliationStatement = lazy(() => import('./pages/finance/banking/BankReconciliationStatement'))
const OutgoingPaymentsReconciliation = lazy(() => import('./pages/finance/expenses/OutgoingPaymentsReconciliation'))
const TdsReceivablesManagement = lazy(() => import('./pages/finance/tax/TdsReceivablesManagement'))

// Leave Management
const LeaveTypesManagement = lazy(() => import('./pages/hr/leave/LeaveTypesManagement'))
const LeaveBalancesManagement = lazy(() => import('./pages/hr/leave/LeaveBalancesManagement'))
const LeaveApplicationsManagement = lazy(() => import('./pages/hr/leave/LeaveApplicationsManagement'))
const HolidaysManagement = lazy(() => import('./pages/hr/leave/HolidaysManagement'))

// Asset Requests
const AssetRequestsManagement = lazy(() => import('./pages/assets/AssetRequestsManagement'))

// Employee Portal Features (Admin Management)
const AnnouncementsManagement = lazy(() => import('./pages/portal/AnnouncementsManagement'))
const SupportTicketsManagement = lazy(() => import('./pages/portal/SupportTicketsManagement'))
const EmployeeDocumentsManagement = lazy(() => import('./pages/portal/EmployeeDocumentsManagement'))

// Document & Expense Management
const DocumentCategoriesManagement = lazy(() => import('./pages/documents/DocumentCategoriesManagement'))
const ExpenseCategoriesManagement = lazy(() => import('./pages/finance/expenses/ExpenseCategoriesManagement'))
const ExpenseClaimsManagement = lazy(() => import('./pages/finance/expenses/ExpenseClaimsManagement'))

// Administration
const Users = lazy(() => import('./pages/admin/users/Users'))
const AuditTrailViewer = lazy(() => import('./pages/admin/audit/AuditTrailViewer'))

// General Ledger
const ChartOfAccountsManagement = lazy(() => import('./pages/finance/ledger/ChartOfAccountsManagement'))
const JournalEntriesManagement = lazy(() => import('./pages/finance/ledger/JournalEntriesManagement'))
const TrialBalanceReport = lazy(() => import('./pages/finance/ledger/TrialBalanceReport'))
const IncomeStatementReport = lazy(() => import('./pages/finance/ledger/IncomeStatementReport'))
const BalanceSheetReport = lazy(() => import('./pages/finance/ledger/BalanceSheetReport'))
const AccountLedgerReport = lazy(() => import('./pages/finance/ledger/AccountLedgerReport'))

// Approval Workflows
const WorkflowTemplatesManagement = lazy(() => import('./pages/workflows/WorkflowTemplatesManagement'))
const WorkflowTemplateEditor = lazy(() => import('./pages/workflows/WorkflowTemplateEditor'))
const OrgChartManagement = lazy(() => import('./pages/hr/employees/OrgChartManagement'))

// E-Invoice
const EInvoiceSettings = lazy(() => import('./pages/finance/tax/EInvoiceSettings'))

// Tax Rule Packs
const TaxRulePacksManagement = lazy(() => import('./pages/finance/tax/TaxRulePacksManagement'))

// Advance Tax (Section 207 - Corporate)
const AdvanceTaxManagement = lazy(() => import('./pages/finance/tax/AdvanceTaxManagement'))
const AdvanceTaxComplianceDashboard = lazy(() => import('./pages/finance/tax/advance-tax/AdvanceTaxComplianceDashboard'))

// GST Compliance
const GstComplianceDashboard = lazy(() => import('./pages/finance/tax/gst/GstComplianceDashboard'))
const RcmManagement = lazy(() => import('./pages/finance/tax/gst/RcmManagement'))
const TdsReturnsManagement = lazy(() => import('./pages/finance/tax/gst/TdsReturnsManagement'))
const LdcManagement = lazy(() => import('./pages/finance/tax/gst/LdcManagement'))
const TcsManagement = lazy(() => import('./pages/finance/tax/gst/TcsManagement'))
const ItcBlockedManagement = lazy(() => import('./pages/finance/tax/gst/ItcBlockedManagement'))
const ItcReversalManagement = lazy(() => import('./pages/finance/tax/gst/ItcReversalManagement'))
const Gstr3bFilingPack = lazy(() => import('./pages/finance/tax/gst/Gstr3bFilingPack'))
const Gstr2bReconciliation = lazy(() => import('./pages/finance/tax/gst/Gstr2bReconciliation'))

// Export & Forex Management
const ExportDashboard = lazy(() => import('./pages/finance/exports/ExportDashboard'))
const FircManagement = lazy(() => import('./pages/finance/exports/FircManagement'))
const LutRegister = lazy(() => import('./pages/finance/exports/LutRegister'))
const FemaCompliance = lazy(() => import('./pages/finance/exports/FemaCompliance'))
const ReceivablesAgeing = lazy(() => import('./pages/finance/exports/ReceivablesAgeing'))

// Statutory Compliance (TDS, PF, ESI)
const StatutoryDashboard = lazy(() => import('./pages/finance/statutory/StatutoryDashboard'))
const Form16Management = lazy(() => import('./pages/finance/statutory/Form16Management'))
const TdsChallanManagement = lazy(() => import('./pages/finance/statutory/TdsChallanManagement'))
const PfEcrManagement = lazy(() => import('./pages/finance/statutory/PfEcrManagement'))
const EsiReturnManagement = lazy(() => import('./pages/finance/statutory/EsiReturnManagement'))
const Form24QFilings = lazy(() => import('./pages/finance/statutory/Form24QFilings'))

// Accounts Payable (Vendor Management)
const VendorsManagement = lazy(() => import('./pages/finance/ap/VendorsManagement'))
const VendorInvoicesManagement = lazy(() => import('./pages/finance/ap/VendorInvoicesManagement'))
const VendorPaymentsManagement = lazy(() => import('./pages/finance/ap/VendorPaymentsManagement'))
const ContractorsManagement = lazy(() => import('./pages/finance/ap/ContractorsManagement'))

// Tags & Attribution (Cost Center Alternative)
const TagsManagement = lazy(() => import('./pages/settings/tags/TagsManagement'))
const AttributionRulesManagement = lazy(() => import('./pages/settings/tags/AttributionRulesManagement'))

// Inventory Management
const WarehousesManagement = lazy(() => import('./pages/inventory/warehouses/WarehousesManagement'))
const StockGroupsManagement = lazy(() => import('./pages/inventory/stockgroups/StockGroupsManagement'))
const UnitsOfMeasureManagement = lazy(() => import('./pages/inventory/units/UnitsOfMeasureManagement'))
const StockItemsManagement = lazy(() => import('./pages/inventory/items/StockItemsManagement'))
const StockMovementsManagement = lazy(() => import('./pages/inventory/movements/StockMovementsManagement'))
const StockTransfersManagement = lazy(() => import('./pages/inventory/transfers/StockTransfersManagement'))

// Manufacturing
const BomManagement = lazy(() => import('./pages/manufacturing/bom/BomManagement'))
const ProductionOrdersManagement = lazy(() => import('./pages/manufacturing/production/ProductionOrdersManagement'))
const SerialNumbersManagement = lazy(() => import('./pages/manufacturing/serial/SerialNumbersManagement'))

// Tally Migration
const TallyMigration = lazy(() => import('./pages/settings/migration/TallyMigration'))
const TallyMigrationHistory = lazy(() => import('./pages/settings/migration/TallyMigrationHistory'))

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

              {/* Credit Note Routes */}
              <Route path="/credit-notes" element={<CreditNoteList />} />
              <Route path="/credit-notes/:id" element={<CreditNoteView />} />
              <Route path="/credit-notes/from-invoice/:invoiceId" element={<CreditNoteFromInvoice />} />

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
              <Route path="/payroll/settings" element={<PayrollSettings />} />
              <Route path="/payroll/settings/pt-slabs" element={<ProfessionalTaxSlabsManagement />} />
              <Route path="/payroll/calculation-rules" element={<CalculationRulesPage />} />
              <Route path="/payroll/payslip/:transactionId" element={<PayslipView />} />

              {/* Bank Routes */}
              <Route path="/bank/accounts" element={<BankAccountsManagement />} />
              <Route path="/bank/import" element={<BankStatementImport />} />
              <Route path="/bank/transactions" element={<BankTransactionsPage />} />
              <Route path="/bank/brs/:bankAccountId" element={<BankReconciliationStatement />} />
              <Route path="/bank/outgoing-payments" element={<OutgoingPaymentsReconciliation />} />

              {/* Tax Compliance Routes */}
              <Route path="/tds-receivables" element={<TdsReceivablesManagement />} />

              {/* Export & Forex Routes */}
              <Route path="/exports" element={<ExportDashboard />} />
              <Route path="/exports/firc" element={<FircManagement />} />
              <Route path="/exports/lut" element={<LutRegister />} />
              <Route path="/exports/fema" element={<FemaCompliance />} />
              <Route path="/exports/ageing" element={<ReceivablesAgeing />} />

              {/* General Ledger Routes */}
              <Route path="/ledger/accounts" element={<ChartOfAccountsManagement />} />
              <Route path="/ledger/journals" element={<JournalEntriesManagement />} />
              <Route path="/ledger/trial-balance" element={<TrialBalanceReport />} />
              <Route path="/ledger/income-statement" element={<IncomeStatementReport />} />
              <Route path="/ledger/balance-sheet" element={<BalanceSheetReport />} />
              <Route path="/ledger/account-ledger" element={<AccountLedgerReport />} />

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

              {/* Document & Expense Management Routes */}
              <Route path="/document-categories" element={<DocumentCategoriesManagement />} />
              <Route path="/expense-categories" element={<ExpenseCategoriesManagement />} />
              <Route path="/expense-claims" element={<ExpenseClaimsManagement />} />

              {/* Administration Routes */}
              <Route path="/users" element={<Users />} />
              <Route path="/admin/audit" element={<AuditTrailViewer />} />

              {/* Approval Workflow Routes */}
              <Route path="/workflows" element={<WorkflowTemplatesManagement />} />
              <Route path="/workflows/:id/edit" element={<WorkflowTemplateEditor />} />

              {/* Organization Chart */}
              <Route path="/org-chart" element={<OrgChartManagement />} />

              {/* E-Invoice Settings */}
              <Route path="/einvoice/settings" element={<EInvoiceSettings />} />

              {/* Tax Rule Packs */}
              <Route path="/tax-rule-packs" element={<TaxRulePacksManagement />} />

              {/* Advance Tax (Section 207 - Corporate) */}
              <Route path="/tax/advance-tax" element={<AdvanceTaxManagement />} />
              <Route path="/tax/advance-tax/compliance" element={<AdvanceTaxComplianceDashboard />} />

              {/* GST Compliance Routes */}
              <Route path="/gst" element={<GstComplianceDashboard />} />
              <Route path="/gst/rcm" element={<RcmManagement />} />
              <Route path="/gst/tds-returns" element={<TdsReturnsManagement />} />
              <Route path="/gst/ldc" element={<LdcManagement />} />
              <Route path="/gst/tcs" element={<TcsManagement />} />
              <Route path="/gst/itc-blocked" element={<ItcBlockedManagement />} />
              <Route path="/gst/itc-reversal" element={<ItcReversalManagement />} />
              <Route path="/gst/gstr3b" element={<Gstr3bFilingPack />} />
              <Route path="/gst/gstr2b" element={<Gstr2bReconciliation />} />

              {/* Statutory Compliance Routes (TDS, PF, ESI) */}
              <Route path="/statutory" element={<StatutoryDashboard />} />
              <Route path="/statutory/form16" element={<Form16Management />} />
              <Route path="/statutory/tds-challan" element={<TdsChallanManagement />} />
              <Route path="/statutory/pf-ecr" element={<PfEcrManagement />} />
              <Route path="/statutory/esi-return" element={<EsiReturnManagement />} />
              <Route path="/statutory/form24q" element={<Form24QFilings />} />

              {/* Accounts Payable Routes (Vendors, Bills, Payments, Contractors) */}
              <Route path="/finance/ap/vendors" element={<VendorsManagement />} />
              <Route path="/finance/ap/vendor-invoices" element={<VendorInvoicesManagement />} />
              <Route path="/finance/ap/vendor-payments" element={<VendorPaymentsManagement />} />
              <Route path="/finance/ap/contractors" element={<ContractorsManagement />} />
              <Route path="/finance/ap/contractor-payments" element={<ContractorPaymentsPage />} />

              {/* Tags & Attribution Routes (Cost Center Alternative) */}
              <Route path="/settings/tags" element={<TagsManagement />} />
              <Route path="/settings/attribution-rules" element={<AttributionRulesManagement />} />

              {/* Inventory Management Routes */}
              <Route path="/inventory/warehouses" element={<WarehousesManagement />} />
              <Route path="/inventory/stock-groups" element={<StockGroupsManagement />} />
              <Route path="/inventory/units" element={<UnitsOfMeasureManagement />} />
              <Route path="/inventory/items" element={<StockItemsManagement />} />
              <Route path="/inventory/movements" element={<StockMovementsManagement />} />
              <Route path="/inventory/transfers" element={<StockTransfersManagement />} />

              {/* Manufacturing Routes */}
              <Route path="/manufacturing/bom" element={<BomManagement />} />
              <Route path="/manufacturing/production" element={<ProductionOrdersManagement />} />
              <Route path="/manufacturing/serial-numbers" element={<SerialNumbersManagement />} />

              {/* Tally Migration Routes */}
              <Route path="/settings/migration/tally" element={<TallyMigration />} />
              <Route path="/settings/migration/history" element={<TallyMigrationHistory />} />
              <Route path="/settings/migration/history/:batchId" element={<TallyMigrationHistory />} />
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

// Central export point for all API services
export * from './types'
export * from './client'

export * from './admin/companyService'
export * from './core/dashboardService'

export * from './billing/invoiceService'
export * from './billing/quoteService'
export * from './billing/invoiceTemplateService'

export * from './crm/customerService'
export * from './catalog/productService'

export * from './assets/assetService'
export * from './assets/assetRequestService'

export * from './documents/documentCategoryService'
export * from './documents/fileService'

export * from './portal/announcementService'
export * from './portal/supportTicketService'
export * from './portal/employeeDocumentService'

export * from './workflows/approvalWorkflowService'

export * from './finance/banking/bankAccountService'
export * from './finance/banking/bankTransactionService'
export * from './finance/expenses/expenseCategoryService'
export * from './finance/expenses/expenseClaimService'
export * from './finance/ledger/ledgerService'
export * from './finance/loans/loanService'
export * from './finance/payments/paymentService'
export * from './finance/payments/paymentAllocationService'
export * from './finance/reports/cashFlowService'
export * from './finance/subscriptions/subscriptionService'
export * from './finance/tax/taxRateService'
export * from './finance/tax/tdsReceivableService'
export * from './finance/tax/taxRulePackService'
export * from './finance/tax/eInvoiceService'

export * from './hr/employees/employeeService'
export * from './hr/employees/hierarchyService'
export * from './hr/leave/leaveTypeService'
export * from './hr/leave/leaveBalanceService'
export * from './hr/leave/leaveApplicationService'
export * from './hr/leave/holidayService'
export * from './hr/payroll/payrollService'

// DEPRECATED: employeeSalaryTransactionService - Use payrollService instead
export * from './hr/payroll/employeeSalaryTransactionService'

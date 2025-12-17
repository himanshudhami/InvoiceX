// Auth types
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserInfo
}

export interface UserInfo {
  id: string
  email: string
  role: string
  employeeId: string | null
  companyId: string
  employeeName: string | null
}

export interface RefreshTokenRequest {
  refreshToken: string
}

// Employee Profile
export interface EmployeeProfile {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  email: string
  phone: string | null
  department: string | null
  designation: string | null
  dateOfJoining: string
  panMasked: string | null
  bankAccountMasked: string | null
  companyName: string
}

// Dashboard
export interface PortalDashboard {
  employee: EmployeeProfile
  leaveBalance: LeaveBalanceSummary[]
  recentPayslips: PayslipSummary[]
  upcomingHolidays: Holiday[]
  pendingLeaveApplications: number
  assignedAssets: number
}

// Leave types
export interface LeaveType {
  id: string
  name: string
  code: string
  daysPerYear: number
  carryForwardAllowed: boolean
  maxCarryForwardDays: number
  encashmentAllowed: boolean
  requiresApproval: boolean
  description: string | null
  isActive: boolean
}

export interface LeaveBalanceSummary {
  leaveTypeId: string
  leaveTypeName: string
  leaveTypeCode: string
  totalEntitlement: number
  taken: number
  available: number
  pending: number
}

export interface LeaveBalance {
  id: string
  employeeId: string
  leaveTypeId: string
  leaveTypeName: string
  leaveTypeCode: string
  financialYear: string
  openingBalance: number
  accrued: number
  taken: number
  carryForwarded: number
  adjusted: number
  encashed: number
  availableBalance: number
}

export interface LeaveApplicationSummary {
  id: string
  leaveTypeName: string
  leaveTypeCode: string
  fromDate: string
  toDate: string
  totalDays: number
  status: LeaveStatus
  appliedAt: string
  reason: string | null
}

export interface LeaveApplicationDetail extends LeaveApplicationSummary {
  employeeId: string
  employeeName: string
  leaveTypeId: string
  isHalfDayStart: boolean
  isHalfDayEnd: boolean
  approvedBy: string | null
  approvedByName: string | null
  approvedAt: string | null
  rejectionReason: string | null
  cancellationReason: string | null
}

export type LeaveStatus = 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn'

export interface ApplyLeaveRequest {
  leaveTypeId: string
  fromDate: string
  toDate: string
  isHalfDayStart?: boolean
  isHalfDayEnd?: boolean
  reason?: string
}

export interface LeaveCalculation {
  fromDate: string
  toDate: string
  totalDays: number
  workingDays: number
  holidays: Holiday[]
  weekendDays: number
}

// Holiday
export interface Holiday {
  id: string
  name: string
  date: string
  isOptional: boolean
  year: number
}

// Leave Calendar Event
export interface LeaveCalendarEvent {
  id: string
  title: string
  date: string
  endDate: string | null
  type: 'leave' | 'holiday'
  status: LeaveStatus | null
  isOptional: boolean | null
}

// Leave Dashboard
export interface LeaveDashboard {
  balances: LeaveBalance[]
  upcomingLeaves: LeaveApplicationSummary[]
  upcomingHolidays: Holiday[]
  pendingApplications: number
  approvedUpcoming: number
}

// Payslip
export interface PayslipSummary {
  id: string
  month: number
  year: number
  grossEarnings: number
  totalDeductions: number
  netPay: number
  paidOn: string | null
  status: string
}

export interface PayslipDetail extends PayslipSummary {
  employeeCode: string
  employeeName: string
  department: string | null
  designation: string | null
  earnings: PayslipComponent[]
  deductions: PayslipComponent[]
  bankAccount: string
  panNumber: string
}

export interface PayslipComponent {
  name: string
  amount: number
}

// Asset
export interface MyAsset {
  id: string
  assetName: string
  assetCode: string
  category: string
  serialNumber: string | null
  assignedDate: string
  expectedReturnDate: string | null
  status: string
  condition: string
}

// Tax Declaration
export interface TaxDeclarationSummary {
  id: string
  financialYear: string
  status: string
  totalDeclaredAmount: number
  verifiedAmount: number
  submittedAt: string | null
}

export interface TaxDeclarationDetail extends TaxDeclarationSummary {
  sections: TaxDeclarationSection[]
}

export interface TaxDeclarationSection {
  sectionCode: string
  sectionName: string
  maxLimit: number
  declaredAmount: number
  verifiedAmount: number
  items: TaxDeclarationItem[]
}

export interface TaxDeclarationItem {
  id: string
  description: string
  declaredAmount: number
  verifiedAmount: number
  documentPath: string | null
}

// API Response wrapper
export interface ApiResponse<T> {
  data: T
  correlationId?: string
}

export interface ApiError {
  error: {
    type: string
    message: string
    details?: string[]
  }
  correlationId?: string
}

// Pagination
export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// ==================== Auth Types ====================

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

// ==================== Company Types ====================

export interface Company {
  id: string
  name: string
  email: string | null
  phone: string | null
  website: string | null
  address: string | null
  city: string | null
  state: string | null
  country: string | null
  postalCode: string | null
  gstin: string | null
  pan: string | null
  tan: string | null
  logoUrl: string | null
  createdAt: string
  updatedAt: string
}

// ==================== Employee Types ====================

export interface Employee {
  id: string
  companyId: string
  employeeCode: string
  firstName: string
  lastName: string
  email: string
  phone: string | null
  department: string | null
  designation: string | null
  dateOfJoining: string
  dateOfBirth: string | null
  gender: string | null
  maritalStatus: string | null
  pan: string | null
  aadhaar: string | null
  bankAccountNumber: string | null
  bankName: string | null
  ifscCode: string | null
  pfAccountNumber: string | null
  uanNumber: string | null
  esiNumber: string | null
  status: string
  createdAt: string
  updatedAt: string
}

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

// ==================== Leave Types ====================

export interface LeaveType {
  id: string
  companyId: string
  name: string
  code: string
  daysPerYear: number
  carryForwardAllowed: boolean
  maxCarryForwardDays: number
  encashmentAllowed: boolean
  requiresApproval: boolean
  description: string | null
  isActive: boolean
  createdAt: string
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

export interface LeaveBalanceSummary {
  leaveTypeId: string
  leaveTypeName: string
  leaveTypeCode: string
  totalEntitlement: number
  taken: number
  available: number
  pending: number
}

export type LeaveStatus = 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn'

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

export interface LeaveDashboard {
  balances: LeaveBalance[]
  upcomingLeaves: LeaveApplicationSummary[]
  upcomingHolidays: Holiday[]
  pendingApplications: number
  approvedUpcoming: number
}

// ==================== Holiday Types ====================

export interface Holiday {
  id: string
  companyId: string
  name: string
  date: string
  isOptional: boolean
  year: number
}

export interface LeaveCalendarEvent {
  id: string
  title: string
  date: string
  endDate: string | null
  type: 'leave' | 'holiday'
  status: LeaveStatus | null
  isOptional: boolean | null
}

// ==================== Payslip Types ====================

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

// ==================== Asset Types ====================

export interface Asset {
  id: string
  companyId: string
  assetCode: string
  name: string
  category: string
  description: string | null
  serialNumber: string | null
  purchaseDate: string | null
  purchasePrice: number | null
  currentValue: number | null
  status: string
  condition: string
  location: string | null
  assignedTo: string | null
  assignedDate: string | null
  warrantyExpiry: string | null
  createdAt: string
  updatedAt: string
}

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

// ==================== Tax Declaration Types ====================

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

// ==================== Portal Dashboard ====================

export interface PortalDashboard {
  employee: EmployeeProfile
  leaveBalance: LeaveBalanceSummary[]
  recentPayslips: PayslipSummary[]
  upcomingHolidays: Holiday[]
  pendingLeaveApplications: number
  assignedAssets: number
}

// ==================== Announcement Types ====================

export interface AnnouncementSummary {
  id: string
  title: string
  content: string
  category: string
  priority: string
  isPinned: boolean
  publishedAt: string | null
  isRead: boolean
}

export interface AnnouncementDetail extends AnnouncementSummary {
  expiresAt: string | null
  createdBy: string | null
  createdByName: string | null
  createdAt: string
  updatedAt: string
}

// ==================== API Types ====================

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

export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// ==================== Utility Types ====================

export type StatusVariant = 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn' | 'default' | 'info' | 'warning' | 'success' | 'error'

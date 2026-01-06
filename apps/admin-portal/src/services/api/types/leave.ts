// Leave Management Types
import type { PaginationParams } from './common';
import type { Employee } from './employee';

export interface LeaveType {
  id: string;
  companyId: string;
  name: string;
  code: string;
  description?: string;
  daysPerYear: number;
  carryForwardAllowed: boolean;
  maxCarryForwardDays: number;
  encashmentAllowed: boolean;
  maxEncashmentDays?: number;
  isPaidLeave: boolean;
  requiresApproval: boolean;
  minDaysNotice?: number;
  maxConsecutiveDays?: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLeaveTypeDto {
  companyId: string;
  name: string;
  code: string;
  description?: string;
  daysPerYear: number;
  carryForwardAllowed?: boolean;
  maxCarryForwardDays?: number;
  encashmentAllowed?: boolean;
  maxEncashmentDays?: number;
  isPaidLeave?: boolean;
  requiresApproval?: boolean;
  minDaysNotice?: number;
  maxConsecutiveDays?: number;
  isActive?: boolean;
}

export interface UpdateLeaveTypeDto extends Partial<CreateLeaveTypeDto> {}

export interface EmployeeLeaveBalance {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  financialYear: string;
  openingBalance: number;
  accrued: number;
  taken: number;
  carryForwarded: number;
  adjusted: number;
  encashed: number;
  available: number;
  createdAt?: string;
  updatedAt?: string;
  // Joined fields
  employee?: Employee;
  leaveType?: LeaveType;
}

export interface CreateLeaveBalanceDto {
  employeeId: string;
  leaveTypeId: string;
  financialYear: string;
  openingBalance?: number;
  accrued?: number;
  taken?: number;
  carryForwarded?: number;
  adjusted?: number;
  encashed?: number;
}

export interface UpdateLeaveBalanceDto {
  openingBalance?: number;
  accrued?: number;
  taken?: number;
  carryForwarded?: number;
  adjusted?: number;
  encashed?: number;
}

export interface AdjustLeaveBalanceDto {
  adjustment: number;
  reason: string;
}

export interface LeaveApplication {
  id: string;
  employeeId: string;
  employeeName?: string;
  employeeCode?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  leaveTypeCode?: string;
  leaveTypeColor?: string;
  companyId: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  isHalfDay: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
  status: 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn';
  appliedAt: string;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: string;
  rejectionReason?: string;
  createdAt?: string;
  updatedAt?: string;
  // Joined fields
  employee?: Employee;
  leaveType?: LeaveType;
  approver?: Employee;
}

export interface CreateLeaveApplicationDto {
  employeeId: string;
  leaveTypeId: string;
  companyId: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  isHalfDay?: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
}

export interface UpdateLeaveApplicationDto {
  fromDate?: string;
  toDate?: string;
  totalDays?: number;
  isHalfDay?: boolean;
  halfDayType?: 'first_half' | 'second_half';
  reason?: string;
}

export interface ApproveLeaveDto {
  comments?: string;
}

export interface RejectLeaveDto {
  reason: string;
}

export interface Holiday {
  id: string;
  companyId: string;
  name: string;
  date: string;
  year: number;
  isOptional: boolean;
  isFloating: boolean;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateHolidayDto {
  companyId: string;
  name: string;
  date: string;
  year: number;
  isOptional?: boolean;
  isFloating?: boolean;
  description?: string;
}

export interface UpdateHolidayDto extends Partial<CreateHolidayDto> {}

export interface BulkHolidaysDto {
  holidays: CreateHolidayDto[];
  skipDuplicates?: boolean;
}

export interface LeaveApplicationFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  leaveTypeId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  financialYear?: string;
}

export interface LeaveBalanceFilterParams extends PaginationParams {
  companyId?: string;
  employeeId?: string;
  leaveTypeId?: string;
  financialYear?: string;
}

export interface HolidayFilterParams extends PaginationParams {
  companyId?: string;
  year?: number;
  isOptional?: boolean;
}

export interface LeaveSummary {
  employeeId: string;
  employeeName: string;
  financialYear: string;
  balances: {
    leaveTypeName: string;
    leaveTypeCode: string;
    opening: number;
    accrued: number;
    taken: number;
    available: number;
  }[];
}

export interface LeaveCalendarEntry {
  date: string;
  type: 'holiday' | 'leave';
  name: string;
  employeeName?: string;
  leaveType?: string;
  isOptional?: boolean;
}

// Subscription management types
import type { PaginationParams } from './common';

export interface Subscription {
  id: string;
  companyId: string;
  name: string;
  vendor?: string;
  planName?: string;
  category?: string;
  status: string;
  startDate?: string;
  renewalDate?: string;
  renewalPeriod?: string;
  seatsTotal?: number;
  seatsUsed?: number;
  licenseKey?: string;
  costPerPeriod?: number;
  costPerSeat?: number;
  currency?: string;
  billingCycleStart?: string;
  billingCycleEnd?: string;
  autoRenew?: boolean;
  url?: string;
  notes?: string;
  pausedOn?: string;
  resumedOn?: string;
  cancelledOn?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface SubscriptionAssignment {
  id: string;
  subscriptionId: string;
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  seatIdentifier?: string;
  role?: string;
  assignedOn: string;
  revokedOn?: string;
  notes?: string;
}

export interface CreateSubscriptionDto {
  companyId: string;
  name: string;
  vendor?: string;
  planName?: string;
  category?: string;
  status?: string;
  startDate?: string;
  renewalDate?: string;
  renewalPeriod?: string;
  seatsTotal?: number;
  seatsUsed?: number;
  licenseKey?: string;
  costPerPeriod?: number;
  costPerSeat?: number;
  currency?: string;
  billingCycleStart?: string;
  billingCycleEnd?: string;
  autoRenew?: boolean;
  url?: string;
  notes?: string;
}

export interface UpdateSubscriptionDto extends Partial<CreateSubscriptionDto> {}

export interface CreateSubscriptionAssignmentDto {
  targetType: 'employee' | 'company';
  companyId: string;
  employeeId?: string;
  seatIdentifier?: string;
  role?: string;
  assignedOn?: string;
  notes?: string;
}

export interface RevokeSubscriptionAssignmentDto {
  revokedOn?: string;
  notes?: string;
}

export interface SubscriptionMonthlyExpense {
  year: number;
  month: number;
  totalCost: number;
  currency: string;
  totalCostInInr: number;
  activeSubscriptionCount: number;
}

export interface SubscriptionCostReport {
  totalMonthlyCost: number;
  totalYearlyCost: number;
  totalCostInInr: number;
  activeSubscriptionCount: number;
  totalSubscriptionCount: number;
  monthlyExpenses: SubscriptionMonthlyExpense[];
}

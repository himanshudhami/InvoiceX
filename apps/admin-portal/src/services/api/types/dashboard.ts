// Dashboard types

export interface DashboardStats {
  totalRevenue: number;
  outstandingAmount: number;
  thisMonthAmount: number;
  overdueAmount: number;
  outstandingCount: number;
  thisMonthCount: number;
  overdueCount: number;
}

export interface RecentInvoice {
  id: string;
  invoiceNumber: string;
  customerName: string;
  totalAmount: number;
  status: string;
  invoiceDate: string;
  dueDate: string;
  daysOverdue?: number;
}

export interface DashboardData {
  stats: DashboardStats;
  recentInvoices: RecentInvoice[];
}

import { apiClient } from '../client';
import { DashboardData } from '../types';

/**
 * Dashboard API service for retrieving aggregated business data
 */
export class DashboardService {
  private readonly endpoint = 'dashboard';

  async getDashboardData(): Promise<DashboardData> {
    return apiClient.get<DashboardData>(this.endpoint);
  }
}

// Singleton instance
export const dashboardService = new DashboardService();
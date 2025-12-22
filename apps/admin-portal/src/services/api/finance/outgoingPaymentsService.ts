import { apiClient } from '../client';
import {
  OutgoingPayment,
  OutgoingPaymentsSummary,
  OutgoingPaymentsFilterParams,
  PagedResponse
} from '../types';

export class OutgoingPaymentsService {
  private readonly endpoint = 'outgoing-payments';

  /**
   * Get paginated list of outgoing payments
   */
  async getOutgoingPayments(
    companyId: string,
    params?: OutgoingPaymentsFilterParams
  ): Promise<PagedResponse<OutgoingPayment>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.reconciled !== undefined) queryParams.append('reconciled', params.reconciled.toString());
    if (params?.types) queryParams.append('types', params.types);
    if (params?.fromDate) queryParams.append('fromDate', params.fromDate);
    if (params?.toDate) queryParams.append('toDate', params.toDate);

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<OutgoingPayment>>(
      `${this.endpoint}/${companyId}${query ? `?${query}` : ''}`
    );
  }

  /**
   * Get outgoing payments pending reconciliation
   */
  async getToReconcile(
    companyId: string,
    params?: OutgoingPaymentsFilterParams
  ): Promise<PagedResponse<OutgoingPayment>> {
    const queryParams = new URLSearchParams();
    if (params?.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params?.types) queryParams.append('types', params.types);
    if (params?.fromDate) queryParams.append('fromDate', params.fromDate);
    if (params?.toDate) queryParams.append('toDate', params.toDate);

    const query = queryParams.toString();
    return apiClient.get<PagedResponse<OutgoingPayment>>(
      `${this.endpoint}/${companyId}/to-reconcile${query ? `?${query}` : ''}`
    );
  }

  /**
   * Get summary of outgoing payments
   */
  async getSummary(
    companyId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<OutgoingPaymentsSummary> {
    const queryParams = new URLSearchParams();
    if (fromDate) queryParams.append('fromDate', fromDate);
    if (toDate) queryParams.append('toDate', toDate);

    const query = queryParams.toString();
    return apiClient.get<OutgoingPaymentsSummary>(
      `${this.endpoint}/${companyId}/summary${query ? `?${query}` : ''}`
    );
  }
}

export const outgoingPaymentsService = new OutgoingPaymentsService();

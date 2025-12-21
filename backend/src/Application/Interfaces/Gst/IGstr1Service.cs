using Application.DTOs.Gst;
using Core.Common;

namespace Application.Interfaces.Gst
{
    /// <summary>
    /// Service interface for GSTR-1 preparation and filing
    /// GSTR-1 is the return for outward supplies filed monthly/quarterly
    /// </summary>
    public interface IGstr1Service
    {
        // ==================== Data Extraction ====================

        /// <summary>
        /// Generate GSTR-1 data for a given period
        /// </summary>
        Task<Result<Gstr1DataDto>> GenerateGstr1DataAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get Table 4 - B2B Supplies (to registered persons)
        /// </summary>
        Task<Result<IEnumerable<Gstr1B2bDto>>> GetB2bDataAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get Table 5 - B2C Large (to unregistered persons, interstate > 2.5L)
        /// </summary>
        Task<Result<IEnumerable<Gstr1B2clDto>>> GetB2clDataAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get Table 6A - Exports with payment of tax
        /// </summary>
        Task<Result<IEnumerable<Gstr1ExportDto>>> GetExportsWithTaxAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get Table 6B - Exports without payment of tax (under LUT/Bond)
        /// </summary>
        Task<Result<IEnumerable<Gstr1ExportDto>>> GetExportsWithoutTaxAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get HSN-wise summary of outward supplies
        /// </summary>
        Task<Result<IEnumerable<Gstr1HsnSummaryDto>>> GetHsnSummaryAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Get Document-issued summary
        /// </summary>
        Task<Result<IEnumerable<Gstr1DocIssuedDto>>> GetDocumentIssuedSummaryAsync(Guid companyId, string returnPeriod);

        // ==================== Filing Management ====================

        /// <summary>
        /// Save GSTR-1 filing record
        /// </summary>
        Task<Result<Gstr1FilingDto>> SaveFilingAsync(Guid companyId, string returnPeriod, Gstr1DataDto data);

        /// <summary>
        /// Mark GSTR-1 as filed
        /// </summary>
        Task<Result> MarkAsFiledAsync(Guid filingId, string arn, DateTime filingDate);

        /// <summary>
        /// Get filing history
        /// </summary>
        Task<Result<IEnumerable<Gstr1FilingDto>>> GetFilingHistoryAsync(Guid companyId, int? year = null);

        // ==================== Validation ====================

        /// <summary>
        /// Validate GSTR-1 data before filing
        /// </summary>
        Task<Result<Gstr1ValidationResultDto>> ValidateDataAsync(Guid companyId, string returnPeriod);

        /// <summary>
        /// Check for missing invoices that should be in GSTR-1
        /// </summary>
        Task<Result<IEnumerable<MissingInvoiceDto>>> GetMissingInvoicesAsync(Guid companyId, string returnPeriod);

        // ==================== Summary ====================

        /// <summary>
        /// Get tax liability summary for GSTR-1
        /// </summary>
        Task<Result<Gstr1TaxSummaryDto>> GetTaxSummaryAsync(Guid companyId, string returnPeriod);
    }
}

using Core.Common;
using Core.Entities.Forex;

namespace Application.Interfaces.Forex
{
    /// <summary>
    /// Service interface for Forex operations following Ind AS 21
    /// Handles forex transaction recording, gain/loss calculation, and revaluation
    /// </summary>
    public interface IForexService
    {
        // Transaction Management
        Task<Result<ForexTransaction>> RecordBookingAsync(ForexBookingRequest request);
        Task<Result<ForexTransaction>> RecordSettlementAsync(ForexSettlementRequest request);
        Task<Result<IEnumerable<ForexTransaction>>> RecordRevaluationAsync(ForexRevaluationRequest request);

        // Gain/Loss Calculation
        Task<Result<ForexGainLoss>> CalculateRealizedGainLossAsync(Guid bookingTransactionId, decimal settlementRate);
        Task<Result<ForexGainLossSummary>> GetGainLossSummaryAsync(Guid companyId, string financialYear);

        // Invoice Forex Operations
        Task<Result<decimal>> GetInvoiceExchangeRateAsync(Guid invoiceId);
        Task<Result> UpdateInvoiceForexFieldsAsync(Guid invoiceId, decimal exchangeRate);

        // Outstanding Receivables
        Task<Result<IEnumerable<OutstandingReceivable>>> GetOutstandingReceivablesAsync(Guid companyId, string currency);
        Task<Result<IEnumerable<ForexTransaction>>> GetBookingsForSettlementAsync(Guid companyId, string currency, decimal? amount = null);
    }

    /// <summary>
    /// Request to record a forex booking (when invoice is created)
    /// </summary>
    public class ForexBookingRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string SourceType { get; set; } = "invoice";
        public Guid SourceId { get; set; }
        public string SourceNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request to record a forex settlement (when payment is received)
    /// </summary>
    public class ForexSettlementRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public Guid BookingTransactionId { get; set; }
        public string SourceType { get; set; } = "payment";
        public Guid SourceId { get; set; }
        public string SourceNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public decimal SettlementRate { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// Request for month-end forex revaluation
    /// </summary>
    public class ForexRevaluationRequest
    {
        public Guid CompanyId { get; set; }
        public DateOnly AsOfDate { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal RevaluationRate { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents calculated forex gain/loss
    /// </summary>
    public class ForexGainLoss
    {
        public decimal BookingRate { get; set; }
        public decimal SettlementRate { get; set; }
        public decimal ForeignAmount { get; set; }
        public decimal BookingInrAmount { get; set; }
        public decimal SettlementInrAmount { get; set; }
        public decimal GainLossAmount { get; set; }
        public bool IsGain => GainLossAmount > 0;
        public string GainLossType { get; set; } = "realized";
    }

    /// <summary>
    /// Summary of forex gains and losses for a period
    /// </summary>
    public class ForexGainLossSummary
    {
        public Guid CompanyId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public decimal TotalRealizedGain { get; set; }
        public decimal TotalRealizedLoss { get; set; }
        public decimal NetRealizedGainLoss => TotalRealizedGain - TotalRealizedLoss;
        public decimal TotalUnrealizedGain { get; set; }
        public decimal TotalUnrealizedLoss { get; set; }
        public decimal NetUnrealizedGainLoss => TotalUnrealizedGain - TotalUnrealizedLoss;
        public decimal TotalGainLoss => NetRealizedGainLoss + NetUnrealizedGainLoss;
    }

    /// <summary>
    /// Represents an outstanding foreign currency receivable
    /// </summary>
    public class OutstandingReceivable
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal ForeignAmount { get; set; }
        public decimal BookingRate { get; set; }
        public decimal BookingInrAmount { get; set; }
        public decimal? CurrentRate { get; set; }
        public decimal? CurrentInrAmount { get; set; }
        public decimal? UnrealizedGainLoss { get; set; }
        public int DaysOutstanding { get; set; }
    }
}

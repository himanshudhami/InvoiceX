using Application.Interfaces.Forex;
using Core.Common;
using Core.Entities.Forex;
using Core.Interfaces.Forex;
using Core.Interfaces;

namespace Application.Services.Forex
{
    /// <summary>
    /// Service for Forex operations following Ind AS 21 (Effects of Changes in Foreign Exchange Rates)
    /// Handles forex transaction recording, gain/loss calculation, and revaluation
    /// </summary>
    public class ForexService : IForexService
    {
        private readonly IForexTransactionRepository _forexRepository;
        private readonly IInvoicesRepository _invoicesRepository;

        public ForexService(
            IForexTransactionRepository forexRepository,
            IInvoicesRepository invoicesRepository)
        {
            _forexRepository = forexRepository ?? throw new ArgumentNullException(nameof(forexRepository));
            _invoicesRepository = invoicesRepository ?? throw new ArgumentNullException(nameof(invoicesRepository));
        }

        /// <inheritdoc />
        public async Task<Result<ForexTransaction>> RecordBookingAsync(ForexBookingRequest request)
        {
            if (request.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(request.Currency))
                return Error.Validation("Currency is required");

            if (request.ExchangeRate <= 0)
                return Error.Validation("Exchange rate must be positive");

            var inrAmount = request.ForeignAmount * request.ExchangeRate;

            var transaction = new ForexTransaction
            {
                CompanyId = request.CompanyId,
                TransactionDate = request.TransactionDate,
                FinancialYear = request.FinancialYear,
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                SourceNumber = request.SourceNumber,
                Currency = request.Currency,
                ForeignAmount = request.ForeignAmount,
                ExchangeRate = request.ExchangeRate,
                InrAmount = inrAmount,
                TransactionType = "booking",
                IsPosted = false,
                CreatedBy = request.CreatedBy
            };

            var created = await _forexRepository.AddAsync(transaction);
            return Result<ForexTransaction>.Success(created);
        }

        /// <inheritdoc />
        public async Task<Result<ForexTransaction>> RecordSettlementAsync(ForexSettlementRequest request)
        {
            if (request.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (request.BookingTransactionId == Guid.Empty)
                return Error.Validation("Booking transaction ID is required");

            // Get the original booking transaction
            var bookingTxn = await _forexRepository.GetByIdAsync(request.BookingTransactionId);
            if (bookingTxn == null)
                return Error.NotFound("Original booking transaction not found");

            // Calculate gain/loss
            var gainLossResult = await CalculateRealizedGainLossAsync(
                request.BookingTransactionId,
                request.SettlementRate);

            if (gainLossResult.IsFailure)
                return gainLossResult.Error!;

            var gainLoss = gainLossResult.Value!;
            var settlementInrAmount = request.ForeignAmount * request.SettlementRate;

            var transaction = new ForexTransaction
            {
                CompanyId = request.CompanyId,
                TransactionDate = request.TransactionDate,
                FinancialYear = request.FinancialYear,
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                SourceNumber = request.SourceNumber,
                Currency = request.Currency,
                ForeignAmount = request.ForeignAmount,
                ExchangeRate = request.SettlementRate,
                InrAmount = settlementInrAmount,
                TransactionType = "settlement",
                ForexGainLoss = gainLoss.GainLossAmount,
                GainLossType = "realized",
                RelatedForexId = request.BookingTransactionId,
                IsPosted = false,
                CreatedBy = request.CreatedBy
            };

            var created = await _forexRepository.AddAsync(transaction);
            return Result<ForexTransaction>.Success(created);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ForexTransaction>>> RecordRevaluationAsync(ForexRevaluationRequest request)
        {
            if (request.CompanyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (request.RevaluationRate <= 0)
                return Error.Validation("Revaluation rate must be positive");

            // Get all outstanding bookings for the currency
            var outstandingBookings = await _forexRepository.GetOutstandingBookingsAsync(
                request.CompanyId,
                request.Currency,
                request.AsOfDate);

            var revaluationTxns = new List<ForexTransaction>();

            foreach (var booking in outstandingBookings)
            {
                // Calculate unrealized gain/loss
                var currentInr = booking.ForeignAmount * request.RevaluationRate;
                var unrealizedGainLoss = currentInr - booking.InrAmount;

                // Only create revaluation entry if there's a material difference
                if (Math.Abs(unrealizedGainLoss) < 0.01m)
                    continue;

                var revalTxn = new ForexTransaction
                {
                    CompanyId = request.CompanyId,
                    TransactionDate = request.AsOfDate,
                    FinancialYear = request.FinancialYear,
                    SourceType = "revaluation",
                    SourceId = booking.Id,
                    SourceNumber = $"REVAL-{request.AsOfDate:yyyyMMdd}-{booking.SourceNumber}",
                    Currency = request.Currency,
                    ForeignAmount = booking.ForeignAmount,
                    ExchangeRate = request.RevaluationRate,
                    InrAmount = currentInr,
                    TransactionType = "revaluation",
                    ForexGainLoss = unrealizedGainLoss,
                    GainLossType = "unrealized",
                    RelatedForexId = booking.Id,
                    IsPosted = false,
                    CreatedBy = request.CreatedBy
                };

                var created = await _forexRepository.AddAsync(revalTxn);
                revaluationTxns.Add(created);
            }

            return Result<IEnumerable<ForexTransaction>>.Success(revaluationTxns);
        }

        /// <inheritdoc />
        public async Task<Result<ForexGainLoss>> CalculateRealizedGainLossAsync(
            Guid bookingTransactionId,
            decimal settlementRate)
        {
            var booking = await _forexRepository.GetByIdAsync(bookingTransactionId);
            if (booking == null)
                return Error.NotFound("Booking transaction not found");

            if (booking.TransactionType != "booking")
                return Error.Validation("Transaction is not a booking");

            var bookingInr = booking.InrAmount;
            var settlementInr = booking.ForeignAmount * settlementRate;
            var gainLoss = settlementInr - bookingInr;

            return Result<ForexGainLoss>.Success(new ForexGainLoss
            {
                BookingRate = booking.ExchangeRate,
                SettlementRate = settlementRate,
                ForeignAmount = booking.ForeignAmount,
                BookingInrAmount = bookingInr,
                SettlementInrAmount = settlementInr,
                GainLossAmount = gainLoss,
                GainLossType = "realized"
            });
        }

        /// <inheritdoc />
        public async Task<Result<ForexGainLossSummary>> GetGainLossSummaryAsync(
            Guid companyId,
            string financialYear)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var (realizedGain, unrealizedGain) = await _forexRepository.GetGainLossSummaryAsync(
                companyId, financialYear);

            // Split into gains and losses
            var summary = new ForexGainLossSummary
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                TotalRealizedGain = realizedGain > 0 ? realizedGain : 0,
                TotalRealizedLoss = realizedGain < 0 ? Math.Abs(realizedGain) : 0,
                TotalUnrealizedGain = unrealizedGain > 0 ? unrealizedGain : 0,
                TotalUnrealizedLoss = unrealizedGain < 0 ? Math.Abs(unrealizedGain) : 0
            };

            return Result<ForexGainLossSummary>.Success(summary);
        }

        /// <inheritdoc />
        public async Task<Result<decimal>> GetInvoiceExchangeRateAsync(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return Error.Validation("Invoice ID is required");

            var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound("Invoice not found");

            if (invoice.InvoiceExchangeRate.HasValue)
                return Result<decimal>.Success(invoice.InvoiceExchangeRate.Value);

            // If exchange rate not set, try to get from forex transaction
            var forexTxn = await _forexRepository.GetBySourceAsync("invoice", invoiceId);
            if (forexTxn != null)
                return Result<decimal>.Success(forexTxn.ExchangeRate);

            return Error.NotFound("Exchange rate not found for this invoice");
        }

        /// <inheritdoc />
        public async Task<Result> UpdateInvoiceForexFieldsAsync(Guid invoiceId, decimal exchangeRate)
        {
            if (invoiceId == Guid.Empty)
                return Error.Validation("Invoice ID is required");

            if (exchangeRate <= 0)
                return Error.Validation("Exchange rate must be positive");

            var invoice = await _invoicesRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
                return Error.NotFound("Invoice not found");

            // Calculate INR amount
            var foreignAmount = invoice.ForeignCurrencyAmount ?? invoice.TotalAmount;
            var inrAmount = foreignAmount * exchangeRate;

            invoice.InvoiceExchangeRate = exchangeRate;
            invoice.InvoiceAmountInr = inrAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _invoicesRepository.UpdateAsync(invoice);
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<OutstandingReceivable>>> GetOutstandingReceivablesAsync(
            Guid companyId,
            string currency)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(currency))
                return Error.Validation("Currency is required");

            var bookings = await _forexRepository.GetOutstandingBookingsAsync(
                companyId,
                currency,
                DateOnly.FromDateTime(DateTime.UtcNow));

            var receivables = new List<OutstandingReceivable>();

            foreach (var booking in bookings)
            {
                if (booking.SourceType != "invoice" || !booking.SourceId.HasValue)
                    continue;

                var invoice = await _invoicesRepository.GetByIdAsync(booking.SourceId.Value);
                if (invoice == null)
                    continue;

                var daysOutstanding = (DateTime.UtcNow.Date - booking.TransactionDate.ToDateTime(TimeOnly.MinValue)).Days;

                receivables.Add(new OutstandingReceivable
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    CustomerId = invoice.CustomerId,
                    Currency = booking.Currency,
                    ForeignAmount = booking.ForeignAmount,
                    BookingRate = booking.ExchangeRate,
                    BookingInrAmount = booking.InrAmount,
                    DaysOutstanding = daysOutstanding
                });
            }

            return Result<IEnumerable<OutstandingReceivable>>.Success(receivables);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<ForexTransaction>>> GetBookingsForSettlementAsync(
            Guid companyId,
            string currency,
            decimal? amount = null)
        {
            if (companyId == Guid.Empty)
                return Error.Validation("Company ID is required");

            if (string.IsNullOrWhiteSpace(currency))
                return Error.Validation("Currency is required");

            var bookings = await _forexRepository.GetBookingsForSettlementAsync(
                companyId, currency, amount);

            return Result<IEnumerable<ForexTransaction>>.Success(bookings);
        }
    }
}

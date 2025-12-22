using Application.Interfaces;
using Application.DTOs.BankTransactions;
using Core.Common;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs.Common;

namespace WebApi.Controllers
{
    /// <summary>
    /// Unified view of outgoing payments for reconciliation.
    /// Aggregates salary, contractor, expense claims, subscriptions, loans, and asset maintenance.
    /// </summary>
    [ApiController]
    [Route("api/outgoing-payments")]
    [Produces("application/json")]
    public class OutgoingPaymentsController : ControllerBase
    {
        private readonly IOutgoingPaymentsService _service;

        public OutgoingPaymentsController(IOutgoingPaymentsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("{companyId}")]
        [ProducesResponseType(typeof(PagedResponse<OutgoingPaymentDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetOutgoingPayments(
            Guid companyId,
            [FromQuery] OutgoingPaymentsFilterRequest request)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetOutgoingPaymentsAsync(
                companyId,
                request.PageNumber,
                request.PageSize,
                request.Reconciled,
                request.Types?.Split(',').ToList(),
                request.FromDate,
                request.ToDate);

            if (result.IsFailure) return HandleError(result.Error!);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<OutgoingPaymentDto>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpGet("{companyId}/to-reconcile")]
        [ProducesResponseType(typeof(PagedResponse<OutgoingPaymentDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetToReconcile(
            Guid companyId,
            [FromQuery] OutgoingPaymentsFilterRequest request)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetOutgoingPaymentsAsync(
                companyId,
                request.PageNumber,
                request.PageSize,
                reconciled: false,
                request.Types?.Split(',').ToList(),
                request.FromDate,
                request.ToDate);

            if (result.IsFailure) return HandleError(result.Error!);

            var (items, totalCount) = result.Value;
            return Ok(new PagedResponse<OutgoingPaymentDto>(items, totalCount, request.PageNumber, request.PageSize));
        }

        [HttpGet("{companyId}/summary")]
        [ProducesResponseType(typeof(OutgoingPaymentsSummaryDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetSummary(
            Guid companyId,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null)
        {
            if (companyId == Guid.Empty)
                return BadRequest("Company ID cannot be empty");

            var result = await _service.GetOutgoingPaymentsSummaryAsync(companyId, fromDate, toDate);
            if (result.IsFailure) return HandleError(result.Error!);
            return Ok(result.Value);
        }

        private IActionResult HandleError(Error error) => error.Type switch
        {
            ErrorType.Validation => BadRequest(error.Message),
            ErrorType.NotFound => NotFound(error.Message),
            ErrorType.Internal => StatusCode(500, error.Message),
            _ => BadRequest(error.Message)
        };
    }

    public class OutgoingPaymentsFilterRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? Reconciled { get; set; }
        public string? Types { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}

namespace Application.DTOs.PaymentAllocations
{
    /// <summary>
    /// DTO for creating a payment allocation
    /// </summary>
    public class CreatePaymentAllocationDto
    {
        public Guid CompanyId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid? InvoiceId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal? AmountInInr { get; set; }
        public decimal ExchangeRate { get; set; } = 1;
        public DateOnly? AllocationDate { get; set; }
        public string AllocationType { get; set; } = "invoice_settlement";
        public decimal TdsAllocated { get; set; }
        public string? Notes { get; set; }
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// DTO for updating a payment allocation
    /// </summary>
    public class UpdatePaymentAllocationDto
    {
        public Guid? InvoiceId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal? AmountInInr { get; set; }
        public decimal ExchangeRate { get; set; } = 1;
        public DateOnly? AllocationDate { get; set; }
        public string AllocationType { get; set; } = "invoice_settlement";
        public decimal TdsAllocated { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for bulk allocation request
    /// </summary>
    public class BulkAllocationDto
    {
        public Guid CompanyId { get; set; }
        public Guid PaymentId { get; set; }
        public List<AllocationItemDto> Allocations { get; set; } = new();
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// Single allocation item in bulk request
    /// </summary>
    public class AllocationItemDto
    {
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public decimal TdsAmount { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Invoice payment status result
    /// </summary>
    public class InvoicePaymentStatusDto
    {
        public Guid InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal InvoiceTotal { get; set; }
        public string Currency { get; set; } = "INR";
        public decimal TotalPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string Status { get; set; } = "unpaid";
        public int PaymentCount { get; set; }
        public DateOnly? LastPaymentDate { get; set; }
    }

    /// <summary>
    /// Payment allocation summary (for display)
    /// </summary>
    public class PaymentAllocationSummaryDto
    {
        public Guid PaymentId { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal Unallocated { get; set; }
        public int AllocationCount { get; set; }
        public List<AllocationDetailDto> Allocations { get; set; } = new();
    }

    /// <summary>
    /// Single allocation detail
    /// </summary>
    public class AllocationDetailDto
    {
        public Guid Id { get; set; }
        public Guid? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal TdsAllocated { get; set; }
        public DateOnly AllocationDate { get; set; }
        public string AllocationType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}

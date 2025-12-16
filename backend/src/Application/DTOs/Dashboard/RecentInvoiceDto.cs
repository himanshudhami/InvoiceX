namespace Application.DTOs.Dashboard
{
    public class RecentInvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DueDate { get; set; }
        public int? DaysOverdue { get; set; }
    }
}
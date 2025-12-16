using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.BankAccounts
{
    /// <summary>
    /// Data transfer object for updating a bank account balance
    /// </summary>
    public class UpdateBalanceDto
    {
        /// <summary>
        /// New balance amount
        /// </summary>
        [Required(ErrorMessage = "New balance is required")]
        public decimal NewBalance { get; set; }

        /// <summary>
        /// Date of the balance snapshot
        /// </summary>
        [Required(ErrorMessage = "As of date is required")]
        public DateOnly AsOfDate { get; set; }
    }
}

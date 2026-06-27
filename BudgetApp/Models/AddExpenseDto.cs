using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Models
{
    public class AddExpenseDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }
}

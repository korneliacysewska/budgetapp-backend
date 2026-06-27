namespace BudgetApp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int UserId { get; set; }




        public Expense(decimal amount, string category)
        {
            Amount = amount;
            Category = category;
        }
        public Expense() { }
    }
}
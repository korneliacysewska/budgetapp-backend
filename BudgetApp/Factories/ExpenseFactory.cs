using BudgetApp.Models;

namespace BudgetApp.Factories
{
    public static class ExpenseFactory
    {
        public static Expense Create(string category, decimal amount)
        {
            switch (category.ToLower())
            {
                case "jedzenie":
                    return new Expense(amount, "Jedzenie");

                case "transport":
                    return new Expense(amount, "Transport");

                case "rozrywka":
                    return new Expense(amount, "Rozrywka");

                case "zdrowie":
                    return new Expense(amount, "Zdrowie");

                case "ubrania":
                    return new Expense(amount, "Ubrania");

                case "inne":
                default:
                    return new Expense(amount, "Inne");
            }
        }
    }
}

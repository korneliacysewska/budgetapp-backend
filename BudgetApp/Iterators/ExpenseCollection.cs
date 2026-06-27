using System.Collections;
using BudgetApp.Models;

namespace BudgetApp.Iterators
{
    public class ExpenseCollection : IEnumerable<Expense>
    {
        private readonly List<Expense> _expenses = new();

        public void Add(Expense expense)
        {
            _expenses.Add(expense);
        }

        public IEnumerator<Expense> GetEnumerator()
        {
            return new ExpenseIterator(_expenses);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
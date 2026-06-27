using System.Collections;
using BudgetApp.Models;

namespace BudgetApp.Iterators
{
    public class ExpenseIterator : IEnumerator<Expense>
    {
        private readonly List<Expense> _expenses;
        private int _position = -1;

        public ExpenseIterator(List<Expense> expenses)
        {
            _expenses = expenses;
        }

        public Expense Current => _expenses[_position];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _position++;
            return _position < _expenses.Count;
        }

        public void Reset()
        {
            _position = -1;
        }

        public void Dispose() { }
    }
}
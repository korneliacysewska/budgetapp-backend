using Microsoft.AspNetCore.Mvc;
using BudgetApp.Models;
using BudgetApp.Data;
using BudgetApp.Factories;
using BudgetApp.Iterators;
using BudgetApp.Reflection;
using Microsoft.Extensions.Caching.Distributed;
using System;
using Newtonsoft.Json;

namespace BudgetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly BudgetContext _context;
        private readonly IDistributedCache _cache;
        public ExpenseController(BudgetContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        
        [HttpGet("user/{userId}")]
        public IActionResult GetUserExpenses(int userId)
        {
            var expensesFromDb = _context.Expenses
                .Where(e => e.UserId == userId)
                .ToList();

            var collection = new ExpenseCollection();

            foreach (var e in expensesFromDb)
                collection.Add(e);

            var result = new List<Expense>();
            foreach (var e in collection) 
                result.Add(e);

            return Ok(result);
        }

        
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.Expenses.ToList());
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddExpense(AddExpenseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var expense = ExpenseFactory.Create(dto.Category, dto.Amount);

            ReflectionMapper.Map(expense, dto);

            _context.Expenses.Add(expense);
            _context.SaveChanges();

            
            await _cache.RemoveAsync($"piechart:{expense.UserId}");
            await _cache.RemoveAsync($"barchart:{expense.UserId}");
            await _cache.RemoveAsync($"dashboard:{expense.UserId}");

            return Ok(expense);
        }


        [HttpGet("user/{userId}/summary")]
        public IActionResult GetCategorySummary(int userId)
        {
            var expenses = _context.Expenses
                .Where(e => e.UserId == userId)
                .ToList();

            var summary = expenses
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Amount)
                })
                .ToList();

            return Ok(summary);
        }


        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(int userId)
        {
            var cacheKey = $"dashboard:{userId}";

            
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return Ok(JsonConvert.DeserializeObject(cached));

            
            var total = _context.Expenses
                .Where(e => e.UserId == userId)
                .Sum(e => e.Amount);

            var lastExpenses = _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .Take(5)
                .ToList();

            var topCategory = _context.Expenses
                .Where(e => e.UserId == userId)
                .GroupBy(e => e.Category)
                .Select(g => new {
                    Category = g.Key,
                    Amount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .FirstOrDefault();

            var dashboard = new
            {
                Total = total,
                LastExpenses = lastExpenses,
                TopCategory = topCategory
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonConvert.SerializeObject(dashboard),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                }
            );

            return Ok(dashboard);
        }


        [HttpGet("user/{userId}/piechart")]
        public IActionResult GetPieChartData(int userId)
        {
            var now = DateTime.Now;
            var monthAgo = now.AddMonths(-1);

            var expenses = _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= monthAgo)
                .ToList();

            var total = expenses.Sum(e => e.Amount);

            var colors = new Dictionary<string, string>
    {
            { "Jedzenie", "#FF6384" },
            { "Transport", "#36A2EB" },
            { "Rozrywka", "#FFCE56" },
            { "Zdrowie", "#4BC0C0" },
            { "Ubrania", "#9966FF" },
            { "Inne", "#C9CBCF" }
    };

            var data = expenses
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Amount),
                    Percentage = Math.Round((g.Sum(x => x.Amount) / total) * 100, 2),
                    Color = colors[g.Key]
                })
                .ToList();

            return Ok(new
            {
                TotalSpent = total,
                Data = data
            });
        }


        [HttpGet("user/{userId}/barchart")]
        public IActionResult GetBarChartData(int userId)
        {
            var now = DateTime.Now;
            var sixMonthsAgo = now.AddMonths(-6);

            var expenses = _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= sixMonthsAgo)
                .ToList();

            var colors = new Dictionary<string, string>
    {
        { "Jedzenie", "#FF6384" },
        { "Transport", "#36A2EB" },
        { "Rozrywka", "#FFCE56" },
        { "Zdrowie", "#4BC0C0" },
        { "Ubrania", "#9966FF" },
        { "Inne", "#C9CBCF" }
    };

            var grouped = expenses
                .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Category })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Category = g.Key.Category,
                    Total = g.Sum(x => x.Amount)
                })
                .ToList();

            var months = Enumerable.Range(0, 6)
                .Select(i => now.AddMonths(-i))
                .OrderBy(d => d)
                .Select(d => new { d.Year, d.Month })
                .ToList();

            var result = months.Select(m => new
            {
                Year = m.Year,
                Month = m.Month,
                Categories = grouped
                    .Where(g => g.Year == m.Year && g.Month == m.Month)
                    .Select(g => new
                    {
                        g.Category,
                        g.Total,
                        Color = colors[g.Category]
                    })
                    .ToList()
            });

            return Ok(result);
        }
    }
}
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ExpenseService _expenseService;

    public DashboardStats Stats { get; set; } = new();

    public IndexModel(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var expenses = await _expenseService.GetExpensesAsync();

        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }

        Stats = new DashboardStats
        {
            TotalExpenses      = expenses.Count,
            DraftCount         = expenses.Count(e => e.StatusName == "Draft"),
            SubmittedCount     = expenses.Count(e => e.StatusName == "Submitted"),
            ApprovedCount      = expenses.Count(e => e.StatusName == "Approved"),
            RejectedCount      = expenses.Count(e => e.StatusName == "Rejected"),
            TotalApprovedAmount = expenses.Where(e => e.StatusName == "Approved").Sum(e => e.AmountDecimal),
            RecentExpenses     = expenses.Take(5).ToList()
        };
    }
}

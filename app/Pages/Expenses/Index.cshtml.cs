using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class IndexModel : PageModel
{
    private readonly ExpenseService _expenseService;

    public List<Expense> Expenses { get; set; } = new();
    public List<Models.ExpenseStatus> Statuses { get; set; } = new();
    public List<Models.ExpenseCategory> Categories { get; set; } = new();
    public List<Models.User> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UserFilter { get; set; }

    public IndexModel(ExpenseService expenseService) => _expenseService = expenseService;

    public async Task OnGetAsync()
    {
        var allExpenses = StatusFilter.HasValue
            ? await _expenseService.GetExpensesByStatusAsync(StatusFilter.Value)
            : UserFilter.HasValue
                ? await _expenseService.GetExpensesByUserAsync(UserFilter.Value)
                : await _expenseService.GetExpensesAsync();

        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }

        Expenses = CategoryFilter.HasValue
            ? allExpenses.Where(e => e.CategoryId == CategoryFilter.Value).ToList()
            : allExpenses;

        Statuses   = await _expenseService.GetStatusesAsync();
        Categories = await _expenseService.GetCategoriesAsync();
        Users      = await _expenseService.GetUsersAsync();
    }
}

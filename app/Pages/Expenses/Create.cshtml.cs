using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly ExpenseService _expenseService;

    [BindProperty]
    public CreateExpenseRequest Input { get; set; } = new()
    {
        UserId      = 1,
        CategoryId  = 1,
        AmountMinor = 0,
        Currency    = "GBP",
        ExpenseDate = DateTime.UtcNow.Date
    };

    public List<Models.ExpenseCategory> Categories { get; set; } = new();
    public List<Models.User> Users { get; set; } = new();

    public CreateModel(ExpenseService expenseService) => _expenseService = expenseService;

    public async Task OnGetAsync()
    {
        Categories = await _expenseService.GetCategoriesAsync();
        Users      = await _expenseService.GetUsersAsync();
        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Categories = await _expenseService.GetCategoriesAsync();
            Users      = await _expenseService.GetUsersAsync();
            return Page();
        }

        var id = await _expenseService.CreateExpenseAsync(Input);
        if (id < 0)
        {
            ModelState.AddModelError("", "Failed to create expense. Please try again.");
            Categories = await _expenseService.GetCategoriesAsync();
            Users      = await _expenseService.GetUsersAsync();
            return Page();
        }

        return RedirectToPage("/Expenses/Details", new { id });
    }
}

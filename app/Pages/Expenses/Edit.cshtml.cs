using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class EditModel : PageModel
{
    private readonly ExpenseService _expenseService;

    [BindProperty]
    public UpdateExpenseRequest Input { get; set; } = new();

    public Expense? Expense { get; set; }
    public List<Models.ExpenseCategory> Categories { get; set; } = new();

    public EditModel(ExpenseService expenseService) => _expenseService = expenseService;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Expense    = await _expenseService.GetExpenseByIdAsync(id);
        Categories = await _expenseService.GetCategoriesAsync();
        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }
        if (Expense == null) return NotFound();

        Input = new UpdateExpenseRequest
        {
            CategoryId  = Expense.CategoryId,
            AmountMinor = Expense.AmountMinor,
            Currency    = Expense.Currency,
            ExpenseDate = Expense.ExpenseDate,
            Description = Expense.Description,
            ReceiptFile = Expense.ReceiptFile
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            Expense    = await _expenseService.GetExpenseByIdAsync(id);
            Categories = await _expenseService.GetCategoriesAsync();
            return Page();
        }

        var ok = await _expenseService.UpdateExpenseAsync(id, Input);
        if (!ok)
        {
            ModelState.AddModelError("", "Failed to update expense. Please try again.");
            Expense    = await _expenseService.GetExpenseByIdAsync(id);
            Categories = await _expenseService.GetCategoriesAsync();
            return Page();
        }

        return RedirectToPage("/Expenses/Details", new { id });
    }
}

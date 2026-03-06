using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Expenses;

public class DetailsModel : PageModel
{
    private readonly ExpenseService _expenseService;

    public Expense? Expense { get; set; }
    public List<Models.User> Users { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public DetailsModel(ExpenseService expenseService) => _expenseService = expenseService;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Expense = await _expenseService.GetExpenseByIdAsync(id);
        Users   = await _expenseService.GetUsersAsync();
        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }
        if (Expense == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var ok = await _expenseService.SubmitExpenseAsync(id);
        TempData["SuccessMessage"] = ok ? "Expense submitted for approval." : "Failed to submit expense.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, int reviewedBy)
    {
        var ok = await _expenseService.ApproveExpenseAsync(id, reviewedBy);
        TempData["SuccessMessage"] = ok ? "Expense approved." : "Failed to approve expense.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, int reviewedBy)
    {
        var ok = await _expenseService.RejectExpenseAsync(id, reviewedBy);
        TempData["SuccessMessage"] = ok ? "Expense rejected." : "Failed to reject expense.";
        return RedirectToPage(new { id });
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages.Manage;

public class UsersModel : PageModel
{
    private readonly ExpenseService _expenseService;

    public List<User> Users { get; set; } = new();

    public UsersModel(ExpenseService expenseService) => _expenseService = expenseService;

    public async Task OnGetAsync()
    {
        Users = await _expenseService.GetUsersAsync();
        if (_expenseService.LastError != null)
        {
            ViewData["DbError"] = _expenseService.LastError;
            ViewData["DbErrorFile"] = Path.GetFileName(_expenseService.LastErrorFile ?? "");
            ViewData["DbErrorLine"] = _expenseService.LastErrorLine;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

/// <summary>Users API</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    public UsersController(ExpenseService expenseService) => _expenseService = expenseService;

    /// <summary>Get all users</summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
        => Ok(await _expenseService.GetUsersAsync());
}

/// <summary>Categories API</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    public CategoriesController(ExpenseService expenseService) => _expenseService = expenseService;

    /// <summary>Get all expense categories</summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetAll()
        => Ok(await _expenseService.GetCategoriesAsync());
}

/// <summary>Statuses API</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly ExpenseService _expenseService;
    public StatusesController(ExpenseService expenseService) => _expenseService = expenseService;

    /// <summary>Get all expense statuses</summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetAll()
        => Ok(await _expenseService.GetStatusesAsync());
}

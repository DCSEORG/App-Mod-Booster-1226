using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

/// <summary>
/// Expenses API - CRUD operations for expense management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpensesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>Get all expenses</summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAll()
    {
        var expenses = await _expenseService.GetExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>Get expense by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    /// <summary>Get expenses by user</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<Expense>>> GetByUser(int userId)
    {
        var expenses = await _expenseService.GetExpensesByUserAsync(userId);
        return Ok(expenses);
    }

    /// <summary>Get expenses by status</summary>
    [HttpGet("status/{statusId:int}")]
    public async Task<ActionResult<List<Expense>>> GetByStatus(int statusId)
    {
        var expenses = await _expenseService.GetExpensesByStatusAsync(statusId);
        return Ok(expenses);
    }

    /// <summary>Create a new expense</summary>
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateExpenseRequest request)
    {
        var id = await _expenseService.CreateExpenseAsync(request);
        if (id < 0) return StatusCode(500, new { error = "Failed to create expense" });
        return CreatedAtAction(nameof(GetById), new { id }, new { expenseId = id });
    }

    /// <summary>Update an existing expense</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request)
    {
        var success = await _expenseService.UpdateExpenseAsync(id, request);
        if (!success) return StatusCode(500, new { error = "Failed to update expense" });
        return NoContent();
    }

    /// <summary>Submit a draft expense for approval</summary>
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var success = await _expenseService.SubmitExpenseAsync(id);
        if (!success) return StatusCode(500, new { error = "Failed to submit expense" });
        return Ok(new { message = "Expense submitted successfully" });
    }

    /// <summary>Approve a submitted expense</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ReviewExpenseRequest request)
    {
        var success = await _expenseService.ApproveExpenseAsync(id, request.ReviewedBy);
        if (!success) return StatusCode(500, new { error = "Failed to approve expense" });
        return Ok(new { message = "Expense approved successfully" });
    }

    /// <summary>Reject a submitted expense</summary>
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReviewExpenseRequest request)
    {
        var success = await _expenseService.RejectExpenseAsync(id, request.ReviewedBy);
        if (!success) return StatusCode(500, new { error = "Failed to reject expense" });
        return Ok(new { message = "Expense rejected successfully" });
    }
}

namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public decimal AmountDecimal { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
}

public class UpdateExpenseRequest
{
    public int CategoryId { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
}

public class ReviewExpenseRequest
{
    public int ReviewedBy { get; set; }
}

public class DashboardStats
{
    public int TotalExpenses { get; set; }
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public List<Expense> RecentExpenses { get; set; } = new();
}

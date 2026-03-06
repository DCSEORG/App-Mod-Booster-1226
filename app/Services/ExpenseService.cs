using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;
using System.Runtime.CompilerServices;

namespace ExpenseManagement.Services;

/// <summary>
/// Provides data access for the Expense Management System via stored procedures.
/// Returns dummy data if the database connection fails, and surfaces error details.
/// </summary>
public class ExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;

    public string? LastError { get; private set; }
    public string? LastErrorFile { get; private set; }
    public int LastErrorLine { get; private set; }

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        return new SqlConnection(connectionString);
    }

    private void SetError(Exception ex, string file, int line)
    {
        LastError = BuildErrorMessage(ex);
        LastErrorFile = file;
        LastErrorLine = line;
        _logger.LogError(ex, "Database error in {File}:{Line}", file, line);
    }

    private static string BuildErrorMessage(Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Active Directory", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            msg += " | MANAGED IDENTITY FIX: The Managed Identity needs db_datareader, db_datawriter roles and EXECUTE permission. " +
                   "Run: python3 run-sql-dbrole.py  — or ensure AZURE_CLIENT_ID is set in App Service configuration.";
        }
        return msg;
    }

    private void ClearError() => LastError = null;

    // ── Expenses ─────────────────────────────────────────────────────────────

    public async Task<List<Expense>> GetExpensesAsync(
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetExpenses", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            return await ReadExpensesAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetExpenseById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            var list = await ReadExpensesAsync(cmd);
            return list.FirstOrDefault();
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetExpensesByUser", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);
            return await ReadExpensesAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyExpenses().Where(e => e.UserId == userId).ToList();
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(int statusId,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetExpensesByStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StatusId", statusId);
            return await ReadExpensesAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyExpenses().Where(e => e.StatusId == statusId).ToList();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest req,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_CreateExpense", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@CategoryId", req.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", req.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency", req.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)req.ReceiptFile ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return -1;
        }
    }

    public async Task<bool> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest req,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_UpdateExpense", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@CategoryId", req.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", req.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency", req.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)req.ReceiptFile ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return false;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_SubmitExpense", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewedBy,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_ApproveExpense", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return false;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewedBy,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_RejectExpense", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return false;
        }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public async Task<List<User>> GetUsersAsync(
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetUsers", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            return await ReadUsersAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyUsers();
        }
    }

    // ── Categories ────────────────────────────────────────────────────────────

    public async Task<List<ExpenseCategory>> GetCategoriesAsync(
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetCategories", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            return await ReadCategoriesAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyCategories();
        }
    }

    // ── Statuses ──────────────────────────────────────────────────────────────

    public async Task<List<Models.ExpenseStatus>> GetStatusesAsync(
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ClearError();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.usp_GetStatuses", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            return await ReadStatusesAsync(cmd);
        }
        catch (Exception ex)
        {
            SetError(ex, file, line);
            return GetDummyStatuses();
        }
    }

    // ── Private Readers ───────────────────────────────────────────────────────

    private static async Task<List<Expense>> ReadExpensesAsync(SqlCommand cmd)
    {
        var list = new List<Expense>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Expense
            {
                ExpenseId      = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
                UserId         = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName       = reader.GetString(reader.GetOrdinal("UserName")),
                CategoryId     = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName   = reader.GetString(reader.GetOrdinal("CategoryName")),
                StatusId       = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName     = reader.GetString(reader.GetOrdinal("StatusName")),
                AmountMinor    = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
                Currency       = reader.GetString(reader.GetOrdinal("Currency")),
                AmountDecimal  = reader.GetDecimal(reader.GetOrdinal("AmountDecimal")),
                ExpenseDate    = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                Description    = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                ReceiptFile    = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
                SubmittedAt    = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
                ReviewedBy     = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
                ReviewedByName = reader.IsDBNull(reader.GetOrdinal("ReviewedByName")) ? null : reader.GetString(reader.GetOrdinal("ReviewedByName")),
                ReviewedAt     = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
                CreatedAt      = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }
        return list;
    }

    private static async Task<List<User>> ReadUsersAsync(SqlCommand cmd)
    {
        var list = new List<User>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new User
            {
                UserId      = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName    = reader.GetString(reader.GetOrdinal("UserName")),
                Email       = reader.GetString(reader.GetOrdinal("Email")),
                RoleId      = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName    = reader.GetString(reader.GetOrdinal("RoleName")),
                ManagerId   = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
                IsActive    = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt   = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }
        return list;
    }

    private static async Task<List<ExpenseCategory>> ReadCategoriesAsync(SqlCommand cmd)
    {
        var list = new List<ExpenseCategory>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ExpenseCategory
            {
                CategoryId   = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                IsActive     = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }
        return list;
    }

    private static async Task<List<Models.ExpenseStatus>> ReadStatusesAsync(SqlCommand cmd)
    {
        var list = new List<Models.ExpenseStatus>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Models.ExpenseStatus
            {
                StatusId   = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
            });
        }
        return list;
    }

    // ── Dummy Data (used when DB is unavailable) ──────────────────────────────

    public static List<Expense> GetDummyExpenses() => new()
    {
        new Expense { ExpenseId=1, UserId=1, UserName="Alice Example",   CategoryId=1, CategoryName="Travel",        StatusId=2, StatusName="Submitted", AmountMinor=2540,  AmountDecimal=25.40m,  Currency="GBP", ExpenseDate=DateTime.UtcNow.AddDays(-10), Description="Taxi from airport to client site", CreatedAt=DateTime.UtcNow.AddDays(-10) },
        new Expense { ExpenseId=2, UserId=1, UserName="Alice Example",   CategoryId=2, CategoryName="Meals",         StatusId=3, StatusName="Approved",   AmountMinor=1425,  AmountDecimal=14.25m,  Currency="GBP", ExpenseDate=DateTime.UtcNow.AddDays(-20), Description="Client lunch meeting",           CreatedAt=DateTime.UtcNow.AddDays(-20), ReviewedByName="Bob Manager", ReviewedAt=DateTime.UtcNow.AddDays(-19) },
        new Expense { ExpenseId=3, UserId=1, UserName="Alice Example",   CategoryId=3, CategoryName="Supplies",      StatusId=1, StatusName="Draft",      AmountMinor=799,   AmountDecimal=7.99m,   Currency="GBP", ExpenseDate=DateTime.UtcNow.AddDays(-3),  Description="Office stationery",              CreatedAt=DateTime.UtcNow.AddDays(-3) },
        new Expense { ExpenseId=4, UserId=1, UserName="Alice Example",   CategoryId=4, CategoryName="Accommodation", StatusId=3, StatusName="Approved",   AmountMinor=12300, AmountDecimal=123.00m, Currency="GBP", ExpenseDate=DateTime.UtcNow.AddDays(-45), Description="Hotel during client visit",      CreatedAt=DateTime.UtcNow.AddDays(-45), ReviewedByName="Bob Manager", ReviewedAt=DateTime.UtcNow.AddDays(-44) },
        new Expense { ExpenseId=5, UserId=2, UserName="Bob Manager",     CategoryId=1, CategoryName="Travel",        StatusId=4, StatusName="Rejected",   AmountMinor=5000,  AmountDecimal=50.00m,  Currency="GBP", ExpenseDate=DateTime.UtcNow.AddDays(-7),  Description="Train tickets",                  CreatedAt=DateTime.UtcNow.AddDays(-7),  ReviewedByName="Bob Manager", ReviewedAt=DateTime.UtcNow.AddDays(-6) }
    };

    public static List<User> GetDummyUsers() => new()
    {
        new User { UserId=1, UserName="Alice Example", Email="alice@example.co.uk",         RoleId=1, RoleName="Employee", ManagerId=2, ManagerName="Bob Manager", IsActive=true, CreatedAt=DateTime.UtcNow.AddDays(-90) },
        new User { UserId=2, UserName="Bob Manager",   Email="bob.manager@example.co.uk",   RoleId=2, RoleName="Manager",  ManagerId=null, IsActive=true,          CreatedAt=DateTime.UtcNow.AddDays(-90) }
    };

    public static List<ExpenseCategory> GetDummyCategories() => new()
    {
        new ExpenseCategory { CategoryId=1, CategoryName="Travel",        IsActive=true },
        new ExpenseCategory { CategoryId=2, CategoryName="Meals",         IsActive=true },
        new ExpenseCategory { CategoryId=3, CategoryName="Supplies",      IsActive=true },
        new ExpenseCategory { CategoryId=4, CategoryName="Accommodation", IsActive=true },
        new ExpenseCategory { CategoryId=5, CategoryName="Other",         IsActive=true }
    };

    public static List<Models.ExpenseStatus> GetDummyStatuses() => new()
    {
        new Models.ExpenseStatus { StatusId=1, StatusName="Draft" },
        new Models.ExpenseStatus { StatusId=2, StatusName="Submitted" },
        new Models.ExpenseStatus { StatusId=3, StatusName="Approved" },
        new Models.ExpenseStatus { StatusId=4, StatusName="Rejected" }
    };
}

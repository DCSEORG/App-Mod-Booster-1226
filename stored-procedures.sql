-- stored-procedures.sql
-- All stored procedures for the Expense Management System
-- Uses CREATE OR ALTER PROCEDURE for idempotent deployment

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetExpenses: List all expenses with related names
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetExpenses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rv.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rv ON e.ReviewedBy = rv.UserId
    ORDER BY e.CreatedAt DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetExpenseById: Get a single expense by ID
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rv.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rv ON e.ReviewedBy = rv.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetExpensesByUser: Get all expenses for a specific user
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetExpensesByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rv.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rv ON e.ReviewedBy = rv.UserId
    WHERE e.UserId = @UserId
    ORDER BY e.CreatedAt DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetExpensesByStatus: Filter expenses by status
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetExpensesByStatus
    @StatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountDecimal,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rv.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rv ON e.ReviewedBy = rv.UserId
    WHERE e.StatusId = @StatusId
    ORDER BY e.CreatedAt DESC;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_CreateExpense: Insert a new expense (Draft status)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_CreateExpense
    @UserId      INT,
    @CategoryId  INT,
    @AmountMinor INT,
    @Currency    NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DraftStatusId INT;
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';

    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());

    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_UpdateExpense: Update an existing expense
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_UpdateExpense
    @ExpenseId   INT,
    @CategoryId  INT,
    @AmountMinor INT,
    @Currency    NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET
        CategoryId  = @CategoryId,
        AmountMinor = @AmountMinor,
        Currency    = @Currency,
        ExpenseDate = @ExpenseDate,
        Description = @Description,
        ReceiptFile = @ReceiptFile
    WHERE ExpenseId = @ExpenseId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_SubmitExpense: Change a Draft expense to Submitted
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';

    UPDATE dbo.Expenses
    SET StatusId    = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_ApproveExpense: Approve a submitted expense
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_ApproveExpense
    @ExpenseId   INT,
    @ReviewedBy  INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';

    UPDATE dbo.Expenses
    SET StatusId   = @ApprovedStatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_RejectExpense: Reject a submitted expense
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_RejectExpense
    @ExpenseId   INT,
    @ReviewedBy  INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';

    UPDATE dbo.Expenses
    SET StatusId   = @RejectedStatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId
      AND StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetUsers: List all active users
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    ORDER BY u.UserName;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetCategories: List all active categories
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- usp_GetStatuses: List all expense statuses
-- ─────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE dbo.usp_GetStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

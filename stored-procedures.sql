-- Stored Procedures for Expense Management System

SET NOCOUNT ON;
GO

-- Drop existing procedures if they exist
IF OBJECT_ID('dbo.sp_GetAllExpenses', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllExpenses;
IF OBJECT_ID('dbo.sp_GetExpenseById', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetExpenseById;
IF OBJECT_ID('dbo.sp_GetExpensesByStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetExpensesByStatus;
IF OBJECT_ID('dbo.sp_GetExpensesByUser', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetExpensesByUser;
IF OBJECT_ID('dbo.sp_CreateExpense', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreateExpense;
IF OBJECT_ID('dbo.sp_UpdateExpenseStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateExpenseStatus;
IF OBJECT_ID('dbo.sp_GetAllCategories', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllCategories;
IF OBJECT_ID('dbo.sp_GetAllStatuses', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetAllStatuses;
IF OBJECT_ID('dbo.sp_GetPendingExpensesForApproval', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetPendingExpensesForApproval;
IF OBJECT_ID('dbo.sp_ApproveExpense', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ApproveExpense;
IF OBJECT_ID('dbo.sp_RejectExpense', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_RejectExpense;
GO

-- Get all expenses with details
CREATE PROCEDURE dbo.sp_GetAllExpenses
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END;
GO

-- Get expense by ID
CREATE PROCEDURE dbo.sp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE e.ExpenseId = @ExpenseId;
END;
GO

-- Get expenses by status
CREATE PROCEDURE dbo.sp_GetExpensesByStatus
    @StatusName NVARCHAR(50)
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = @StatusName
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END;
GO

-- Get expenses by user
CREATE PROCEDURE dbo.sp_GetExpensesByUser
    @UserId INT
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE e.UserId = @UserId
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END;
GO

-- Create a new expense
CREATE PROCEDURE dbo.sp_CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @SubmitNow BIT = 0
AS
BEGIN
    DECLARE @StatusId INT;
    DECLARE @ExpenseId INT;
    
    -- Determine status based on SubmitNow flag
    IF @SubmitNow = 1
        SET @StatusId = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');
    ELSE
        SET @StatusId = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');
    
    INSERT INTO dbo.Expenses (
        UserId, CategoryId, StatusId, AmountMinor, Currency, 
        ExpenseDate, Description, SubmittedAt, CreatedAt
    )
    VALUES (
        @UserId, @CategoryId, @StatusId, @AmountMinor, 'GBP',
        @ExpenseDate, @Description, 
        CASE WHEN @SubmitNow = 1 THEN SYSUTCDATETIME() ELSE NULL END,
        SYSUTCDATETIME()
    );
    
    SET @ExpenseId = SCOPE_IDENTITY();
    
    -- Return the created expense
    EXEC dbo.sp_GetExpenseById @ExpenseId;
END;
GO

-- Update expense status
CREATE PROCEDURE dbo.sp_UpdateExpenseStatus
    @ExpenseId INT,
    @StatusName NVARCHAR(50),
    @ReviewedBy INT = NULL
AS
BEGIN
    DECLARE @StatusId INT;
    
    SET @StatusId = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = @StatusName);
    
    IF @StatusId IS NULL
    BEGIN
        RAISERROR('Invalid status name', 16, 1);
        RETURN;
    END
    
    UPDATE dbo.Expenses
    SET 
        StatusId = @StatusId,
        SubmittedAt = CASE WHEN @StatusName = 'Submitted' AND SubmittedAt IS NULL THEN SYSUTCDATETIME() ELSE SubmittedAt END,
        ReviewedBy = CASE WHEN @StatusName IN ('Approved', 'Rejected') THEN @ReviewedBy ELSE ReviewedBy END,
        ReviewedAt = CASE WHEN @StatusName IN ('Approved', 'Rejected') THEN SYSUTCDATETIME() ELSE ReviewedAt END
    WHERE ExpenseId = @ExpenseId;
    
    -- Return the updated expense
    EXEC dbo.sp_GetExpenseById @ExpenseId;
END;
GO

-- Get all categories
CREATE PROCEDURE dbo.sp_GetAllCategories
AS
BEGIN
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END;
GO

-- Get all statuses
CREATE PROCEDURE dbo.sp_GetAllStatuses
AS
BEGIN
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END;
GO

-- Get pending expenses for approval (manager view)
CREATE PROCEDURE dbo.sp_GetPendingExpensesForApproval
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        CAST(e.AmountMinor/100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
    ORDER BY e.SubmittedAt ASC;
END;
GO

-- Approve expense
CREATE PROCEDURE dbo.sp_ApproveExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    EXEC dbo.sp_UpdateExpenseStatus @ExpenseId, 'Approved', @ReviewedBy;
END;
GO

-- Reject expense
CREATE PROCEDURE dbo.sp_RejectExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    EXEC dbo.sp_UpdateExpenseStatus @ExpenseId, 'Rejected', @ReviewedBy;
END;
GO

PRINT 'Stored procedures created successfully!';
GO

using app.Models;
using Microsoft.Data.SqlClient;

namespace app.Services;

public interface IDatabaseService
{
    Task<(List<Expense> expenses, string? error)> GetAllExpensesAsync();
    Task<(Expense? expense, string? error)> GetExpenseByIdAsync(int id);
    Task<(List<Expense> expenses, string? error)> GetExpensesByStatusAsync(string status);
    Task<(List<Expense> expenses, string? error)> GetPendingExpensesAsync();
    Task<(Expense? expense, string? error)> CreateExpenseAsync(CreateExpenseRequest request);
    Task<(Expense? expense, string? error)> UpdateExpenseStatusAsync(UpdateStatusRequest request);
    Task<(Expense? expense, string? error)> ApproveExpenseAsync(int expenseId, int reviewedBy);
    Task<(List<ExpenseCategory> categories, string? error)> GetAllCategoriesAsync();
    Task<(List<ExpenseStatus> statuses, string? error)> GetAllStatusesAsync();
    bool IsConnected();
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private bool _isConnected = false;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        _logger = logger;
    }

    public bool IsConnected() => _isConnected;

    private async Task<(SqlConnection? connection, string? error)> GetConnectionAsync()
    {
        try
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _isConnected = true;
            return (connection, null);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetConnectionAsync", 32);
            _logger.LogError(ex, "Database connection failed");
            return (null, errorMsg);
        }
    }

    private string FormatDetailedError(Exception ex, string location, int lineNumber)
    {
        var message = $"Error in {location} (Line {lineNumber}): {ex.Message}";
        
        if (ex is SqlException sqlEx)
        {
            message += $" | SQL Error: {sqlEx.Number}";
            
            if (sqlEx.Message.Contains("managed identity", StringComparison.OrdinalIgnoreCase) ||
                sqlEx.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            {
                message += " | MANAGED IDENTITY FIX: Ensure the App Service managed identity has db_datareader and db_datawriter roles on the database. Run: CREATE USER [your-managed-identity-name] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [your-managed-identity-name]; ALTER ROLE db_datawriter ADD MEMBER [your-managed-identity-name];";
            }
        }
        
        return message;
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Demo User",
                Email = "demo@example.com",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 12000,
                Amount = 120.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Client meeting travel",
                SubmittedAt = DateTime.Now.AddDays(-4),
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Demo User",
                Email = "demo@example.com",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 6900,
                Amount = 69.00m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Business lunch",
                SubmittedAt = DateTime.Now.AddDays(-9),
                ReviewedAt = DateTime.Now.AddDays(-8),
                CreatedAt = DateTime.Now.AddDays(-10)
            }
        };
    }

    public async Task<(List<Expense> expenses, string? error)> GetAllExpensesAsync()
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (GetDummyExpenses(), error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetAllExpenses", connection);
                command.CommandTimeout = 30;
                
                var expenses = new List<Expense>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        expenses.Add(MapExpense(reader));
                    }
                }
                return (expenses, null);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetAllExpensesAsync", 120);
            _logger.LogError(ex, "Failed to get expenses");
            return (GetDummyExpenses(), errorMsg);
        }
    }

    public async Task<(Expense? expense, string? error)> GetExpenseByIdAsync(int id)
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (null, error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetExpenseById @ExpenseId", connection);
                command.Parameters.AddWithValue("@ExpenseId", id);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (MapExpense(reader), null);
                    }
                }
                return (null, "Expense not found");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetExpenseByIdAsync", 150);
            _logger.LogError(ex, "Failed to get expense by ID");
            return (null, errorMsg);
        }
    }

    public async Task<(List<Expense> expenses, string? error)> GetExpensesByStatusAsync(string status)
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (GetDummyExpenses().Where(e => e.StatusName == status).ToList(), error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetExpensesByStatus @StatusName", connection);
                command.Parameters.AddWithValue("@StatusName", status);
                
                var expenses = new List<Expense>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        expenses.Add(MapExpense(reader));
                    }
                }
                return (expenses, null);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetExpensesByStatusAsync", 185);
            _logger.LogError(ex, "Failed to get expenses by status");
            return (GetDummyExpenses().Where(e => e.StatusName == status).ToList(), errorMsg);
        }
    }

    public async Task<(List<Expense> expenses, string? error)> GetPendingExpensesAsync()
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList(), error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetPendingExpensesForApproval", connection);
                
                var expenses = new List<Expense>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        expenses.Add(MapExpense(reader));
                    }
                }
                return (expenses, null);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetPendingExpensesAsync", 218);
            _logger.LogError(ex, "Failed to get pending expenses");
            return (GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList(), errorMsg);
        }
    }

    public async Task<(Expense? expense, string? error)> CreateExpenseAsync(CreateExpenseRequest request)
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (null, error);
        }

        try
        {
            using (connection)
            {
                var amountMinor = (int)(request.Amount * 100);
                var command = new SqlCommand(
                    "EXEC dbo.sp_CreateExpense @UserId, @CategoryId, @AmountMinor, @ExpenseDate, @Description, @SubmitNow", 
                    connection);
                command.Parameters.AddWithValue("@UserId", request.UserId);
                command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                command.Parameters.AddWithValue("@AmountMinor", amountMinor);
                command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
                command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@SubmitNow", request.SubmitNow);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (MapExpense(reader), null);
                    }
                }
                return (null, "Failed to create expense");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.CreateExpenseAsync", 260);
            _logger.LogError(ex, "Failed to create expense");
            return (null, errorMsg);
        }
    }

    public async Task<(Expense? expense, string? error)> UpdateExpenseStatusAsync(UpdateStatusRequest request)
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (null, error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand(
                    "EXEC dbo.sp_UpdateExpenseStatus @ExpenseId, @StatusName, @ReviewedBy", 
                    connection);
                command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
                command.Parameters.AddWithValue("@StatusName", request.StatusName);
                command.Parameters.AddWithValue("@ReviewedBy", (object?)request.ReviewedBy ?? DBNull.Value);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (MapExpense(reader), null);
                    }
                }
                return (null, "Failed to update expense status");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.UpdateExpenseStatusAsync", 296);
            _logger.LogError(ex, "Failed to update expense status");
            return (null, errorMsg);
        }
    }

    public async Task<(Expense? expense, string? error)> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            return (null, error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand(
                    "EXEC dbo.sp_ApproveExpense @ExpenseId, @ReviewedBy", 
                    connection);
                command.Parameters.AddWithValue("@ExpenseId", expenseId);
                command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return (MapExpense(reader), null);
                    }
                }
                return (null, "Failed to approve expense");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.ApproveExpenseAsync", 332);
            _logger.LogError(ex, "Failed to approve expense");
            return (null, errorMsg);
        }
    }

    public async Task<(List<ExpenseCategory> categories, string? error)> GetAllCategoriesAsync()
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            var dummyCategories = new List<ExpenseCategory>
            {
                new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
                new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
                new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true }
            };
            return (dummyCategories, error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetAllCategories", connection);
                
                var categories = new List<ExpenseCategory>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new ExpenseCategory
                        {
                            CategoryId = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            IsActive = reader.GetBoolean(2)
                        });
                    }
                }
                return (categories, null);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetAllCategoriesAsync", 380);
            _logger.LogError(ex, "Failed to get categories");
            var dummyCategories = new List<ExpenseCategory>
            {
                new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true }
            };
            return (dummyCategories, errorMsg);
        }
    }

    public async Task<(List<ExpenseStatus> statuses, string? error)> GetAllStatusesAsync()
    {
        var (connection, error) = await GetConnectionAsync();
        if (connection == null)
        {
            var dummyStatuses = new List<ExpenseStatus>
            {
                new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
                new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
                new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
                new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
            };
            return (dummyStatuses, error);
        }

        try
        {
            using (connection)
            {
                var command = new SqlCommand("EXEC dbo.sp_GetAllStatuses", connection);
                
                var statuses = new List<ExpenseStatus>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        statuses.Add(new ExpenseStatus
                        {
                            StatusId = reader.GetInt32(0),
                            StatusName = reader.GetString(1)
                        });
                    }
                }
                return (statuses, null);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = FormatDetailedError(ex, "DatabaseService.GetAllStatusesAsync", 430);
            _logger.LogError(ex, "Failed to get statuses");
            var dummyStatuses = new List<ExpenseStatus>
            {
                new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
                new ExpenseStatus { StatusId = 3, StatusName = "Approved" }
            };
            return (dummyStatuses, errorMsg);
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}

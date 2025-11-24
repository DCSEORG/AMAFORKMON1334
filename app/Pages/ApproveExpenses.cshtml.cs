using app.Models;
using app.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace app.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly IDatabaseService _databaseService;

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool ApprovalSuccess { get; set; }

    [BindProperty]
    public int ExpenseId { get; set; }

    public ApproveExpensesModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        var (expenses, error) = await _databaseService.GetPendingExpensesAsync();
        PendingExpenses = expenses;
        ErrorMessage = error;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Note: ReviewedBy should be derived from authenticated user in production
        var (expense, error) = await _databaseService.ApproveExpenseAsync(ExpenseId, 2); // TODO: Replace with authenticated user ID

        if (error != null)
        {
            ErrorMessage = error;
            var (expenses, _) = await _databaseService.GetPendingExpensesAsync();
            PendingExpenses = expenses;
            return Page();
        }

        ApprovalSuccess = true;

        // Refresh the list
        var (updatedExpenses, _) = await _databaseService.GetPendingExpensesAsync();
        PendingExpenses = updatedExpenses;

        return Page();
    }
}

using app.Models;
using app.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace app.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IDatabaseService _databaseService;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty]
    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    [BindProperty]
    public int CategoryId { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    public AddExpenseModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        var (categories, error) = await _databaseService.GetAllCategoriesAsync();
        Categories = categories;
        ErrorMessage = error;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (categories, _) = await _databaseService.GetAllCategoriesAsync();
        Categories = categories;

        // Note: UserId should be derived from authenticated user in production
        var request = new CreateExpenseRequest
        {
            UserId = 1, // TODO: Replace with authenticated user ID
            Amount = Amount,
            CategoryId = CategoryId,
            ExpenseDate = ExpenseDate,
            Description = Description,
            SubmitNow = true
        };

        var (expense, error) = await _databaseService.CreateExpenseAsync(request);

        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }

        Success = true;
        
        // Clear form
        Amount = 0;
        CategoryId = 0;
        Description = null;
        ExpenseDate = DateTime.Now;

        return Page();
    }
}

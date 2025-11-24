using app.Models;
using app.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace app.Pages;

public class IndexModel : PageModel
{
    private readonly IDatabaseService _databaseService;

    public List<Expense> Expenses { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IndexModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        var (expenses, error) = await _databaseService.GetAllExpensesAsync();
        Expenses = expenses;
        ErrorMessage = error;
    }
}

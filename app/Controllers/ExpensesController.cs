using app.Models;
using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IDatabaseService databaseService, ILogger<ExpensesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAll()
    {
        var (expenses, error) = await _databaseService.GetAllExpensesAsync();
        
        if (error != null)
        {
            _logger.LogWarning($"GetAll returned with error: {error}");
            return Ok(new { expenses, error });
        }
        
        return Ok(expenses);
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        var (expense, error) = await _databaseService.GetExpenseByIdAsync(id);
        
        if (error != null)
        {
            return NotFound(new { error });
        }
        
        return Ok(expense);
    }

    /// <summary>
    /// Get expenses by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<Expense>>> GetByStatus(string status)
    {
        var (expenses, error) = await _databaseService.GetExpensesByStatusAsync(status);
        
        if (error != null)
        {
            _logger.LogWarning($"GetByStatus returned with error: {error}");
            return Ok(new { expenses, error });
        }
        
        return Ok(expenses);
    }

    /// <summary>
    /// Get pending expenses for approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPending()
    {
        var (expenses, error) = await _databaseService.GetPendingExpensesAsync();
        
        if (error != null)
        {
            _logger.LogWarning($"GetPending returned with error: {error}");
            return Ok(new { expenses, error });
        }
        
        return Ok(expenses);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Expense>> Create([FromBody] CreateExpenseRequest request)
    {
        var (expense, error) = await _databaseService.CreateExpenseAsync(request);
        
        if (error != null)
        {
            return BadRequest(new { error });
        }
        
        return CreatedAtAction(nameof(GetById), new { id = expense!.ExpenseId }, expense);
    }

    /// <summary>
    /// Update expense status
    /// </summary>
    [HttpPut("status")]
    public async Task<ActionResult<Expense>> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        var (expense, error) = await _databaseService.UpdateExpenseStatusAsync(request);
        
        if (error != null)
        {
            return BadRequest(new { error });
        }
        
        return Ok(expense);
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<Expense>> Approve(int id, [FromQuery] int reviewedBy = 2)
    {
        var (expense, error) = await _databaseService.ApproveExpenseAsync(id, reviewedBy);
        
        if (error != null)
        {
            return BadRequest(new { error });
        }
        
        return Ok(expense);
    }
}

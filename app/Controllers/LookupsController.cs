using app.Models;
using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IDatabaseService databaseService, ILogger<CategoriesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetAll()
    {
        var (categories, error) = await _databaseService.GetAllCategoriesAsync();
        
        if (error != null)
        {
            _logger.LogWarning($"GetAllCategories returned with error: {error}");
            return Ok(new { categories, error });
        }
        
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StatusesController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<StatusesController> _logger;

    public StatusesController(IDatabaseService databaseService, ILogger<StatusesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetAll()
    {
        var (statuses, error) = await _databaseService.GetAllStatusesAsync();
        
        if (error != null)
        {
            _logger.LogWarning($"GetAllStatuses returned with error: {error}");
            return Ok(new { statuses, error });
        }
        
        return Ok(statuses);
    }
}

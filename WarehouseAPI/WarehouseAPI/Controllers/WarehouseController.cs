using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.DTOs;
using WarehouseAPI.Services;

namespace WarehouseAPI.Controllers;


[ApiController]
[Route("api/warehouse")]
public class WarehouseController(IDbService dbService) : ControllerBase
{
    
    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse(AddProductToWarehouseRequest request)
    {
        if (request.Amount < 1)
            return BadRequest("Amount should be greater than 0");
        
        if (await dbService.DoesProductExist(request.IdProduct) is null) 
            return NotFound($"Product with id - {request.IdProduct} does not exist");
    
        if (!await dbService.DoesWarehouseExist(request.IdWarehouse))
            return NotFound($"Warehouse with id - {request.IdWarehouse} does not exist");
    
        var orderId = await dbService.DoesOrderExist(request.IdProduct, request.Amount, request.CreatedAt);
        if (orderId == 0)
            return NotFound("There is no corresponding order");
    
        if (!await dbService.IsOrderNotFulfilled(orderId))
            return Conflict($"Order with id - {orderId} is already fulfilled");
    
    
        var createdId = await dbService.AddProductToWarehouse(request);
        
        return Created($"api/warehouse/{createdId}", $"Created id - {createdId}");
    }
    
    // [HttpPost]
    // public async Task<IActionResult> AddProductToWarehouseProcedure(AddProductToWarehouseRequest request)
    // {
    //     var createdId = await dbService.AddProductToWarehouseProcedure(request);
    //     if (createdId == 0)
    //         return NotFound();
    //     return Created($"api/warehouse/{createdId}", $"Created id - {createdId}");
    // }
    
}
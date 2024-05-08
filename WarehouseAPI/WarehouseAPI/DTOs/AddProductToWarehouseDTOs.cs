using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.DTOs;

public record AddProductToWarehouseRequest
(
    [Required] int IdProduct,
    [Required] int IdWarehouse,
    [Required] int Amount,
    [Required] DateTime CreatedAt
);

using System.ComponentModel.DataAnnotations;
namespace WarehouseAPI.DTOs;

public record ProductDTO
(
    [Required] int IdProduct,
    [Required] string Name,
    [Required] string Description,
    [Required] decimal Price
);
using System.Data.SqlClient;
using WarehouseAPI.DTOs;

namespace WarehouseAPI.Services;

public interface IDbService
{
    Task<ProductDTO?> DoesProductExist(int idProduct);
    Task<bool> DoesWarehouseExist(int idWarehouse);
    Task<bool> IsOrderNotFulfilled(int idOrder);
    Task<int> DoesOrderExist(int idProduct, int amount, DateTime date);
    Task<int> AddProductToWarehouse(AddProductToWarehouseRequest request);
    Task<int> AddProductToWarehouseProcedure(AddProductToWarehouseRequest request);

}


public class DbService(IConfiguration configuration) : IDbService
{
    
    public async Task<ProductDTO?> DoesProductExist(int idProduct)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT * FROM Product WHERE IdProduct = @idProduct";
        command.Parameters.AddWithValue("@idProduct", idProduct);
        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();
        
        if (!reader.HasRows) return null;
        
        await reader.ReadAsync();
        var result = new ProductDTO(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDecimal(3)
        );

        return result;
    }

    public async Task<bool> DoesWarehouseExist(int idWarehouse)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @idWarehouse";
        command.Parameters.AddWithValue("@idWarehouse", idWarehouse);
        
        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        
        return result is not null;
    }

    public async Task<int> DoesOrderExist(int idProduct, int amount, DateTime date)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                               SELECT IdOrder
                               FROM "Order"
                               WHERE IdProduct = @idProduct AND Amount = @amount AND CreatedAt < @date
                               """;
        command.Parameters.AddWithValue("@idProduct", idProduct);
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@date", date);
        
        await connection.OpenAsync();
        var idOrder = await command.ExecuteScalarAsync();
        if (idOrder is null) return 0;
        return (int)idOrder;
    }

    public async Task<bool> IsOrderNotFulfilled(int idOrder)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                               SELECT 1
                               FROM Product_Warehouse pw
                               JOIN "Order" o
                               ON o.IdOrder = pw.IdOrder
                               WHERE o.IdOrder = @idOrder
                               """;
        command.Parameters.AddWithValue("@idOrder", idOrder);
        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        return result is null;
    }
    

    public async Task<int> AddProductToWarehouse(AddProductToWarehouseRequest request)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var idOrder = await DoesOrderExist(request.IdProduct, request.Amount, request.CreatedAt);
            await using var command = new SqlCommand();
            command.Connection = connection;
            command.CommandText = """
                                    UPDATE "Order"
                                    SET FulfilledAt = @fulfilledAt
                                    WHERE IdOrder = @idOrder
                                    """;
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@idOrder", idOrder);
            command.Parameters.AddWithValue("@fulfilledAt", DateTime.Now);
            await command.ExecuteNonQueryAsync();
    
            var product = await DoesProductExist(request.IdProduct);
            var price = product.Price * request.Amount;
            command.Parameters.Clear();
            command.CommandText = """
                                    INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                    VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                                    SELECT CAST(SCOPE_IDENTITY() AS int);
                                    """;
            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@Price", price);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            
            var idProductWarehouse = await command.ExecuteScalarAsync();
            
            await transaction.CommitAsync();
    
            return (int)idProductWarehouse;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<int> AddProductToWarehouseProcedure(AddProductToWarehouseRequest request)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """EXEC AddProductToWarehouse @IdProduct, @IdWarehouse, @Amount, @CreatedAt""";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
        await connection.OpenAsync();
        var createdId = await command.ExecuteScalarAsync();
        if (createdId is null) return 0;
        return (int)createdId;
    }
    
}
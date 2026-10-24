using EcommerceRefatorado.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

public sealed class PedidoRepository(AppDbContext dbContext) : IPedidoRepository
{
    public async Task<IReadOnlyList<Pedido>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Pedidos
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .ThenInclude(item => item.Produto)
            .OrderBy(pedido => pedido.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Pedidos
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .ThenInclude(item => item.Produto)
            .FirstOrDefaultAsync(pedido => pedido.Id == id, cancellationToken);
    }

    public async Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        await dbContext.Pedidos.AddAsync(pedido, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddWithStockAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Produtos.UpdateRange(produtos);
            await dbContext.Pedidos.AddAsync(pedido, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default)
    {
        dbContext.Pedidos.Update(pedido);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var pedido = await dbContext.Pedidos
            .FirstOrDefaultAsync(pedido => pedido.Id == id, cancellationToken);

        if (pedido is null)
        {
            return false;
        }

        dbContext.Pedidos.Remove(pedido);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
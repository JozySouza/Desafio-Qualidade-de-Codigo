using EcommerceRefatorado.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

public sealed class PedidoRepository(AppDbContext dbContext) : IPedidoRepository
{
    public async Task<IReadOnlyList<Pedido>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Pedidos
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .ThenInclude(item => item.Produto)
            .OrderBy(pedido => pedido.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Pedido?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Pedidos
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .ThenInclude(item => item.Produto)
            .FirstOrDefaultAsync(
                pedido => pedido.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Pedido pedido,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Pedidos.AddAsync(
            pedido,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPendingAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default)
    {
        dbContext.AttachRange(produtos);

        await dbContext.Pedidos.AddAsync(
            pedido,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddWithStockAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            dbContext.Produtos.UpdateRange(produtos);

            await dbContext.Pedidos.AddAsync(
                pedido,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // Atualiza somente o status e o rastreamento.
    // Evita sobrescrever dados antigos do estoque.
    public async Task UpdateAsync(
        Pedido pedido,
        CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(pedido);

        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Unchanged;
        }

        entry.Property(p => p.Status).IsModified = true;
        entry.Property(p => p.CodigoRastreio).IsModified = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWithStockAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            dbContext.Pedidos.Update(pedido);
            dbContext.Produtos.UpdateRange(produtos);

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // Executa uma operação dentro de uma única transação.
    // Se ocorrer uma exceção, desfaz as alterações no banco.
    public async Task<T> ExecutarEmTransacaoAsync<T>(
        Func<CancellationToken, Task<T>> operacao,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var resultado = await operacao(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // Impede que duas requisições reprocessarem
    // simultaneamente um mesmo pedido com pagamento falho.
    public async Task<bool> TentarIniciarReprocessamentoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "O reprocessamento precisa ocorrer dentro de uma transação.");
        }

        var linhasAfetadas =
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE "Pedidos"
                SET "Status" = 'Pendente'
                WHERE "Id" = {pedidoId}
                  AND "Status" = 'Falhou';
                """,
                cancellationToken);

        return linhasAfetadas == 1;
    }

    // Reserva o estoque de forma atômica.
    // A reserva acontece dentro da transação principal.
    public async Task<bool> ReservarEstoqueAsync(
        IReadOnlyCollection<ItemPedido> itens,
        CancellationToken cancellationToken = default)
    {
        if (itens.Count == 0)
        {
            throw new DomainException(
                "Não é possível reservar estoque para um pedido sem itens.");
        }

        var itensAgrupados = itens
            .GroupBy(item => item.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .OrderBy(item => item.ProdutoId)
            .ToList();

        if (itensAgrupados.Any(item => item.Quantidade <= 0))
        {
            throw new DomainException(
                "A quantidade dos produtos deve ser maior que zero.");
        }

        var transaction = dbContext.Database.CurrentTransaction;

        if (transaction is null)
        {
            throw new InvalidOperationException(
                "A reserva de estoque precisa ocorrer dentro de uma transação.");
        }

        // O savepoint permite desfazer uma reserva parcial
        // sem perder as demais operações da transação.
        var savepoint = $"reserva_{Guid.NewGuid():N}";

        await transaction.CreateSavepointAsync(
            savepoint,
            cancellationToken);

        try
        {
            foreach (var item in itensAgrupados)
            {
                var linhasAfetadas =
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                        UPDATE "Produtos"
                        SET "Estoque" = "Estoque" - {item.Quantidade}
                        WHERE "Id" = {item.ProdutoId}
                          AND "Ativo" = 1
                          AND "Estoque" >= {item.Quantidade};
                        """,
                        cancellationToken);

                if (linhasAfetadas != 1)
                {
                    await transaction.RollbackToSavepointAsync(
                        savepoint,
                        cancellationToken);

                    await transaction.ReleaseSavepointAsync(
                        savepoint,
                        cancellationToken);

                    return false;
                }
            }

            await transaction.ReleaseSavepointAsync(
                savepoint,
                cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackToSavepointAsync(
                savepoint,
                CancellationToken.None);

            throw;
        }
    }

    // Devolve o estoque quando o pagamento é recusado.
    // A liberação participa da mesma transação da reserva.
    public async Task LiberarEstoqueAsync(
        IReadOnlyCollection<ItemPedido> itens,
        CancellationToken cancellationToken = default)
    {
        if (itens.Count == 0)
        {
            throw new DomainException(
                "Não existem itens para liberar.");
        }

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A liberação de estoque precisa ocorrer dentro de uma transação.");
        }

        var itensAgrupados = itens
            .GroupBy(item => item.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .OrderBy(item => item.ProdutoId)
            .ToList();

        if (itensAgrupados.Any(item => item.Quantidade <= 0))
        {
            throw new DomainException(
                "A quantidade dos produtos deve ser maior que zero.");
        }

        foreach (var item in itensAgrupados)
        {
            var linhasAfetadas =
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE "Produtos"
                    SET "Estoque" = "Estoque" + {item.Quantidade}
                    WHERE "Id" = {item.ProdutoId};
                    """,
                    cancellationToken);

            if (linhasAfetadas != 1)
            {
                throw new InvalidOperationException(
                    $"Não foi possível liberar o estoque do produto {item.ProdutoId}.");
            }
        }
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await dbContext.Pedidos
            .FirstOrDefaultAsync(
                pedido => pedido.Id == id,
                cancellationToken);

        if (pedido is null)
        {
            return false;
        }

        dbContext.Pedidos.Remove(pedido);

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
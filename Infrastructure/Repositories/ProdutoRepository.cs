using EcommerceRefatorado.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceRefatorado;

public sealed class ProdutoRepository(AppDbContext dbContext) : IProdutoRepository
{
    public async Task<IReadOnlyList<Produto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Produtos
            .AsNoTracking()
            .OrderBy(produto => produto.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Produto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.Produtos
            .AsNoTracking()
            .FirstOrDefaultAsync(produto => produto.Id == id, cancellationToken);
    }

    public async Task AddAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        await dbContext.Produtos.AddAsync(produto, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Produto produto, CancellationToken cancellationToken = default)
    {
        dbContext.Produtos.Update(produto);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
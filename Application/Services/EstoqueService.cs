using EcommerceRefatorado;
using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado.Services;

public class EstoqueService : IEstoqueService
{
    private readonly IProdutoRepository _repository;

    public EstoqueService(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<EstoqueResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _repository.GetAllAsync(cancellationToken);
        return produtos
            .Where(produto => produto.Ativo)
            .Select(p => new EstoqueResponse(p.Nome, p.Estoque))
            .ToList();
    }

    public async Task<EstoqueResponse?> ReabastecerAsync(int id, int quantidade, CancellationToken cancellationToken = default)
    {
        var produto = await _repository.GetByIdAsync(id, cancellationToken);
        if (produto is null || !produto.Ativo) return null;
        
        produto.Reabastecer(quantidade);
        await _repository.UpdateAsync(produto, cancellationToken);

        return new EstoqueResponse(produto.Nome, produto.Estoque);
    }
}
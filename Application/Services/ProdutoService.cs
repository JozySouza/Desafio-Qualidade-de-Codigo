using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;

    public ProdutoService(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProdutoResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var produtos = await _repository.GetAllAsync(cancellationToken);
        return produtos.Select(p => new ProdutoResponse(p.Id, p.Nome, p.Preco, p.Estoque, p.Descricao)).ToList();
    }

    public async Task<ProdutoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var produto = await _repository.GetByIdAsync(id, cancellationToken);
        if (produto is null) return null;

        return new ProdutoResponse(produto.Id, produto.Nome, produto.Preco, produto.Estoque, produto.Descricao);
    }

    public async Task<ProdutoResponse> AddAsync(ProdutoRequest request, CancellationToken cancellationToken = default)
    {
        var produto = new Produto
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Preco = request.Preco,
            Estoque = request.Estoque
        };

        await _repository.AddAsync(produto, cancellationToken);

        return new ProdutoResponse(produto.Id, produto.Nome, produto.Preco, produto.Estoque, produto.Descricao);
    }

    public async Task<ProdutoResponse?> UpdateAsync(int id, ProdutoRequest request, CancellationToken cancellationToken = default)
    {
        var produto = await _repository.GetByIdAsync(id, cancellationToken);
        if (produto is null) return null;

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Preco = request.Preco;
        produto.Estoque = request.Estoque;

        await _repository.UpdateAsync(produto, cancellationToken);

        return new ProdutoResponse (
            produto.Id,
            produto.Nome,
            produto.Preco,
            produto.Estoque,
            produto.Descricao
        );
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted;
    }
}
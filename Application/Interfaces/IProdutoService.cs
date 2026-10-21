namespace EcommerceRefatorado.Interfaces;

using EcommerceRefatorado;

public interface IProdutoService
{
    Task<IReadOnlyList<ProdutoResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProdutoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProdutoResponse> AddAsync(ProdutoRequest request, CancellationToken cancellationToken = default);
    Task<ProdutoResponse?> UpdateAsync(int id, ProdutoRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
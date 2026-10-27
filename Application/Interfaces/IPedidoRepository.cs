namespace EcommerceRefatorado.Interfaces;

using EcommerceRefatorado;

public interface IPedidoRepository
{
    Task<IReadOnlyList<Pedido>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Pedido pedido, CancellationToken cancellationToken = default);
    Task AddWithStockAsync(Pedido pedido, IReadOnlyCollection<Produto> produtos, CancellationToken cancellationToken = default);
    Task UpdateAsync(Pedido pedido, CancellationToken cancellationToken = default);
    Task UpdateWithStockAsync(Pedido pedido, IReadOnlyCollection<Produto> produtos, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

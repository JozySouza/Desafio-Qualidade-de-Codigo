namespace EcommerceRefatorado.Interfaces;

using EcommerceRefatorado;

public interface IPedidoRepository
{
    Task<IReadOnlyList<Pedido>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Pedido?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Pedido pedido,
        CancellationToken cancellationToken = default);

    Task AddPendingAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default);

    Task AddWithStockAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Pedido pedido,
        CancellationToken cancellationToken = default);

    Task UpdateWithStockAsync(
        Pedido pedido,
        IReadOnlyCollection<Produto> produtos,
        CancellationToken cancellationToken = default);

    // Executa uma operação dentro de uma transação.
    // Em caso de erro, as alterações no banco são desfeitas.
    Task<T> ExecutarEmTransacaoAsync<T>(
        Func<CancellationToken, Task<T>> operacao,
        CancellationToken cancellationToken = default);

    // Impede que duas requisições iniciem o reprocessamento
    // do mesmo pedido simultaneamente.
    Task<bool> TentarIniciarReprocessamentoAsync(
        int pedidoId,
        CancellationToken cancellationToken = default);

    // Reserva o estoque antes de processar o pagamento.
    Task<bool> ReservarEstoqueAsync(
        IReadOnlyCollection<ItemPedido> itens,
        CancellationToken cancellationToken = default);

    // Libera o estoque reservado quando o pagamento é recusado.
    Task LiberarEstoqueAsync(
        IReadOnlyCollection<ItemPedido> itens,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
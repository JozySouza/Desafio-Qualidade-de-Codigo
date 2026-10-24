namespace EcommerceRefatorado.Interfaces;

public interface IPedidoService
{
    Task<PedidoResponse> CriarAsync(PedidoRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PedidoResponse>> ListarAsync(CancellationToken cancellationToken = default);
    Task<PedidoResponse?> BuscarAsync(int id, CancellationToken cancellationToken = default);
}
namespace EcommerceRefatorado.Interfaces;

using EcommerceRefatorado;

public interface IEstoqueService
{
    Task<IReadOnlyList<EstoqueResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EstoqueResponse?> ReabastecerAsync(int id, int quantidade, CancellationToken cancellationToken = default);
}
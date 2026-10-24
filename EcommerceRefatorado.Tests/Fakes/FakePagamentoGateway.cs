
using EcommerceRefatorado;
using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado.Tests.Fakes;

public class FakePagamentoGateway : IPagamentoGateway
{
    public bool PagamentoAprovado { get; set; } = true;

    public int QuantidadeChamadas { get; private set; }

    public Task<bool> ProcessarAsync(
        decimal valor,
        FormaPagamento formaPagamento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        QuantidadeChamadas++;

        return Task.FromResult(PagamentoAprovado);
    }
}

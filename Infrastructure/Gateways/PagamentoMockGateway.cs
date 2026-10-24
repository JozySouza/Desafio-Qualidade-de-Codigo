using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado;

public sealed class PagamentoMockGateway : IPagamentoGateway
{
	public Task<bool> ProcessarAsync(
		decimal valor,
		FormaPagamento formaPagamento,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(valor > 0m && formaPagamento != FormaPagamento.NaoInformado);
	}
}

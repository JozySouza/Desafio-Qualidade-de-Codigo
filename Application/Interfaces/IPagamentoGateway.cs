namespace EcommerceRefatorado.Interfaces;

public interface IPagamentoGateway
{
	Task<bool> ProcessarAsync(decimal valor, FormaPagamento formaPagamento, CancellationToken cancellationToken = default);
}

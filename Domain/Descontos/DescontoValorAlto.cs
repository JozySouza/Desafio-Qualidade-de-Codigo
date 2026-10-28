namespace EcommerceRefatorado;

public sealed class DescontoValorAlto : IRegraDesconto
{
	private const decimal ValorMinimo = 500m;
	private const decimal PercentualDesconto = 0.10m;

	public decimal CalcularPercentual(decimal total, string clienteEmail)
	{
		return total > ValorMinimo ? PercentualDesconto : 0m;
	}
}

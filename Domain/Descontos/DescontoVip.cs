namespace EcommerceRefatorado;

public sealed class DescontoVip : IRegraDesconto
{
	private const decimal PercentualDesconto = 0.20m;

	public decimal CalcularPercentual(decimal total, string clienteEmail)
	{
		return clienteEmail.Contains("vip", StringComparison.OrdinalIgnoreCase)
			? PercentualDesconto
			: 0m;
	}
}

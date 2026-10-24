namespace EcommerceRefatorado;

public sealed class DescontoVip : IRegraDesconto
{
	public decimal CalcularDesconto(decimal total, string clienteEmail)
	{
		return clienteEmail.Contains("vip", StringComparison.OrdinalIgnoreCase)
			? total * 0.20m
			: 0m;
	}
}

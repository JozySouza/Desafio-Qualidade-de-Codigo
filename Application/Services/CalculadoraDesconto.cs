namespace EcommerceRefatorado.Services;

public sealed class CalculadoraDesconto(IEnumerable<IRegraDesconto> regras)
{
	public decimal Calcular(decimal total, string clienteEmail)
	{
		var percentual = regras
			.Select(regra => regra.CalcularPercentual(total, clienteEmail))
			.DefaultIfEmpty(0m)
			.Max();

		return total - total * percentual;
	}
}

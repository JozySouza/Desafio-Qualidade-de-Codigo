namespace EcommerceRefatorado.Services;

public sealed class CalculadoraDesconto(IEnumerable<IRegraDesconto> regras)
{
	public decimal Calcular(decimal total, string clienteEmail)
	{
		foreach (var regra in regras)
		{
			var desconto = regra.CalcularDesconto(total, clienteEmail);
			if (desconto > 0m)
			{
				return total - desconto;
			}
		}

		return total;
	}
}

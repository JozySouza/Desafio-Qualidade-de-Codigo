namespace EcommerceRefatorado;

public sealed class DescontoValorAlto : IRegraDesconto
{
	public decimal CalcularDesconto(decimal total, string clienteEmail)
	{
		return total > 500m ? total * 0.10m : 0m;
	}
}

namespace EcommerceRefatorado;

public interface IRegraDesconto
{
	decimal CalcularDesconto(decimal total, string clienteEmail);
}

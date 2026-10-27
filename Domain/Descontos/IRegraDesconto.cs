namespace EcommerceRefatorado;

public interface IRegraDesconto
{
	decimal CalcularPercentual(decimal total, string clienteEmail);
}

namespace EcommerceRefatorado;

public interface ILogisticaGateway
{
	Task<string> EnviarAsync(Pedido pedido, CancellationToken cancellationToken = default);
}


namespace EcommerceRefatorado;

public sealed class LogisticaMockGateway : ILogisticaGateway
{
	public Task<string> EnviarAsync(Pedido pedido, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult($"BR{Guid.NewGuid():N}"[..20].ToUpperInvariant());
	}
}

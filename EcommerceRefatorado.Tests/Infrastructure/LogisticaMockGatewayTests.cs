
using EcommerceRefatorado;
using Xunit;

namespace EcommerceRefatorado.Tests.Infrastructure;

public class LogisticaMockGatewayTests
{
    private readonly LogisticaMockGateway _gateway = new();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EnviarPedido_DeveGerarCodigoRastreio()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1
        };

        // Act
        var codigo = await _gateway.EnviarAsync(pedido);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(codigo));
        Assert.StartsWith("BR", codigo);
        Assert.Equal(20, codigo.Length);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PedidosDiferentes_DevemGerarCodigosDiferentes()
    {
        // Arrange
        var pedido1 = new Pedido { Id = 1 };
        var pedido2 = new Pedido { Id = 2 };

        // Act
        var codigo1 = await _gateway.EnviarAsync(pedido1);
        var codigo2 = await _gateway.EnviarAsync(pedido2);

        // Assert
        Assert.NotEqual(codigo1, codigo2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Cancelamento_DeveInterromperEnvio()
    {
        // Arrange
        var pedido = new Pedido { Id = 1 };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act e Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _gateway.EnviarAsync(
                pedido,
                cancellationTokenSource.Token));
    }
}


using EcommerceRefatorado;
using Xunit;

namespace EcommerceRefatorado.Tests.Infrastructure;

public class PagamentoMockGatewayTests
{
    private readonly PagamentoMockGateway _gateway = new();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PagamentoValido_DeveSerAprovado()
    {
        var resultado = await _gateway.ProcessarAsync(
            150m, FormaPagamento.Pix);

        Assert.True(resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValorZero_DeveSerRecusado()
    {
        var resultado = await _gateway.ProcessarAsync(
            0m, FormaPagamento.Pix);

        Assert.False(resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValorNegativo_DeveSerRecusado()
    {
        var resultado = await _gateway.ProcessarAsync(
            -50m, FormaPagamento.CartaoCredito);

        Assert.False(resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FormaPagamentoNaoInformada_DeveSerRecusada()
    {
        var resultado = await _gateway.ProcessarAsync(
            100m, FormaPagamento.NaoInformado);

        Assert.False(resultado);
    }
}

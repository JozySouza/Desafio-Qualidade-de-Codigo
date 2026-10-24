
using EcommerceRefatorado;
using EcommerceRefatorado.Services;
using Xunit;

namespace EcommerceRefatorado.Tests.Domain;

public class CalculadoraDescontoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void SemRegraAplicavel_DeveManterValorOriginal()
    {
        var calculadora = CriarCalculadora();

        var resultado = calculadora.Calcular(
            200m, "cliente@exemplo.com");

        Assert.Equal(200m, resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void PedidoAcimaDe500_DeveAplicarDescontoDe10PorCento()
    {
        var calculadora = CriarCalculadora();

        var resultado = calculadora.Calcular(
            600m, "cliente@exemplo.com");

        Assert.Equal(540m, resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ClienteVip_DeveAplicarDescontoDe20PorCento()
    {
        var calculadora = CriarCalculadora();

        var resultado = calculadora.Calcular(
            200m, "clientevip@exemplo.com");

        Assert.Equal(160m, resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void PedidoDe500_NaoDeveAplicarDescontoValorAlto()
    {
        var calculadora = CriarCalculadora();

        var resultado = calculadora.Calcular(
            500m, "cliente@exemplo.com");

        Assert.Equal(500m, resultado);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void PedidoAltoDeClienteVip_DeveSeguirPrioridadeAtual()
    {
        var calculadora = CriarCalculadora();

        var resultado = calculadora.Calcular(
            600m, "clientevip@exemplo.com");

        // A regra de valor alto é avaliada primeiro.
        Assert.Equal(540m, resultado);
    }

    private static CalculadoraDesconto CriarCalculadora()
    {
        IRegraDesconto[] regras =
        {
            new DescontoValorAlto(),
            new DescontoVip()
        };

        return new CalculadoraDesconto(regras);
    }
}

using Xunit;

namespace EcommerceRefatorado.Tests;

// Testes iniciais só para o pipeline ter o que rodar.
// Durante a refatoração, troque por testes das classes novas
// (CalculadoraDesconto, PedidoService, EstoqueService...).
// O Trait "Category=Unit" é o que o comando `squad test` filtra.
public class SmokeTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Pipeline_Executa_Testes()
    {
        Assert.True(true);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(2, 3, 5)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Soma_Retorna_Valor_Esperado(int a, int b, int esperado)
    {
        Assert.Equal(esperado, a + b);
    }
}

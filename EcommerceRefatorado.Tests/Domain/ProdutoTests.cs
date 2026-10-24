
using EcommerceRefatorado;
using Xunit;

namespace EcommerceRefatorado.Tests.Domain;

public class ProdutoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Reabastecer_DeveAumentarEstoque()
    {
        // Arrange
        var produto = CriarProduto();

        // Act
        produto.Reabastecer(5);

        // Assert
        Assert.Equal(15, produto.Estoque);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-5)]
    public void Reabastecer_QuantidadeInvalida_DeveLancarExcecao(
        int quantidade)
    {
        var produto = CriarProduto();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => produto.Reabastecer(quantidade));

        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DebitarEstoque_DeveReduzirQuantidade()
    {
        var produto = CriarProduto();

        produto.DebitarEstoque(3);

        Assert.Equal(7, produto.Estoque);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DebitarEstoque_Insuficiente_DeveLancarExcecao()
    {
        var produto = CriarProduto();

        Assert.Throws<InvalidOperationException>(
            () => produto.DebitarEstoque(15));

        Assert.Equal(10, produto.Estoque);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-3)]
    public void DebitarEstoque_QuantidadeInvalida_DeveLancarExcecao(
        int quantidade)
    {
        var produto = CriarProduto();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => produto.DebitarEstoque(quantidade));

        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Desativar_DeveMarcarProdutoComoInativo()
    {
        var produto = CriarProduto();

        produto.Desativar();

        Assert.False(produto.Ativo);
        Assert.Equal(10, produto.Estoque);
    }

    private static Produto CriarProduto()
    {
        return new Produto
        {
            Id = 1,
            Nome = "Camiseta",
            Preco = 50m,
            Estoque = 10
        };
    }
}

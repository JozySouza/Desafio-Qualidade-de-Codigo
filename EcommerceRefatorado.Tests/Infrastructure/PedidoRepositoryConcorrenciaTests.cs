using EcommerceRefatorado;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcommerceRefatorado.Tests.Infrastructure;

public class PedidoRepositoryConcorrenciaTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DuasComprasSimultaneas_NaoDevemVenderUltimaUnidadeDuasVezes()
    {
        var arquivoBanco = Path.Combine(
            Path.GetTempPath(),
            $"teste-estoque-{Guid.NewGuid():N}.db");

        var connectionString =
            $"Data Source={arquivoBanco};Default Timeout=15";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        try
        {
            int produtoId;

            // Cria um banco SQLite real com apenas uma unidade.
            await using (var contexto = new AppDbContext(options))
            {
                await contexto.Database.EnsureCreatedAsync();

                var produto = new Produto
                {
                    Nome = "Última camiseta",
                    Preco = 50m,
                    Estoque = 1
                };

                contexto.Produtos.Add(produto);

                await contexto.SaveChangesAsync();

                produtoId = produto.Id;
            }

            // Cada compra utiliza sua própria conexão com o banco.
            async Task<bool> TentarComprarAsync()
            {
                await using var contexto = new AppDbContext(options);

                var repositorio = new PedidoRepository(contexto);

                var itens = new List<ItemPedido>
                {
                    new()
                    {
                        ProdutoId = produtoId,
                        Quantidade = 1
                    }
                };

                return await repositorio.ExecutarEmTransacaoAsync(
                    ct => repositorio.ReservarEstoqueAsync(itens, ct));
            }

            // Prepara duas tentativas de compra simultâneas.
            var inicio = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> ExecutarCompraAsync()
            {
                await inicio.Task;

                return await TentarComprarAsync();
            }

            var compra1 = Task.Run(ExecutarCompraAsync);
            var compra2 = Task.Run(ExecutarCompraAsync);

            // Libera as duas tentativas ao mesmo tempo.
            inicio.SetResult();

            var resultados = await Task.WhenAll(compra1, compra2);

            // Apenas uma compra pode reservar a última unidade.
            Assert.Equal(
                1,
                resultados.Count(resultado => resultado));

            // Consulta o estoque diretamente no SQLite.
            await using var verificacao = new AppDbContext(options);

            var estoqueFinal = await verificacao.Produtos
                .Where(produto => produto.Id == produtoId)
                .Select(produto => produto.Estoque)
                .SingleAsync();

            // O estoque deve terminar em zero, nunca negativo.
            Assert.Equal(0, estoqueFinal);
        }
        finally
        {
            // Libera as conexões antes de excluir o banco temporário.
            SqliteConnection.ClearAllPools();

            if (File.Exists(arquivoBanco))
            {
                File.Delete(arquivoBanco);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FalhaDuranteTransacao_DeveRestaurarEstoque()
    {
        var arquivoBanco = Path.Combine(
            Path.GetTempPath(),
            $"teste-rollback-{Guid.NewGuid():N}.db");

        var connectionString =
            $"Data Source={arquivoBanco};Default Timeout=15";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        try
        {
            int produtoId;

            // Cria um banco SQLite real com cinco unidades em estoque.
            await using (var contexto = new AppDbContext(options))
            {
                await contexto.Database.EnsureCreatedAsync();

                var produto = new Produto
                {
                    Nome = "Produto para teste de rollback",
                    Preco = 100m,
                    Estoque = 5
                };

                contexto.Produtos.Add(produto);

                await contexto.SaveChangesAsync();

                produtoId = produto.Id;
            }

            // Inicia a operação de reserva.
            await using (var contexto = new AppDbContext(options))
            {
                var repositorio = new PedidoRepository(contexto);

                var itens = new List<ItemPedido>
                {
                    new()
                    {
                        ProdutoId = produtoId,
                        Quantidade = 2
                    }
                };

                // Esperamos uma exceção durante a transação.
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => repositorio.ExecutarEmTransacaoAsync<bool>(
                        async ct =>
                        {
                            // Reserva duas unidades.
                            var reservado =
                                await repositorio.ReservarEstoqueAsync(
                                    itens,
                                    ct);

                            Assert.True(reservado);

                            // Simula uma falha antes do commit.
                            throw new InvalidOperationException(
                                "Falha simulada durante a transação.");
                        }));
            }

            // Abre uma nova conexão para conferir o estoque.
            await using (var verificacao = new AppDbContext(options))
            {
                var estoqueFinal = await verificacao.Produtos
                    .Where(produto => produto.Id == produtoId)
                    .Select(produto => produto.Estoque)
                    .SingleAsync();

                // O rollback deve restaurar as cinco unidades.
                Assert.Equal(5, estoqueFinal);
            }
        }
        finally
        {
            // Libera as conexões antes de excluir o banco temporário.
            SqliteConnection.ClearAllPools();

            if (File.Exists(arquivoBanco))
            {
                File.Delete(arquivoBanco);
            }
        }
    }
}
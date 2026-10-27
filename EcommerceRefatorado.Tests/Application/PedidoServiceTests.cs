using EcommerceRefatorado;
using EcommerceRefatorado.Interfaces;
using EcommerceRefatorado.Services;
using EcommerceRefatorado.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EcommerceRefatorado.Tests.Application;

public class PedidoServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task PagamentoRecusado_DeveSalvarPedidoFalhouSemDebitarEstoque()
    {
        var pagamento = new FakePagamentoGateway
        {
            PagamentoAprovado = false
        };

        var repositorioPedidos = new PedidoRepositoryFake();
        var service = CriarService(
            pagamento,
            repositorioPedidos,
            new LogisticaGatewayFake());

        var resultado = await service.CriarAsync(CriarRequest());

        Assert.Equal(StatusPedido.Falhou, resultado.Status);
        Assert.Equal(1, pagamento.QuantidadeChamadas);
        Assert.Equal(1, repositorioPedidos.QuantidadeSalvamentos);
        Assert.Equal(StatusPedido.Falhou, repositorioPedidos.PedidoSalvo?.Status);
        Assert.Equal(10, repositorioPedidos.EstoqueNoSalvamento);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EstoqueInsuficiente_NaoDeveProcessarPagamento()
    {
        var pagamento = new FakePagamentoGateway();
        var repositorioPedidos = new PedidoRepositoryFake();

        var service = CriarService(
            pagamento,
            repositorioPedidos,
            new LogisticaGatewayFake(),
            estoque: 1);

        await Assert.ThrowsAsync<DomainException>(
            () => service.CriarAsync(CriarRequest(quantidade: 5)));

        Assert.Equal(0, pagamento.QuantidadeChamadas);
        Assert.Equal(0, repositorioPedidos.QuantidadeSalvamentos);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FalhaLogistica_DeveManterPedidoPago()
    {
        var pagamento = new FakePagamentoGateway();
        var repositorioPedidos = new PedidoRepositoryFake();

        var service = CriarService(
            pagamento,
            repositorioPedidos,
            new LogisticaGatewayFake { DeveFalhar = true });

        var resultado = await service.CriarAsync(CriarRequest());

        Assert.Equal(StatusPedido.Pago, resultado.Status);
        Assert.Null(resultado.CodigoRastreio);
        Assert.Equal(1, repositorioPedidos.QuantidadeSalvamentos);
        Assert.Equal(0, repositorioPedidos.QuantidadeAtualizacoes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FalhaAoAtualizarPedido_DevePropagarExcecao()
    {
        // Arrange
        var pagamento = new FakePagamentoGateway();

        var repositorioPedidos = new PedidoRepositoryFake
        {
            FalharNaAtualizacao = true
        };

        var service = CriarService(
            pagamento,
            repositorioPedidos,
            new LogisticaGatewayFake());

        // Act e Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CriarAsync(CriarRequest()));

        Assert.Equal(1, repositorioPedidos.QuantidadeSalvamentos);
        Assert.Equal(1, repositorioPedidos.QuantidadeAtualizacoes);
    }

    private static PedidoRequest CriarRequest(int quantidade = 2)
    {
        return new PedidoRequest
        {
            ClienteNome = "Cliente Teste",
            ClienteEmail = "cliente@exemplo.com",
            FormaPagamento = FormaPagamento.Pix,
            Itens = new List<ItemPedidoRequest>
            {
                new()
                {
                    ProdutoId = 1,
                    Quantidade = quantidade
                }
            }
        };
    }

    private static PedidoService CriarService(
        FakePagamentoGateway pagamento,
        PedidoRepositoryFake repositorioPedidos,
        LogisticaGatewayFake logistica,
        int estoque = 10)
    {
        var produto = new Produto
        {
            Id = 1,
            Nome = "Camiseta",
            Preco = 50m,
            Estoque = estoque
        };

        var repositorioProdutos = new ProdutoRepositoryFake(produto);

        var calculadora = new CalculadoraDesconto(
            Array.Empty<IRegraDesconto>());

        return new PedidoService(
            repositorioPedidos,
            repositorioProdutos,
            pagamento,
            logistica,
            calculadora,
            NullLogger<PedidoService>.Instance);
    }

    private sealed class ProdutoRepositoryFake(Produto produto)
        : IProdutoRepository
    {
        public Task<Produto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Produto?>(
                id == produto.Id ? produto : null);
        }

        public Task<IReadOnlyList<Produto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Produto> produtos = new[] { produto };
            return Task.FromResult(produtos);
        }

        public Task AddAsync(
            Produto produto,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(
            Produto produto,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PedidoRepositoryFake : IPedidoRepository
    {
        public int QuantidadeSalvamentos { get; private set; }
        public int QuantidadeAtualizacoes { get; private set; }
        public Pedido? PedidoSalvo { get; private set; }
        public int? EstoqueNoSalvamento { get; private set; }

        public bool FalharNaAtualizacao { get; set; }

        public Task AddWithStockAsync(
            Pedido pedido,
            IReadOnlyCollection<Produto> produtos,
            CancellationToken cancellationToken = default)
        {
            QuantidadeSalvamentos++;
            PedidoSalvo = pedido;
            EstoqueNoSalvamento = produtos.Single().Estoque;
            pedido.Id = 1;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Pedido pedido,
            CancellationToken cancellationToken = default)
        {
            QuantidadeAtualizacoes++;

            if (FalharNaAtualizacao)
            {
                throw new InvalidOperationException(
                    "Falha ao atualizar pedido no banco.");
            }

            return Task.CompletedTask;
        }

        public Task AddAsync(
            Pedido pedido,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Pedido?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Pedido?>(null);

        public Task<IReadOnlyList<Pedido>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Pedido> pedidos = Array.Empty<Pedido>();
            return Task.FromResult(pedidos);
        }

        public Task<bool> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class LogisticaGatewayFake : ILogisticaGateway
    {
        public bool DeveFalhar { get; set; }

        public Task<string> EnviarAsync(
            Pedido pedido,
            CancellationToken cancellationToken = default)
        {
            if (DeveFalhar)
            {
                throw new InvalidOperationException(
                    "Transportadora indisponível.");
            }

            return Task.FromResult("BR123456789012345678");
        }
    }
}

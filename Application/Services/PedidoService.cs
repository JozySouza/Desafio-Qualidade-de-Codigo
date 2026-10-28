using EcommerceRefatorado.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceRefatorado.Services;

public sealed class PedidoService(
    IPedidoRepository pedidoRepository,
    IProdutoRepository produtoRepository,
    IPagamentoGateway pagamentoGateway,
    ILogisticaGateway logisticaGateway,
    CalculadoraDesconto calculadoraDesconto,
    ILogger<PedidoService> logger) : IPedidoService
{
    public async Task<PedidoResponse> CriarAsync(
        PedidoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Itens.Count == 0)
        {
            throw new DomainException("Pedido sem itens.");
        }

        if (request.FormaPagamento == FormaPagamento.NaoInformado)
        {
            throw new DomainException("Informe a forma de pagamento.");
        }

        var itensAgrupados = request.Itens
            .GroupBy(item => item.ProdutoId)
            .Select(grupo => new
            {
                ProdutoId = grupo.Key,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .ToList();

        var produtos = new List<Produto>(itensAgrupados.Count);
        var itensPedido = new List<ItemPedido>(itensAgrupados.Count);

        foreach (var itemRequest in itensAgrupados)
        {
            var produto = await produtoRepository.GetByIdAsync(
                itemRequest.ProdutoId,
                cancellationToken);

            if (produto is null)
            {
                throw new DomainException(
                    $"Produto {itemRequest.ProdutoId} não encontrado.");
            }

            if (!produto.Ativo)
            {
                throw new DomainException(
                    $"Produto {produto.Nome} está inativo.");
            }

            if (produto.Estoque < itemRequest.Quantidade)
            {
                throw new DomainException(
                    $"Estoque insuficiente para {produto.Nome}.");
            }

            produtos.Add(produto);

            itensPedido.Add(new ItemPedido
            {
                ProdutoId = produto.Id,
                Produto = produto,
                Quantidade = itemRequest.Quantidade,
                PrecoUnitario = produto.Preco
            });
        }

        var subtotal = itensPedido.Sum(
            item => item.PrecoUnitario * item.Quantidade);

        var total = calculadoraDesconto.Calcular(
            subtotal,
            request.ClienteEmail);

        var pedido = new Pedido
        {
            ClienteNome = request.ClienteNome.Trim(),
            ClienteEmail = request.ClienteEmail.Trim(),
            FormaPagamento = request.FormaPagamento,
            Status = StatusPedido.Pendente,
            ValorTotal = total,
            Itens = itensPedido
        };

        // Registra o pedido antes de chamar o gateway de pagamento.
        await pedidoRepository.AddPendingAsync(
            pedido,
            produtos,
            cancellationToken);

        // Assim, uma falha ao registrar o pedido não resulta em cobrança.
        await ProcessarPagamentoAsync(pedido, cancellationToken);

        if (pedido.Status == StatusPedido.Falhou)
        {
            // Mantém a recusa registrada para permitir o reprocessamento.
            await pedidoRepository.UpdateAsync(pedido, cancellationToken);
            return Mapear(pedido);
        }

        // Persiste o pagamento aprovado e a baixa de estoque na mesma transação.
        await pedidoRepository.UpdateWithStockAsync(
            pedido,
            produtos,
            cancellationToken);

        // A logística só recebe pedidos já persistidos como pagos.
        await EnviarParaLogisticaAsync(pedido, cancellationToken);

        return Mapear(pedido);
    }

    public async Task<PedidoResponse?> ReprocessarPagamentoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await pedidoRepository.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
        {
            return null;
        }

        if (pedido.Status != StatusPedido.Falhou)
        {
            throw new DomainException(
                "Somente pedidos com pagamento falho podem ser reprocessados.");
        }

        await ProcessarPagamentoAsync(pedido, cancellationToken);

        var produtos = pedido.Itens
            .Select(item => item.Produto)
            .DistinctBy(produto => produto.Id)
            .ToList();

        await pedidoRepository.UpdateWithStockAsync(
            pedido,
            produtos,
            cancellationToken);

        if (pedido.Status == StatusPedido.Pago)
        {
            await EnviarParaLogisticaAsync(pedido, cancellationToken);
        }

        return Mapear(pedido);
    }

    public async Task<PedidoResponse?> ReenviarLogisticaAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await pedidoRepository.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
        {
            return null;
        }

        if (pedido.Status is not (StatusPedido.Pago or StatusPedido.Enviado))
        {
            throw new DomainException(
                "Somente pedidos pagos podem ser enviados à logística.");
        }

        await EnviarParaLogisticaAsync(pedido, cancellationToken);
        return Mapear(pedido);
    }

    public async Task<IReadOnlyList<PedidoResponse>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        var pedidos = await pedidoRepository.GetAllAsync(
            cancellationToken);

        return pedidos.Select(Mapear).ToList();
    }

    public async Task<PedidoResponse?> BuscarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await pedidoRepository.GetByIdAsync(
            id,
            cancellationToken);

        return pedido is null ? null : Mapear(pedido);
    }

    private async Task ProcessarPagamentoAsync(
        Pedido pedido,
        CancellationToken cancellationToken)
    {
        var itensPorProduto = pedido.Itens
            .GroupBy(item => item.ProdutoId)
            .Select(grupo => new
            {
                Produto = grupo.First().Produto,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .ToList();

        foreach (var item in itensPorProduto)
        {
            if (!item.Produto.Ativo)
            {
                throw new DomainException(
                    $"Produto {item.Produto.Nome} está inativo.");
            }

            if (item.Produto.Estoque < item.Quantidade)
            {
                throw new DomainException(
                    $"Estoque insuficiente para {item.Produto.Nome}.");
            }
        }

        var pagamentoAprovado = await pagamentoGateway.ProcessarAsync(
            pedido.ValorTotal,
            pedido.FormaPagamento,
            cancellationToken);

        if (!pagamentoAprovado)
        {
            pedido.Status = StatusPedido.Falhou;
            return;
        }

        // O estoque só é debitado após aprovação do pagamento.
        foreach (var item in itensPorProduto)
        {
            item.Produto.DebitarEstoque(item.Quantidade);
        }

        pedido.Status = StatusPedido.Pago;
    }

    private async Task EnviarParaLogisticaAsync(
        Pedido pedido,
        CancellationToken cancellationToken)
    {
        string? codigoRastreio;

        try
        {
            codigoRastreio = await logisticaGateway.EnviarAsync(
                pedido,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha na logística do pedido {PedidoId}. Pedido permanece pago.",
                pedido.Id);
            return;
        }

        if (codigoRastreio is null)
        {
            return;
        }

        pedido.CodigoRastreio = codigoRastreio;
        pedido.Status = StatusPedido.Enviado;

        await pedidoRepository.UpdateAsync(pedido, cancellationToken);

        logger.LogInformation(
            "Pedido {PedidoId} enviado com sucesso. Código: {CodigoRastreio}",
            pedido.Id,
            codigoRastreio);
    }

    private static PedidoResponse Mapear(Pedido pedido)
    {
        var itens = pedido.Itens
            .Select(item => new ItemPedidoResponse(
                item.ProdutoId,
                item.Produto.Nome,
                item.Quantidade,
                item.PrecoUnitario,
                item.PrecoUnitario * item.Quantidade))
            .ToList();

        return new PedidoResponse(
            pedido.Id,
            pedido.ClienteNome,
            pedido.ClienteEmail,
            pedido.DataCriacao,
            pedido.FormaPagamento,
            pedido.Status,
            pedido.ValorTotal,
            pedido.CodigoRastreio,
            itens);
    }
}

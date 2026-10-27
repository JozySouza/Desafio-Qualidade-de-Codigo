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

        // Valida o estoque antes do pagamento, mas só o altera após aprovação.
        var pagamentoAprovado = await pagamentoGateway.ProcessarAsync(
            total,
            request.FormaPagamento,
            cancellationToken);

        if (pagamentoAprovado)
        {
            foreach (var item in itensPedido)
            {
                item.Produto.DebitarEstoque(item.Quantidade);
            }
        }

        var pedido = new Pedido
        {
            ClienteNome = request.ClienteNome.Trim(),
            ClienteEmail = request.ClienteEmail.Trim(),
            FormaPagamento = request.FormaPagamento,
            Status = pagamentoAprovado
                ? StatusPedido.Pago
                : StatusPedido.Falhou,
            ValorTotal = total,
            Itens = itensPedido
        };

        // Salva o pedido e a baixa de estoque na mesma transação.
        await pedidoRepository.AddWithStockAsync(
            pedido,
            produtos,
            cancellationToken);

        if (!pagamentoAprovado)
        {
            return Mapear(pedido);
        }

        // Tenta realizar o envio logístico somente após pagamento aprovado.
        string? codigoRastreio = null;

        try
        {
            codigoRastreio = await logisticaGateway.EnviarAsync(
                pedido,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Respeita o cancelamento da requisição.
            throw;
        }
        catch (Exception ex)
        {
            // O pedido já foi pago e salvo.
            // Registra a falha sem desfazer o pagamento.
            logger.LogError(
                ex,
                "Falha na logística do pedido {PedidoId}. Pedido permanece pago.",
                pedido.Id);
        }

        // Atualiza o pedido somente se a logística retornar um código.
        // Erros de persistência não são tratados como falhas logísticas.
        if (codigoRastreio is not null)
        {
            pedido.CodigoRastreio = codigoRastreio;
            pedido.Status = StatusPedido.Enviado;

            await pedidoRepository.UpdateAsync(
                pedido,
                cancellationToken);

            logger.LogInformation(
                "Pedido {PedidoId} enviado com sucesso. Código: {CodigoRastreio}",
                pedido.Id,
                codigoRastreio);
        }

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

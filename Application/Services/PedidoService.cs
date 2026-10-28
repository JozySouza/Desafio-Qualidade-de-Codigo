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
    private enum ResultadoProcessamento
    {
        Aprovado,
        Recusado,
        EstoqueInsuficiente,
        ReprocessamentoIndisponivel,
        ErroPagamento
    }

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

            if (itemRequest.Quantidade <= 0)
            {
                throw new DomainException(
                    $"Quantidade inválida para {produto.Nome}.");
            }

            // A validação inicial evita pedidos inválidos.
            // A proteção contra concorrência acontece na reserva.
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

        // Registra o pedido antes do processamento do pagamento.
        await pedidoRepository.AddPendingAsync(
            pedido,
            produtos,
            cancellationToken);

        // Reserva, pagamento simulado e atualização do pedido
        // acontecem dentro da mesma transação SQLite.
        var resultado = await pedidoRepository.ExecutarEmTransacaoAsync(
            ct => ProcessarPagamentoAsync(pedido, ct),
            cancellationToken);

        if (resultado == ResultadoProcessamento.EstoqueInsuficiente)
        {
            throw new DomainException(
                "Estoque insuficiente para concluir o pedido.");
        }

        if (resultado == ResultadoProcessamento.Recusado)
        {
            return Mapear(pedido);
        }

        // O erro só é informado depois que a transação
        // registra a falha e libera o estoque.
        if (resultado == ResultadoProcessamento.ErroPagamento)
        {
            throw new InvalidOperationException(
                "Não foi possível processar o pagamento. " +
                "O pedido foi marcado como falho.");
        }

        // A logística só recebe pedidos com pagamento confirmado.
        await EnviarParaLogisticaAsync(
            pedido,
            cancellationToken);

        return Mapear(pedido);
    }

    public async Task<PedidoResponse?> ReprocessarPagamentoAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await pedidoRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (pedido is null)
        {
            return null;
        }

        if (pedido.Status != StatusPedido.Falhou)
        {
            throw new DomainException(
                "Somente pedidos com pagamento falho podem ser reprocessados.");
        }

        var resultado = await pedidoRepository.ExecutarEmTransacaoAsync(
            async ct =>
            {
                // Impede que duas requisições iniciem o
                // reprocessamento do mesmo pedido.
                var iniciou = await pedidoRepository
                    .TentarIniciarReprocessamentoAsync(id, ct);

                if (!iniciou)
                {
                    return ResultadoProcessamento.ReprocessamentoIndisponivel;
                }

                return await ProcessarPagamentoAsync(pedido, ct);
            },
            cancellationToken);

        if (resultado == ResultadoProcessamento.ReprocessamentoIndisponivel)
        {
            throw new DomainException(
                "Este pedido não está disponível para reprocessamento.");
        }

        if (resultado == ResultadoProcessamento.EstoqueInsuficiente)
        {
            throw new DomainException(
                "Estoque insuficiente para reprocessar o pedido.");
        }

        if (resultado == ResultadoProcessamento.ErroPagamento)
        {
            throw new InvalidOperationException(
                "Não foi possível reprocessar o pagamento. " +
                "O pedido permanece como falho.");
        }

        if (resultado == ResultadoProcessamento.Aprovado)
        {
            await EnviarParaLogisticaAsync(
                pedido,
                cancellationToken);
        }

        return Mapear(pedido);
    }

    public async Task<PedidoResponse?> ReenviarLogisticaAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pedido = await pedidoRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (pedido is null)
        {
            return null;
        }

        if (pedido.Status is not
            (StatusPedido.Pago or StatusPedido.Enviado))
        {
            throw new DomainException(
                "Somente pedidos pagos podem ser enviados à logística.");
        }

        await EnviarParaLogisticaAsync(
            pedido,
            cancellationToken);

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

    private async Task<ResultadoProcessamento> ProcessarPagamentoAsync(
        Pedido pedido,
        CancellationToken cancellationToken)
    {
        // A reserva é feita diretamente no SQLite.
        // A verificação e a redução do estoque são atômicas.
        var estoqueReservado =
            await pedidoRepository.ReservarEstoqueAsync(
                pedido.Itens.ToList(),
                cancellationToken);

        if (!estoqueReservado)
        {
            pedido.Status = StatusPedido.Falhou;

            await pedidoRepository.UpdateAsync(
                pedido,
                cancellationToken);

            return ResultadoProcessamento.EstoqueInsuficiente;
        }

        bool pagamentoAprovado;

        try
        {
            // O pagamento simulado é processado
            // depois que o estoque foi reservado.
            pagamentoAprovado =
                await pagamentoGateway.ProcessarAsync(
                    pedido.ValorTotal,
                    pedido.FormaPagamento,
                    cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Um cancelamento solicitado deve interromper
            // a operação e provocar rollback da transação.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar pagamento do pedido {PedidoId}.",
                pedido.Id);

            // O gateway simulado apresentou um erro.
            // Devolve as unidades reservadas.
            await pedidoRepository.LiberarEstoqueAsync(
                pedido.Itens.ToList(),
                CancellationToken.None);

            // Registra a falha para permitir
            // um novo processamento posteriormente.
            pedido.Status = StatusPedido.Falhou;

            await pedidoRepository.UpdateAsync(
                pedido,
                CancellationToken.None);

            return ResultadoProcessamento.ErroPagamento;
        }

        if (!pagamentoAprovado)
        {
            // Pagamento recusado: devolve o estoque reservado.
            await pedidoRepository.LiberarEstoqueAsync(
                pedido.Itens.ToList(),
                cancellationToken);

            pedido.Status = StatusPedido.Falhou;

            await pedidoRepository.UpdateAsync(
                pedido,
                cancellationToken);

            return ResultadoProcessamento.Recusado;
        }

        // O estoque já foi reduzido durante a reserva.
        // Confirmamos apenas o status do pedido.
        pedido.Status = StatusPedido.Pago;

        await pedidoRepository.UpdateAsync(
            pedido,
            cancellationToken);

        return ResultadoProcessamento.Aprovado;
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
                "Falha na logística do pedido {PedidoId}. " +
                "Pedido permanece pago.",
                pedido.Id);

            return;
        }

        if (codigoRastreio is null)
        {
            return;
        }

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
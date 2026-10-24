using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado.Services;

public sealed class PedidoService(
	IPedidoRepository pedidoRepository,
	IProdutoRepository produtoRepository,
	IPagamentoGateway pagamentoGateway,
	ILogisticaGateway logisticaGateway,
	CalculadoraDesconto calculadoraDesconto) : IPedidoService
{
	public async Task<PedidoResponse> CriarAsync(PedidoRequest request, CancellationToken cancellationToken = default)
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
			.Select(grupo => new { ProdutoId = grupo.Key, Quantidade = grupo.Sum(item => item.Quantidade) })
			.ToList();
		var produtos = new List<Produto>(itensAgrupados.Count);
		var itensPedido = new List<ItemPedido>(itensAgrupados.Count);

		foreach (var itemRequest in itensAgrupados)
		{
			var produto = await produtoRepository.GetByIdAsync(itemRequest.ProdutoId, cancellationToken);
			if (produto is null)
			{
				throw new DomainException($"Produto {itemRequest.ProdutoId} não encontrado.");
			}

			if (!produto.Ativo)
			{
				throw new DomainException($"Produto {produto.Nome} está inativo.");
			}

			if (produto.Estoque < itemRequest.Quantidade)
			{
				throw new DomainException($"Estoque insuficiente para {produto.Nome}.");
			}

			produto.DebitarEstoque(itemRequest.Quantidade);
			produtos.Add(produto);
			itensPedido.Add(new ItemPedido
			{
				ProdutoId = produto.Id,
				Produto = produto,
				Quantidade = itemRequest.Quantidade,
				PrecoUnitario = produto.Preco
			});
		}

		var subtotal = itensPedido.Sum(item => item.PrecoUnitario * item.Quantidade);
		var total = calculadoraDesconto.Calcular(subtotal, request.ClienteEmail);
		var pagamentoAprovado = await pagamentoGateway.ProcessarAsync(total, request.FormaPagamento, cancellationToken);
		if (!pagamentoAprovado)
		{
			throw new DomainException("Pagamento recusado.");
		}

		var pedido = new Pedido
		{
			ClienteNome = request.ClienteNome.Trim(),
			ClienteEmail = request.ClienteEmail.Trim(),
			FormaPagamento = request.FormaPagamento,
			Status = StatusPedido.Pago,
			ValorTotal = total,
			Itens = itensPedido
		};

		await pedidoRepository.AddWithStockAsync(pedido, produtos, cancellationToken);
		pedido.CodigoRastreio = await logisticaGateway.EnviarAsync(pedido, cancellationToken);
		pedido.Status = StatusPedido.Enviado;
		await pedidoRepository.UpdateAsync(pedido, cancellationToken);

		return Mapear(pedido);
	}

	public async Task<IReadOnlyList<PedidoResponse>> ListarAsync(CancellationToken cancellationToken = default)
	{
		var pedidos = await pedidoRepository.GetAllAsync(cancellationToken);
		return pedidos.Select(Mapear).ToList();
	}

	public async Task<PedidoResponse?> BuscarAsync(int id, CancellationToken cancellationToken = default)
	{
		var pedido = await pedidoRepository.GetByIdAsync(id, cancellationToken);
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

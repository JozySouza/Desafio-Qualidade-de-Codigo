namespace EcommerceRefatorado;

public sealed record PedidoResponse(
	int Id,
	string ClienteNome,
	string ClienteEmail,
	DateTime DataCriacao,
	FormaPagamento FormaPagamento,
	StatusPedido Status,
	decimal ValorTotal,
	string? CodigoRastreio,
	IReadOnlyList<ItemPedidoResponse> Itens);

public sealed record ItemPedidoResponse(
	int ProdutoId,
	string ProdutoNome,
	int Quantidade,
	decimal PrecoUnitario,
	decimal Subtotal);

using System.ComponentModel.DataAnnotations;

namespace EcommerceRefatorado;

public sealed class PedidoRequest
{
	[Required]
	[StringLength(150, MinimumLength = 1)]
	public string ClienteNome { get; init; } = string.Empty;

	[Required]
	[EmailAddress]
	[StringLength(254)]
	public string ClienteEmail { get; init; } = string.Empty;

	[EnumDataType(typeof(FormaPagamento))]
	public FormaPagamento FormaPagamento { get; init; }

	[Required]
	[MinLength(1)]
	public IReadOnlyList<ItemPedidoRequest> Itens { get; init; } = [];
}

public sealed class ItemPedidoRequest
{
	[Range(1, int.MaxValue)]
	public int ProdutoId { get; init; }

	[Range(1, int.MaxValue)]
	public int Quantidade { get; init; }
}

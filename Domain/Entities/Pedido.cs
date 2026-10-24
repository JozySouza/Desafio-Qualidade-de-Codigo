namespace EcommerceRefatorado;

public class Pedido
{
    public int Id { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public StatusPedido Status { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public decimal ValorTotal { get; set; }
    public string? CodigoRastreio { get; set; }
    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
}
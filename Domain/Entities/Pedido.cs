namespace EcommerceRefatorado;

public class Pedido
{
    public int Id { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public StatusPedido Status { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
}
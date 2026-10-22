namespace EcommerceRefatorado;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string Descricao { get; internal set; } = string.Empty;

    public bool Ativo { get; private set; } = true;

    public void Reabastecer(int quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        Estoque += quantidade;
    }

    public void Desativar()
    {
        Ativo = false;
    }
}
namespace EcommerceRefatorado;

public sealed record ProdutoResponse(int Id, string Nome, decimal Preco, int Estoque, string Descricao);
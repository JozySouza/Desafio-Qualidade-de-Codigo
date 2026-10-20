using System.ComponentModel.DataAnnotations;

namespace EcommerceRefatorado;

public sealed class ProdutoRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Nome { get; init; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Descricao { get; init; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Preco { get; init; }

    [Range(0, int.MaxValue)]
    public int Estoque { get; init; }
}
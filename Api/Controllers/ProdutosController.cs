using Microsoft.AspNetCore.Mvc;
using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController(IProdutoRepository produtoRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Produto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Produto>>> GetAll(CancellationToken cancellationToken)
    {
        var produtos = await produtoRepository.GetAllAsync(cancellationToken);

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<Produto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Produto>> GetById(int id, CancellationToken cancellationToken)
    {
        var produto = await produtoRepository.GetByIdAsync(id, cancellationToken);

        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType<Produto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Produto>> Create(
        ProdutoRequest request,
        CancellationToken cancellationToken)
    {
        var produto = new Produto
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Preco = request.Preco,
            Estoque = request.Estoque
        };

        await produtoRepository.AddAsync(produto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = produto.Id }, produto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        ProdutoRequest request,
        CancellationToken cancellationToken)
    {
        var produto = await produtoRepository.GetByIdAsync(id, cancellationToken);

        if (produto is null)
        {
            return NotFound();
        }

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Preco = request.Preco;
        produto.Estoque = request.Estoque;

        await produtoRepository.UpdateAsync(produto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await produtoRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
using Microsoft.AspNetCore.Mvc;
using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado;

[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;

    public ProdutosController(IProdutoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProdutoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProdutoResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var produtos = await _service.GetAllAsync(cancellationToken);

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var produto = await _service.GetByIdAsync(id ,cancellationToken);

        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProdutoResponse>> Create(
        ProdutoRequest request,
        CancellationToken cancellationToken)
    {
        var produto = await _service.AddAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = produto.Id }, produto);
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
        var produto = await _service.UpdateAsync(id, request, cancellationToken);

        return produto is null ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id ,cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
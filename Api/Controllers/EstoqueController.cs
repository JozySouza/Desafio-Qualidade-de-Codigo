using EcommerceRefatorado.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceRefatorado.Controllers;

[ApiController]
[Route("api/estoque")]
public class EstoqueController : ControllerBase
{
    private readonly IEstoqueService _service;

    public EstoqueController(IEstoqueService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EstoqueResponse>>> ListarEstoqueAsync(CancellationToken cancellationToken)
    {
        var estoque = await _service.GetAllAsync(cancellationToken);
        return Ok(estoque);
    }

    [HttpPost("reabastecer/{produtoId:int}/{quantidade:int}")]
    public async Task<ActionResult<EstoqueResponse>> ReabastecerEstoque(int produtoId, int quantidade, CancellationToken cancellationToken)
    {
        if (quantidade <= 0) return BadRequest();

        var produto = await _service.ReabastecerAsync(produtoId, quantidade, cancellationToken);
        if (produto == null) return NotFound();

        return Ok(produto);
    }
}
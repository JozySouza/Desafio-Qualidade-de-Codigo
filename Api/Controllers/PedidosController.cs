using Microsoft.AspNetCore.Mvc;
using EcommerceRefatorado.Interfaces;

namespace EcommerceRefatorado;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _service;

    public PedidosController(IPedidoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PedidoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PedidoResponse>>> Listar(
        CancellationToken cancellationToken)
    {
        var pedidos = await _service.ListarAsync(cancellationToken);

        return Ok(pedidos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> Buscar(
        int id,
        CancellationToken cancellationToken)
    {
        var pedido = await _service.BuscarAsync(
            id,
            cancellationToken);

        return pedido is null ? NotFound() : Ok(pedido);
    }

    [HttpPost]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PedidoResponse>> Criar(
        [FromBody] PedidoRequest request,
        CancellationToken cancellationToken)
    {
        var pedido = await _service.CriarAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(Buscar),
            new { id = pedido.Id },
            pedido);
    }
}

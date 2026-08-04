using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StockManufactura.Application.Commands;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class UsuariosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsuariosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioDto>> Create(CreateUsuarioCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
    }
}

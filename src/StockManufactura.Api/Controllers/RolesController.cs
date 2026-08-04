using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StockManufactura.Application.Commands;
using StockManufactura.Application.DTOs;

namespace StockManufactura.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<RolDto>> Create(CreateRolCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
    }
}

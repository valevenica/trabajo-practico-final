using System;
using System.Collections.Generic;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetRolesQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<RolDto>> Create(CreateRolCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}/permissions")]
        public async Task<ActionResult<RolDto>> UpdatePermissions(Guid id, UpdateRolPermissionsCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}

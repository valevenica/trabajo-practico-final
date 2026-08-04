using FluentValidation;
using StockManufactura.Application.Commands;

namespace StockManufactura.Application.Validators
{
    public sealed class CreateRolCommandValidator : AbstractValidator<CreateRolCommand>
    {
        public CreateRolCommandValidator()
        {
            RuleFor(command => command.Nombre)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.Descripcion)
                .MaximumLength(250);
        }
    }
}

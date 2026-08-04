using FluentValidation;
using StockManufactura.Application.Commands;

namespace StockManufactura.Application.Validators
{
    public sealed class CreateUsuarioCommandValidator : AbstractValidator<CreateUsuarioCommand>
    {
        public CreateUsuarioCommandValidator()
        {
            RuleFor(command => command.Nombre)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(250);

            RuleFor(command => command.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(command => command.RolId)
                .NotEmpty();
        }
    }
}

using AutoMapper;
using StockManufactura.Application.DTOs;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Mappings
{
    public sealed class StockManufacturaMappingProfile : Profile
    {
        public StockManufacturaMappingProfile()
        {
            CreateMap<Rol, RolDto>();
            CreateMap<Usuario, UsuarioDto>()
                .ForMember(dest => dest.RolNombre, opt => opt.MapFrom(src => src.Rol.Nombre));
        }
    }
}

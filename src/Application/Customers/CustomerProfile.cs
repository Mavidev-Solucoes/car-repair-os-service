using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Customers;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForCtorParam(nameof(DTOs.CustomerDto.Vehicles), options => options.MapFrom(source => source.Vehicles));
    }
}

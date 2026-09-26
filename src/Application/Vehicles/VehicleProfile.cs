using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Vehicles;

public class VehicleProfile : Profile
{
    public VehicleProfile()
    {
        CreateMap<Vehicle, VehicleDto>();
    }
}

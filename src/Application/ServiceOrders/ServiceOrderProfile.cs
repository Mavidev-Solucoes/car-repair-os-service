using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.ServiceOrders;

public class ServiceOrderProfile : Profile
{
    public ServiceOrderProfile()
    {
        CreateMap<ServiceOrder, ServiceOrderDto>();
        CreateMap<ServiceOrderItem, ServiceOrderItemDto>();
        CreateMap<ServiceStatusHistory, ServiceStatusHistoryDto>();
    }
}

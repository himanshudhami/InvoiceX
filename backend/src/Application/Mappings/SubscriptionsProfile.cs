using Application.DTOs.Subscriptions;
using AutoMapper;
using Core.Entities;

namespace Application.Mappings;

public class SubscriptionsProfile : Profile
{
    public SubscriptionsProfile()
    {
        CreateMap<CreateSubscriptionDto, Subscriptions>();
        CreateMap<UpdateSubscriptionDto, Subscriptions>();
    }
}





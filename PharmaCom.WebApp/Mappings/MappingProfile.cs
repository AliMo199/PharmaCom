using AutoMapper;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.ViewModels;

namespace PharmaCom.WebApp.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Simple mapping for user profile
            CreateMap<ApplicationUser, UserProfileDto>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.MemberSince,
                    opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }

    public class UserProfileDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime MemberSince { get; set; }
    }
}
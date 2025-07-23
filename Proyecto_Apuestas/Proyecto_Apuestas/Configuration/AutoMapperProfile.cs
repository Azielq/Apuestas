using AutoMapper;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Configuration
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // UserAccount mappings
            CreateMap<UserAccount, UserProfileViewModel>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName))
                .ForMember(dest => dest.TotalBets, opt => opt.Ignore()) // Will be set manually in service
                .ForMember(dest => dest.WonBets, opt => opt.Ignore())
                .ForMember(dest => dest.LostBets, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWinnings, opt => opt.Ignore())
                .ForMember(dest => dest.TotalLosses, opt => opt.Ignore())
                .ForMember(dest => dest.WinRate, opt => opt.Ignore());

            CreateMap<UpdateProfileViewModel, UserAccount>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.CreditBalance, opt => opt.Ignore())
                .ForMember(dest => dest.LastBet, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.LockedUntil, opt => opt.Ignore())
                .ForMember(dest => dest.BirthDate, opt => opt.Ignore())
                .ForMember(dest => dest.LoginAttempts, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethods, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentTransactions, opt => opt.Ignore())
                .ForMember(dest => dest.ReportLogs, opt => opt.Ignore())
                .ForMember(dest => dest.Bets, opt => opt.Ignore());

            // Team mappings
            CreateMap<Team, TeamViewModel>()
                .ForMember(dest => dest.SportName, opt => opt.MapFrom(src => src.Sport.Name))
                .ForMember(dest => dest.LogoUrl, opt => opt.MapFrom(src => src.Images.FirstOrDefault().Url));

            // Notification mappings
            CreateMap<Notification, NotificationViewModel>()
                .ForMember(dest => dest.TimeAgo, opt => opt.Ignore()); // Will be calculated in service

            // Event mappings
            CreateMap<Event, EventListViewModel>()
                .ForMember(dest => dest.Teams, opt => opt.Ignore()) // Will be mapped manually in service
                .ForMember(dest => dest.SportName, opt => opt.Ignore()) // Will be mapped manually in service
                .ForMember(dest => dest.CompetitionName, opt => opt.MapFrom(src => "Liga Principal"))
                .ForMember(dest => dest.IsLive, opt => opt.MapFrom(src => src.Date <= DateTime.Now && src.Date >= DateTime.Now.AddHours(-3)))
                .ForMember(dest => dest.IsFinished, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Outcome) || src.Date < DateTime.Now.AddHours(-3)));

            // Bet mappings
            CreateMap<Bet, RecentBetViewModel>()
                .ForMember(dest => dest.EventName, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.TeamName, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GetBetStatusDisplay(src.BetStatus)))
                .ForMember(dest => dest.IsLive, opt => opt.MapFrom(src => src.Event.Date <= DateTime.Now && src.Event.Date >= DateTime.Now.AddHours(-3)));

            // OddsHistory mappings
            CreateMap<OddsHistory, OddsViewModel>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.TeamName))
                .ForMember(dest => dest.Source, opt => opt.MapFrom(src => "Internal"))
                .ForMember(dest => dest.Trend, opt => opt.MapFrom(src => "stable"));
        }

        private static string GetBetStatusDisplay(string status)
        {
            return status switch
            {
                "P" => "Pendiente",
                "W" => "Ganada",
                "L" => "Perdida",
                "C" => "Cancelada",
                _ => "Desconocido"
            };
        }
    }
}
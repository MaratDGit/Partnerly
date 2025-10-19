using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IUserRepository _userRepo;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;
        private readonly IEventBus _eventBus;
        private readonly ISystemSettingsRepository _systemSettingsRepository;   

        public SupportTicketService(ISupportTicketRepository supportTicketRepository, IUserRepository userRepo, ISystemSettingsRepository systemSettingsRepository, IRoleService roleService, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService, IEventBus eventBus)
        {
            _supportTicketRepository = supportTicketRepository;
            _userRepo = userRepo;
            _systemSettingsRepository = systemSettingsRepository;
            _roleService = roleService;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
            _eventBus = eventBus;
        }

        public async Task<SupportTicket?> GetSupportTicketByIDAsync(Guid? id) =>
            await _supportTicketRepository.GetByIdAsync(id);

        public async Task<IEnumerable<SupportTicket?>> GetUserSupportTicketsAsync(Guid? userID) =>
            await _supportTicketRepository.GetUserSupportTicketsAsync(userID);

        public async Task<IEnumerable<SupportTicket?>> GetAllSupportTicketAsync() =>
            await _supportTicketRepository.GetAllAsync();

        public async Task<ServiceResult<SupportTicket?>> CreateSupportTicketAsync(SupportTicket? ticket)
        {
            if (ticket == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket"));
                return ServiceResult<SupportTicket?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket") });
            }

            var newTicket = new SupportTicket { Id = Guid.NewGuid() };

            newTicket.TicketID = await _systemSettingsRepository.GenerateNextCodeAsync("T-");

            newTicket.Subject = ticket.Subject;
            newTicket.Message = ticket.Message;
            newTicket.AssignedTo = ticket.AssignedTo;
            newTicket.Status = SupportTicketStatusAttribute.New;
            newTicket.IsRead = false;
            newTicket.UserId = ticket.UserId;
            if (ticket.AssignedTo == null)
            {
                var superAdmin = await _userRepo.GetByEmailAsync(Constants.SuperUserEmail);
                newTicket.AssignedTo = superAdmin?.Id;
            }

            try
            {
                await _supportTicketRepository.AddAsync(newTicket);
                await _supportTicketRepository.SaveChangesAsync();

                await _eventBus.PublishAsync(new SupportTickedCreatedEvent(newTicket: newTicket, sendEmail: true, sendNote: true));
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserCreated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<SupportTicket?>.Fail(new List<string> { ex.Message });
            }

            return ServiceResult<SupportTicket?>.Ok(newTicket);
        }

        public async Task<ServiceResult<SupportTicket?>> UpdateSupportTicketAsync(SupportTicket? ticket)
        {
            if (ticket == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket"));
                return ServiceResult<SupportTicket?>.Fail(new List<string> { String.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket") });
            }

            if (await _supportTicketRepository.GetByIdAsync(ticket.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "SupportTicket Id"));
                return ServiceResult<SupportTicket?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "SupportTicket Id") });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, ticket))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<SupportTicket?>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            try
            {
                _supportTicketRepository.Update(ticket);
                await _supportTicketRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserUpdated, LogTypeAttribute.Error, ex.Message);
                return ServiceResult<SupportTicket?>.Fail(new List<string> { ex.Message });
            }

            return ServiceResult<SupportTicket?>.Ok(ticket);
        }

        public async Task<ServiceResult<SupportTicket?>> DeleteSupportTicketAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.UserDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket ID"));
                return ServiceResult<SupportTicket?>.Fail(new List<string> { });
            }

            var ticket = await _supportTicketRepository.GetByIdAsync((Guid)id);

            if (ticket == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "SupportTicket"));
                return ServiceResult<SupportTicket?>.Fail(new List<string> { String.Format(ErrorMessages.Cannotbefound, "SupportTicket") });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, ticket))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.SupportTicketDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<SupportTicket?>.Fail(new List<string> { ErrorMessages.NoPermissionForThisAction });
            }

            _supportTicketRepository.Delete(ticket);
            await _supportTicketRepository.SaveChangesAsync();

            return ServiceResult<SupportTicket?>.Ok(ticket);
        }
    }
}

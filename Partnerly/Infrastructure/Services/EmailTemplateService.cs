using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Infrastructure.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IEmailTemplateRepository _templateRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogService _logService;

        public EmailTemplateService(IEmailTemplateRepository templateRepo, IRoleRepository roleRepo, IPermissionService permissionService, ICurrentUserService currentUserService, ILogService logService)
        {
            _templateRepo = templateRepo;
            _roleRepo = roleRepo;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logService = logService;
        }

        public async Task<EmailTemplate?> GetTemplateByIDAsync(Guid? id) =>
            await _templateRepo.GetByIdAsync(id);

        public async Task<EmailTemplate?> GetTemplateByNameAsync(string? name) =>
            await _templateRepo.GetByNameAsync(name);

        public async Task<IEnumerable<EmailTemplate?>> GetAllTemplatesAsync() =>
            await _templateRepo.GetAllAsync();

        public async Task<ServiceResult<EmailTemplate?>> CreateTemplateAsync(EmailTemplate? rec)
        {
            if (rec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role"));
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            var newRec = new EmailTemplate { Id = Guid.NewGuid() };

            newRec.Name = role.Name;
            newRec.Type = role.Type;
            newRec.IsDeleted = false;

            await _templateRepo.AddAsync(newRec);
            await _templateRepo.SaveChangesAsync();

            return ServiceResult<EmailTemplate?>.Ok(newRec);
        }

        public async Task<ServiceResult<EmailTemplate?>> UpdateTemplateAsync(EmailTemplate? rec)
        {
            if (rec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role"));
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            if (await _templateRepo.GetByIdAsync(role.Id) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Role"));
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanUpdateAsync(_currentUserService.UserId, rec))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            _templateRepo.Update(rec);
            await _templateRepo.SaveChangesAsync();

            return ServiceResult<EmailTemplate?>.Ok(rec);
        }

        public async Task<ServiceResult<EmailTemplate?>> DeleteTemplateAsync(Guid? id)
        {
            if (id == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Role ID"));
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            var rec = await _templateRepo.GetByIdAsync(id);
            if (rec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "Role"));
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, rec))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.RoleDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<EmailTemplate?>.Fail(new List<string> { });
            }

            _templateRepo.Delete(rec);
            await _templateRepo.SaveChangesAsync();

            return ServiceResult<EmailTemplate?>.Ok(rec);
        }
    }
}

using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Security.Cryptography;

namespace Partnerly.Infrastructure.Services
{
    public class EmailConfirmationTokenService : IEmailConfirmationTokenService
    {
        private readonly IEmailConfirmationTokenRepository _tokenRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISystemSettingsService _setupServoce;
        private readonly ILogService _logService;

        public EmailConfirmationTokenService(IEmailConfirmationTokenRepository tokenRepo, IRoleRepository roleRepo, IPermissionService permissionService, ICurrentUserService currentUserService, ISystemSettingsService setupServoce, ILogService logService)
        {
            _tokenRepo = tokenRepo;
            _roleRepo = roleRepo;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _setupServoce = setupServoce;
            _logService = logService;
        }

        public async Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(Guid? userID, string? token) =>
            await _tokenRepo.GetByUserIDAndTokenAsync(userID, token);

        public async Task<EmailConfirmationToken?> GetByUserIDAndTokenAsync(string? userID, string? token)
        {
            if (Guid.TryParse(userID, out var guid))
            {
                return await GetByUserIDAndTokenAsync(guid, token);
            }

            return null;
        }

        public async Task<IEnumerable<EmailConfirmationToken?>> GetAllTokensAsync() =>
            await _tokenRepo.GetAllAsync();

        public async Task<ServiceResult<EmailConfirmationToken?>> CreateConfirmationTokenAsync(EmailConfirmationToken? tokenRec)
        {
            if (tokenRec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenCreated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "EmailConfirmationToken"));
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            var setup = await _setupServoce.GetSetupByIDAsync(1);

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes);
            
            int tokenExpiredHousr = 24;
            if (tokenRec.TokenType == EmailTokenTypeAttribute.Registration)
            {
                tokenExpiredHousr = setup?.EmailConfirmationTokenExpiredAtHours ?? 24;
            }
            else if (tokenRec.TokenType == EmailTokenTypeAttribute.ForgotPassword)
            {
                tokenExpiredHousr = setup?.ForgotPasswordTokenExpiredAtHours ?? 1;
            }

            var newtoken = new EmailConfirmationToken();

            newtoken.UserId = tokenRec.UserId;
            newtoken.Token = token;
            newtoken.TokenType = tokenRec.TokenType;
            newtoken.ExpiresAt = DateTime.UtcNow.AddHours(tokenExpiredHousr);
            newtoken.Used = false;
            newtoken.Note = tokenRec.Note;

            await _tokenRepo.AddAsync(newtoken);
            await _tokenRepo.SaveChangesAsync();

            return ServiceResult<EmailConfirmationToken?>.Ok(newtoken);
        }

        public async Task<ServiceResult<EmailConfirmationToken?>> UpdateConfirmationTokenAsync(EmailConfirmationToken? token, Guid? userID = null)
        {
            if (token == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "EmailConfirmationToken"));
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            if (await _tokenRepo.GetByUserIDAndTokenAsync(token.UserId, token.Token) == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenUpdated, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "EmailConfirmationToken"));
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanUpdateAsync(userID ?? _currentUserService.UserId, token))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenUpdated, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            _tokenRepo.Update(token);
            await _tokenRepo.SaveChangesAsync();

            return ServiceResult<EmailConfirmationToken?>.Ok(token);
        }

        public async Task<ServiceResult<EmailConfirmationToken?>> DeleteConfirmationTokenAsync(Guid? userID, string? token)
        {
            if (token == null || userID == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.RecordIsNullFromController, "Token And User"));
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            var tokenRec = await _tokenRepo.GetByUserIDAndTokenAsync(userID, token);
            if (tokenRec == null)
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenDeleted, LogTypeAttribute.Error, String.Format(ErrorMessages.Cannotbefound, "EmailConfirmationToken"));
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            if (!await _permissionService.CanDeleteAsync(_currentUserService.UserId, tokenRec))
            {
                await _logService.CreateLogAsync(LogActionsAttribute.EmailConfirmationTokenDeleted, LogTypeAttribute.Critical, ErrorMessages.NoPermissionForThisAction);
                return ServiceResult<EmailConfirmationToken?>.Fail(new List<string> { });
            }

            _tokenRepo.Delete(tokenRec);
            await _roleRepo.SaveChangesAsync();

            return ServiceResult<EmailConfirmationToken?>.Ok(tokenRec);
        }
    }
}

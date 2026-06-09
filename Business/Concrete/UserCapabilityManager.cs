using Business.Abstract;
using Business.Constants;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs.Capability;

namespace Business.Concrete;

public class UserCapabilityManager : IUserCapabilityService
{
    private readonly IUserCapabilityDal _userCapabilityDal;
    private readonly ICapabilityDal _capabilityDal;
    private readonly ICapabilityAuditService _auditService;
    private readonly IClientContext _clientContext;
    private readonly ICapabilitySnapshot _snapshot;

    public UserCapabilityManager(
        IUserCapabilityDal userCapabilityDal,
        ICapabilityDal capabilityDal,
        ICapabilityAuditService auditService,
        IClientContext clientContext,
        ICapabilitySnapshot snapshot)
    {
        _userCapabilityDal = userCapabilityDal;
        _capabilityDal = capabilityDal;
        _auditService = auditService;
        _clientContext = clientContext;
        _snapshot = snapshot;
    }

    public IDataResult<List<UserCapabilityDto>> GetByUser(int userId, int? institutionId = null, bool includeExpired = false)
    {
        var now = DateTime.UtcNow;
        var query = _userCapabilityDal.GetAll(uc => uc.UserId == userId);

        if (institutionId.HasValue)
            query = query.Where(uc => uc.InstitutionId == institutionId.Value).ToList();

        if (!includeExpired)
            query = query.Where(uc => uc.Status == 1 && (uc.ExpiresAt == null || uc.ExpiresAt > now)).ToList();

        var capIds = query.Select(uc => uc.CapabilityId).Distinct().ToList();
        var caps = _capabilityDal.GetAll(c => capIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        var dtos = query.Select(uc =>
        {
            caps.TryGetValue(uc.CapabilityId, out var cap);
            return new UserCapabilityDto
            {
                Id = uc.Id,
                UserId = uc.UserId,
                CapabilityId = uc.CapabilityId,
                CapabilityCode = cap?.Code ?? string.Empty,
                CapabilityDescription = cap?.Description ?? string.Empty,
                Category = cap?.Category,
                InstitutionId = uc.InstitutionId,
                ScopeJson = uc.ScopeJson,
                ExpiresAt = uc.ExpiresAt,
                GrantedBy = uc.GrantedBy,
                GrantedAt = uc.GrantedAt,
                RevokedAt = uc.RevokedAt,
                Reason = uc.Reason,
                Status = uc.Status,
            };
        }).ToList();

        return new SuccessDataResult<List<UserCapabilityDto>>(dtos);
    }

    public async Task<IResult> GrantAsync(int userId, GrantCapabilityDto dto)
    {
        var cap = _capabilityDal.Get(c => c.Code == dto.CapabilityCode && c.IsActive);
        if (cap == null) return new ErrorResult(Messages.CapabilityNotFound);

        var existing = _userCapabilityDal.Get(uc =>
            uc.UserId == userId &&
            uc.CapabilityId == cap.Id &&
            uc.InstitutionId == dto.InstitutionId &&
            uc.Status == 1);

        if (existing != null) return new ErrorResult(Messages.CapabilityAlreadyGranted);

        var actorId = _clientContext.GetUserId() ?? 0;
        var entry = new UserCapability
        {
            UserId = userId,
            CapabilityId = cap.Id,
            InstitutionId = dto.InstitutionId,
            ScopeJson = dto.ScopeJson,
            ExpiresAt = dto.ExpiresAt,
            GrantedBy = actorId,
            GrantedAt = DateTime.UtcNow,
            Reason = dto.Reason,
            Status = 1,
        };

        _userCapabilityDal.Add(entry);

        _snapshot.AddOrReplace(new UserCapabilitySnapshotEntry(
            entry.Id, userId, cap.Id, cap.Code,
            dto.InstitutionId, dto.ScopeJson, dto.ExpiresAt));

        _auditService.Add(new CapabilityAuditLog
        {
            ActorUserId    = actorId,
            TargetUserId   = userId,
            Action         = "grant",
            CapabilityCode = cap.Code,
            PayloadJson    = $"{{\"code\":\"{cap.Code}\",\"institutionId\":{dto.InstitutionId?.ToString() ?? "null"}}}",
        });

        return new SuccessResult(Messages.CapabilityGranted);
    }

    public async Task<IResult> RevokeBulkAsync(int userId, RevokeBulkDto dto)
    {
        if (dto.CapabilityCodes == null || dto.CapabilityCodes.Count == 0)
            return new ErrorResult("Silinecek yetki listesi boş.");

        var revokedCount = 0;
        var skippedCount = 0;

        foreach (var code in dto.CapabilityCodes)
        {
            var result = await RevokeAsync(userId, new RevokeCapabilityDto
            {
                CapabilityCode = code,
                InstitutionId  = dto.InstitutionId,
                Reason         = dto.Reason,
            });
            if (result.Success) revokedCount++;
            else skippedCount++;
        }

        return new SuccessResult($"{revokedCount} yetki kaldırıldı, {skippedCount} atlandı.");
    }

    public async Task<IResult> RevokeAsync(int userId, RevokeCapabilityDto dto)
    {
        var cap = _capabilityDal.Get(c => c.Code == dto.CapabilityCode && c.IsActive);
        if (cap == null) return new ErrorResult(Messages.CapabilityNotFound);

        var entry = _userCapabilityDal.Get(uc =>
            uc.UserId == userId &&
            uc.CapabilityId == cap.Id &&
            uc.InstitutionId == dto.InstitutionId &&
            uc.Status == 1);

        if (entry == null) return new ErrorResult(Messages.CapabilityNotGranted);

        entry.Status = 2;
        entry.RevokedAt = DateTime.UtcNow;
        entry.Reason = dto.Reason ?? entry.Reason;

        _userCapabilityDal.Update(entry);
        _snapshot.Remove(entry.Id);

        var actorId = _clientContext.GetUserId() ?? 0;
        _auditService.Add(new CapabilityAuditLog
        {
            ActorUserId    = actorId,
            TargetUserId   = userId,
            Action         = "revoke",
            CapabilityCode = cap.Code,
            PayloadJson    = $"{{\"code\":\"{cap.Code}\",\"institutionId\":{dto.InstitutionId?.ToString() ?? "null"}}}",
        });

        return new SuccessResult(Messages.CapabilityRevoked);
    }
}

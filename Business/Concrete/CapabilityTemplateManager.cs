using Business.Abstract;
using Business.Constants;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs.Capability;

namespace Business.Concrete;

public class CapabilityTemplateManager : ICapabilityTemplateService
{
    private readonly ICapabilityTemplateDal _templateDal;
    private readonly ITemplateVersionDal _versionDal;
    private readonly ITemplateItemDal _itemDal;
    private readonly ICapabilityDal _capabilityDal;
    private readonly IUserCapabilityService _userCapabilityService;
    private readonly IClientContext _clientContext;

    public CapabilityTemplateManager(
        ICapabilityTemplateDal templateDal,
        ITemplateVersionDal versionDal,
        ITemplateItemDal itemDal,
        ICapabilityDal capabilityDal,
        IUserCapabilityService userCapabilityService,
        IClientContext clientContext)
    {
        _templateDal = templateDal;
        _versionDal = versionDal;
        _itemDal = itemDal;
        _capabilityDal = capabilityDal;
        _userCapabilityService = userCapabilityService;
        _clientContext = clientContext;
    }

    // ── QUERY ────────────────────────────────────────────────────────────────

    public IDataResult<List<CapabilityTemplateDto>> GetAll()
    {
        var templates = _templateDal.GetAll();
        var dtos = templates.Select(t => ToDto(t)).ToList();

        // LatestVersion bilgisini zenginleştir
        foreach (var dto in dtos)
        {
            var latestVersion = _versionDal
                .GetAll(v => v.TemplateId == dto.Id && v.IsPublished)
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();

            if (latestVersion != null)
                dto.LatestVersion = BuildVersionDto(latestVersion, includeItems: true);
        }

        return new SuccessDataResult<List<CapabilityTemplateDto>>(dtos);
    }

    public IDataResult<CapabilityTemplateDto> GetById(int id)
    {
        var tmpl = _templateDal.Get(t => t.Id == id);
        if (tmpl == null)
            return new ErrorDataResult<CapabilityTemplateDto>(default!, "Şablon bulunamadı.");

        var dto = ToDto(tmpl);
        var latestVersion = _versionDal
            .GetAll(v => v.TemplateId == id && v.IsPublished)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (latestVersion != null)
            dto.LatestVersion = BuildVersionDto(latestVersion, includeItems: true);

        return new SuccessDataResult<CapabilityTemplateDto>(dto);
    }

    public IDataResult<List<TemplateVersionDto>> GetVersions(int templateId)
    {
        var versions = _versionDal
            .GetAll(v => v.TemplateId == templateId)
            .OrderBy(v => v.Version)
            .ToList();

        var dtos = versions.Select(v => BuildVersionDto(v, includeItems: true)).ToList();
        return new SuccessDataResult<List<TemplateVersionDto>>(dtos);
    }

    // ── COMMAND ──────────────────────────────────────────────────────────────

    public IResult Create(CreateTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new ErrorResult("Şablon adı zorunludur.");

        if (!dto.CapabilityCodes.Any())
            return new ErrorResult("En az bir yetki kodu seçilmelidir.");

        var existing = _templateDal.Get(t => t.Name == dto.Name);
        if (existing != null)
            return new ErrorResult("Bu isimde bir şablon zaten var.");

        var actorId = _clientContext.GetUserId() ?? 0;

        // Template oluştur
        var template = new CapabilityTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Status = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorId,
        };
        _templateDal.Add(template);

        // İlk versiyon (taslak — yayımlanmamış)
        var version = new TemplateVersion
        {
            TemplateId = template.Id,
            Version = 1,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorId,
        };
        _versionDal.Add(version);

        // Items
        AddItemsToVersion(version.Id, dto.CapabilityCodes);

        return new SuccessResult("Şablon oluşturuldu.");
    }

    public IResult PublishVersion(int templateId, PublishTemplateVersionDto dto)
    {
        var template = _templateDal.Get(t => t.Id == templateId && t.Status == 1);
        if (template == null)
            return new ErrorResult("Şablon bulunamadı veya pasif.");

        if (!dto.CapabilityCodes.Any())
            return new ErrorResult("En az bir yetki kodu seçilmelidir.");

        var actorId = _clientContext.GetUserId() ?? 0;
        var now = DateTime.UtcNow;

        // Önceki aktif versiyonu bul (opsiyonel — sadece bilgi amaçlı)
        var maxVersion = _versionDal
            .GetAll(v => v.TemplateId == templateId)
            .Select(v => v.Version)
            .DefaultIfEmpty(0)
            .Max();

        // Yeni versiyon
        var version = new TemplateVersion
        {
            TemplateId = templateId,
            Version = maxVersion + 1,
            IsPublished = true,
            PublishedAt = now,
            ChangeNote = dto.ChangeNote,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        _versionDal.Add(version);

        // Items
        AddItemsToVersion(version.Id, dto.CapabilityCodes);

        return new SuccessResult($"Versiyon {version.Version} yayımlandı.");
    }

    public async Task<IResult> ApplyAsync(int templateId, ApplyTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return new ErrorResult("Gerekçe zorunludur.");

        if (!dto.UserIds.Any())
            return new ErrorResult("En az bir kullanıcı ID'si gereklidir.");

        // Versiyonu bul
        var version = _versionDal.Get(v => v.Id == dto.TemplateVersionId && v.IsPublished);
        if (version == null)
            return new ErrorResult("Yayımlanmış versiyon bulunamadı.");

        // Template aktif mi?
        var template = _templateDal.Get(t => t.Id == templateId && t.Status == 1);
        if (template == null)
            return new ErrorResult("Şablon bulunamadı veya pasif.");

        // Items (capability kodları)
        var items = _itemDal.GetAll(i => i.TemplateVersionId == dto.TemplateVersionId);
        var capIds = items.Select(i => i.CapabilityId).ToList();
        var caps = _capabilityDal.GetAll(c => capIds.Contains(c.Id) && c.IsActive)
            .ToDictionary(c => c.Id);

        var appliedCount = 0;
        var skippedCount = 0;

        foreach (var userId in dto.UserIds)
        {
            foreach (var item in items)
            {
                if (!caps.TryGetValue(item.CapabilityId, out var cap))
                    continue;

                var grantDto = new GrantCapabilityDto
                {
                    CapabilityCode = cap.Code,
                    InstitutionId = dto.InstitutionId,
                    ScopeJson = item.ScopeJson,
                    ExpiresAt = dto.ExpiresAt,
                    Reason = $"template_apply:{templateId}:v{version.Version} — {dto.Reason}",
                };

                var result = await _userCapabilityService.GrantAsync(userId, grantDto);
                if (result.Success) appliedCount++;
                else skippedCount++;
            }
        }

        return new SuccessResult(
            $"Şablon uygulandı. {appliedCount} grant eklendi, {skippedCount} zaten vardı (atlandı).");
    }

    public IResult Deactivate(int id)
    {
        var template = _templateDal.Get(t => t.Id == id);
        if (template == null)
            return new ErrorResult("Şablon bulunamadı.");

        template.Status = 2;
        _templateDal.Update(template);
        return new SuccessResult("Şablon pasifleştirildi.");
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private static CapabilityTemplateDto ToDto(CapabilityTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        IsActive = t.Status == 1,
        CreatedAt = t.CreatedAt,
    };

    private TemplateVersionDto BuildVersionDto(TemplateVersion v, bool includeItems)
    {
        var dto = new TemplateVersionDto
        {
            Id = v.Id,
            TemplateId = v.TemplateId,
            Version = v.Version,
            IsPublished = v.IsPublished,
            ChangeNote = v.ChangeNote,
            PublishedAt = v.PublishedAt ?? (v.IsPublished ? v.CreatedAt : null),
        };

        if (includeItems)
        {
            var items = _itemDal.GetAll(i => i.TemplateVersionId == v.Id);
            var capIds = items.Select(i => i.CapabilityId).ToList();
            var caps = _capabilityDal.GetAll(c => capIds.Contains(c.Id))
                .ToDictionary(c => c.Id);

            dto.Items = items.Select(i =>
            {
                caps.TryGetValue(i.CapabilityId, out var cap);
                return new TemplateItemDto
                {
                    Id = i.Id,
                    CapabilityId = i.CapabilityId,
                    CapabilityCode = cap?.Code ?? string.Empty,
                    CapabilityDescription = cap?.Description,
                };
            }).ToList();
        }

        return dto;
    }

    private void AddItemsToVersion(int versionId, IEnumerable<string> codes)
    {
        foreach (var code in codes)
        {
            var cap = _capabilityDal.Get(c => c.Code == code && c.IsActive);
            if (cap == null) continue;

            _itemDal.Add(new TemplateItem
            {
                TemplateVersionId = versionId,
                CapabilityId = cap.Id,
            });
        }
    }
}

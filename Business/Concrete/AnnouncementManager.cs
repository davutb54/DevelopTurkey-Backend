using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class AnnouncementManager : IAnnouncementService
{
    private readonly IAnnouncementDal _announcementDal;

    public AnnouncementManager(IAnnouncementDal announcementDal)
    {
        _announcementDal = announcementDal;
    }

    public IDataResult<List<AnnouncementDto>> GetActive(int? institutionId = null)
    {
        var now = DateTime.UtcNow;
        var list = _announcementDal.GetAll(a =>
            a.IsActive &&
            (a.ExpiresAt == null || a.ExpiresAt > now) &&
            (a.TargetGroup == "all" ||
             (a.TargetGroup == "institution" && a.InstitutionId == institutionId) ||
             a.TargetGroup == "registered")
        ).Select(ToDto).OrderByDescending(a => a.CreatedAt).ToList();

        return new SuccessDataResult<List<AnnouncementDto>>(list);
    }

    public IDataResult<List<AnnouncementDto>> GetAll()
    {
        var list = _announcementDal.GetAll()
            .Select(ToDto)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
        return new SuccessDataResult<List<AnnouncementDto>>(list);
    }

    public IDataResult<AnnouncementDto> GetById(int id)
    {
        var entity = _announcementDal.Get(a => a.Id == id);
        return entity is null
            ? new ErrorDataResult<AnnouncementDto>(null!, "Duyuru bulunamadı.")
            : new SuccessDataResult<AnnouncementDto>(ToDto(entity));
    }

    public IResult Create(CreateAnnouncementDto dto, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return new ErrorResult("Başlık boş olamaz.");

        _announcementDal.Add(new Announcement
        {
            Title           = dto.Title,
            Content         = dto.Content,
            TargetGroup     = dto.TargetGroup ?? "all",
            InstitutionId   = dto.InstitutionId,
            Link            = dto.Link,
            IsActive        = true,
            CreatedByUserId = createdByUserId,
            CreatedAt       = DateTime.UtcNow,
            ExpiresAt       = dto.ExpiresAt,
        });
        return new SuccessResult("Duyuru oluşturuldu.");
    }

    public IResult Deactivate(int id)
    {
        var entity = _announcementDal.Get(a => a.Id == id);
        if (entity is null) return new ErrorResult("Duyuru bulunamadı.");
        entity.IsActive = false;
        _announcementDal.Update(entity);
        return new SuccessResult("Duyuru devre dışı bırakıldı.");
    }

    public IResult Delete(int id)
    {
        var entity = _announcementDal.Get(a => a.Id == id);
        if (entity is null) return new ErrorResult("Duyuru bulunamadı.");
        _announcementDal.Delete(entity);
        return new SuccessResult("Duyuru silindi.");
    }

    private static AnnouncementDto ToDto(Announcement a) => new()
    {
        Id              = a.Id,
        Title           = a.Title,
        Content         = a.Content,
        TargetGroup     = a.TargetGroup,
        InstitutionId   = a.InstitutionId,
        Link            = a.Link,
        IsActive        = a.IsActive,
        CreatedByUserId = a.CreatedByUserId,
        CreatedAt       = a.CreatedAt,
        ExpiresAt       = a.ExpiresAt,
    };
}

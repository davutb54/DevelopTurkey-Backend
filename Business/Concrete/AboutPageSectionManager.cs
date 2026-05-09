using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class AboutPageSectionManager : IAboutPageSectionService
{
    private readonly IAboutPageSectionDal _aboutPageSectionDal;
    private readonly ILogService _logService;

    public AboutPageSectionManager(IAboutPageSectionDal aboutPageSectionDal, ILogService logService)
    {
        _aboutPageSectionDal = aboutPageSectionDal;
        _logService = logService;
    }

    public IDataResult<List<AboutPageSection>> GetActiveSections()
    {
        var sections = _aboutPageSectionDal.GetAll(s => s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.OrderIndex)
            .ToList();
        return new SuccessDataResult<List<AboutPageSection>>(sections);
    }

    public IDataResult<List<AboutPageSection>> GetAll()
    {
        var sections = _aboutPageSectionDal.GetAll().OrderBy(s => s.OrderIndex).ToList();
        return new SuccessDataResult<List<AboutPageSection>>(sections);
    }

    public IResult Add(AboutPageSection section)
    {
        section.CreatedDate = DateTime.Now;
        section.IsActive = true;
        section.IsDeleted = false;
        _aboutPageSectionDal.Add(section);

        _logService.LogInfo(
            "AdminAction",
            "AboutSectionAdd",
            $"Hakkımızda bölümü eklendi. Başlık:{section.Title}, Sıra:{section.OrderIndex}");

        return new SuccessResult("Bölüm başarıyla eklendi.");
    }

    public IResult Update(AboutPageSection section)
    {
        var existing = _aboutPageSectionDal.Get(s => s.Id == section.Id);
        if (existing == null)
            return new ErrorResult("Bölüm bulunamadı.");

        existing.Title = section.Title;
        existing.Content = section.Content;
        existing.OrderIndex = section.OrderIndex;
        existing.IsActive = section.IsActive;
        existing.UpdatedDate = DateTime.Now;

        _aboutPageSectionDal.Update(existing);

        _logService.LogInfo(
            "AdminAction",
            "AboutSectionUpdate",
            $"Hakkımızda bölümü güncellendi. Id:{section.Id}, Başlık:{section.Title}, Aktif:{section.IsActive}");

        return new SuccessResult("Bölüm başarıyla güncellendi.");
    }

    public IResult Delete(int id)
    {
        var section = _aboutPageSectionDal.Get(s => s.Id == id);
        if (section == null)
            return new ErrorResult("Bölüm bulunamadı.");

        section.IsDeleted = true;
        section.UpdatedDate = DateTime.Now;
        _aboutPageSectionDal.Update(section);

        _logService.LogInfo(
            "AdminAction",
            "AboutSectionDelete",
            $"Hakkımızda bölümü silindi (soft delete). Id:{id}, Başlık:{section.Title}");

        return new SuccessResult("Bölüm başarıyla silindi.");
    }
}

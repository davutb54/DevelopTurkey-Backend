using Business.Abstract;
using Business.Constants;
using Business.Models;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using System.Linq;

namespace Business.Concrete;

public class InstitutionManager : IInstitutionService
{
    private readonly IInstitutionDal _institutionDal;
    private readonly ILogService _logService;
    private readonly IWorkflowEventBus _eventBus;

    public InstitutionManager(IInstitutionDal institutionDal, ILogService logService, IWorkflowEventBus eventBus)
    {
        _institutionDal = institutionDal;
        _logService = logService;
        _eventBus = eventBus;
    }

    public IDataResult<Institution> GetById(int id)
    {
        var institution = _institutionDal.Get(i => i.Id == id);
        if (institution == null)
        {
            return new ErrorDataResult<Institution>(institution,"Kurum bulunamadı");
        }
        return new SuccessDataResult<Institution>(institution);
    }

    public IDataResult<List<Institution>> GetAll()
    {
        return new SuccessDataResult<List<Institution>>(_institutionDal.GetAll());
    }

    public IDataResult<Institution> GetByDomain(string domain)
    {
        var institution = _institutionDal.Get(i => i.Domain == domain);
        if (institution == null)
        {
            return new ErrorDataResult<Institution>(institution,"Belirtilen domain ile kurum bulunamadı");
        }
        return new SuccessDataResult<Institution>(institution);
    }

    public IDataResult<InstitutionPublicInfoDto> GetPublicInfo(string domain)
    {
        var institution = _institutionDal.Get(i => i.Domain == domain);
        if (institution == null)
            return new ErrorDataResult<InstitutionPublicInfoDto>(default, "Belirtilen domain ile kurum bulunamadı");

        return new SuccessDataResult<InstitutionPublicInfoDto>(new InstitutionPublicInfoDto
        {
            Id           = institution.Id,
            Name         = institution.Name,
            LogoUrl      = institution.LogoUrl,
            PrimaryColor = institution.PrimaryColor,
        });
    }

    public IDataResult<InstitutionPublicInfoDto> GetPublicInfoBySubdomain(string slug)
    {
        var institution = _institutionDal.Get(i => i.Subdomain == slug);
        if (institution == null)
            return new ErrorDataResult<InstitutionPublicInfoDto>(default, "Belirtilen subdomain ile kurum bulunamadı");

        return new SuccessDataResult<InstitutionPublicInfoDto>(new InstitutionPublicInfoDto
        {
            Id           = institution.Id,
            Name         = institution.Name,
            LogoUrl      = institution.LogoUrl,
            PrimaryColor = institution.PrimaryColor,
        });
    }

    public IResult Add(Institution institution)
    {
        NormalizeInstitutionJson(institution);
        _institutionDal.Add(institution);

        _ = _eventBus.PublishAsync("institution.created", new RuleContext
        {
            InstitutionId = institution.Id
        });

        _logService.LogInfo("AdminAction", "Add", $"Kurum eklendi - İsim: {institution.Name}");

        return new SuccessResult("Kurum başarıyla eklendi");
    }

    public IResult Update(Institution institution)
    {
        NormalizeInstitutionJson(institution);
        _institutionDal.Update(institution);

        _ = _eventBus.PublishAsync("institution.updated", new RuleContext
        {
            InstitutionId = institution.Id
        });

        _logService.LogInfo("AdminAction", "Update", $"Kurum güncellendi - ID: {institution.Id}, İsim: {institution.Name}");

        return new SuccessResult("Kurum başarıyla güncellendi");
    }

    public IResult Delete(int id)
    {
        var institution = _institutionDal.Get(i => i.Id == id);
        if (institution == null)
        {
            return new ErrorResult("Kurum bulunamadı");
        }

        institution.Status = false;
        _institutionDal.Update(institution);

        _ = _eventBus.PublishAsync("institution.deactivated", new RuleContext
        {
            InstitutionId = institution.Id
        });

        _logService.LogWarning("AdminAction", "Delete", $"Kurum pasife alındı - ID: {id}, İsim: {institution.Name}");

        return new SuccessResult("Kurum başarıyla pasif duruma alındı");
    }

    private static void NormalizeInstitutionJson(Institution institution)
    {
        institution.CustomFieldsJson = string.IsNullOrWhiteSpace(institution.CustomFieldsJson) ? "[]" : institution.CustomFieldsJson;
    }
}

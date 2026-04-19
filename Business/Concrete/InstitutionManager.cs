using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class InstitutionManager : IInstitutionService
{
    private readonly IInstitutionDal _institutionDal;
    private readonly ILogService _logService;

    public InstitutionManager(IInstitutionDal institutionDal, ILogService logService)
    {
        _institutionDal = institutionDal;
        _logService = logService;
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

    public IResult Add(Institution institution)
    {
        _institutionDal.Add(institution);

        _logService.LogInfo("AdminAction", "Add", $"Kurum eklendi - İsim: {institution.Name}");

        return new SuccessResult("Kurum başarıyla eklendi");
    }

    public IResult Update(Institution institution)
    {
        _institutionDal.Update(institution);

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

        _logService.LogWarning("AdminAction", "Delete", $"Kurum pasife alındı - ID: {id}, İsim: {institution.Name}");

        return new SuccessResult("Kurum başarıyla pasif duruma alındı");
    }
}

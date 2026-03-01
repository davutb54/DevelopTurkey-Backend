using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class InstitutionManager : IInstitutionService
{
    private readonly IInstitutionDal _institutionDal;
    private readonly ILogDal _logDal;

    public InstitutionManager(IInstitutionDal institutionDal, ILogDal logDal)
    {
        _institutionDal = institutionDal;
        _logDal = logDal;
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

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kurum eklendi - İsim: {institution.Name}",
            Type = "Institution,Add,Info"
        });

        return new SuccessResult("Kurum başarıyla eklendi");
    }

    public IResult Update(Institution institution)
    {
        _institutionDal.Update(institution);

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kurum güncellendi - ID: {institution.Id}",
            Type = "Institution,Update,Info"
        });

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

        _logDal.Add(new Log
        {
            CreationDate = DateTime.Now,
            Message = $"Kurum silindi - ID: {id}",
            Type = "Institution,Delete,Info"
        });

        return new SuccessResult("Kurum başarıyla silindi");
    }
}

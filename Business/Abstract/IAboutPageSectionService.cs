using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IAboutPageSectionService
{
    IDataResult<List<AboutPageSection>> GetActiveSections();
    IDataResult<List<AboutPageSection>> GetAll();
    IResult Add(AboutPageSection section);
    IResult Update(AboutPageSection section);
    IResult Delete(int id);
}

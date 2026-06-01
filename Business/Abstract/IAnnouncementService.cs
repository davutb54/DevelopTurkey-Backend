using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IAnnouncementService
{
    IDataResult<List<AnnouncementDto>> GetActive(int? institutionId = null);
    IDataResult<List<AnnouncementDto>> GetAll();
    IDataResult<AnnouncementDto> GetById(int id);
    IResult Create(CreateAnnouncementDto dto, int createdByUserId);
    IResult Deactivate(int id);
    IResult Delete(int id);
}

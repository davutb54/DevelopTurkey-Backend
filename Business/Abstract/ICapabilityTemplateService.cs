using Core.Utilities.Results;
using Entities.DTOs.Capability;

namespace Business.Abstract;

public interface ICapabilityTemplateService
{
    IDataResult<List<CapabilityTemplateDto>> GetAll();
    IDataResult<CapabilityTemplateDto> GetById(int id);
    IDataResult<List<TemplateVersionDto>> GetVersions(int templateId);
    IResult Create(CreateTemplateDto dto);
    IResult PublishVersion(int templateId, PublishTemplateVersionDto dto);
    Task<IResult> ApplyAsync(int templateId, ApplyTemplateDto dto);
    IResult Deactivate(int id);
}

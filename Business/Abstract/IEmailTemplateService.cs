using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IEmailTemplateService
{
    IDataResult<List<EmailTemplate>> GetAll();
    IDataResult<EmailTemplate> GetById(int id);
    IDataResult<EmailTemplate> GetByKey(string templateKey);
    IResult Add(EmailTemplate emailTemplate);
    IResult Update(EmailTemplate emailTemplate);
    IResult Delete(EmailTemplate emailTemplate);
    
    // Method to replace placeholders
    IDataResult<string> RenderTemplate(string templateBody, Dictionary<string, string> placeholders);
}

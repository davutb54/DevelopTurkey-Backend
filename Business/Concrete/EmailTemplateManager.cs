using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class EmailTemplateManager : IEmailTemplateService
{
    private readonly IEmailTemplateDal _emailTemplateDal;

    public EmailTemplateManager(IEmailTemplateDal emailTemplateDal)
    {
        _emailTemplateDal = emailTemplateDal;
    }

    public IDataResult<List<EmailTemplate>> GetAll()
    {
        return new SuccessDataResult<List<EmailTemplate>>(_emailTemplateDal.GetAll(), "Şablonlar listelendi.");
    }

    public IDataResult<EmailTemplate> GetById(int id)
    {
        return new SuccessDataResult<EmailTemplate>(_emailTemplateDal.Get(t => t.Id == id), "Şablon getirildi.");
    }

    public IDataResult<EmailTemplate> GetByKey(string templateKey)
    {
        var template = _emailTemplateDal.Get(t => t.TemplateKey == templateKey && t.IsActive);
        if (template == null)
            return new ErrorDataResult<EmailTemplate>(null, "E-posta şablonu bulunamadı.");
        
        return new SuccessDataResult<EmailTemplate>(template);
    }

    public IResult Add(EmailTemplate emailTemplate)
    {
        _emailTemplateDal.Add(emailTemplate);
        return new SuccessResult("E-posta şablonu eklendi.");
    }

    public IResult Update(EmailTemplate emailTemplate)
    {
        var existingTemplate = _emailTemplateDal.Get(t => t.Id == emailTemplate.Id);
        if (existingTemplate == null) return new ErrorResult("Şablon bulunamadı.");

        existingTemplate.Subject = emailTemplate.Subject;
        existingTemplate.Body = emailTemplate.Body;
        existingTemplate.Description = emailTemplate.Description;
        existingTemplate.AvailablePlaceholders = emailTemplate.AvailablePlaceholders;
        existingTemplate.IsActive = emailTemplate.IsActive;
        existingTemplate.UpdatedAt = DateTime.Now;

        _emailTemplateDal.Update(existingTemplate);
        return new SuccessResult("E-posta şablonu güncellendi.");
    }

    public IResult Delete(EmailTemplate emailTemplate)
    {
        _emailTemplateDal.Delete(emailTemplate);
        return new SuccessResult("E-posta şablonu silindi.");
    }

    public IDataResult<string> RenderTemplate(string templateBody, Dictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(templateBody)) return new ErrorDataResult<string>(null, "Şablon gövdesi boş.");

        string renderedBody = templateBody;
        foreach (var placeholder in placeholders)
        {
            renderedBody = renderedBody.Replace(placeholder.Key, placeholder.Value);
        }

        return new SuccessDataResult<string>(renderedBody, "Şablon başarıyla işlendi.");
    }
}

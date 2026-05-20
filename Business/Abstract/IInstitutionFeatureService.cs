using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IInstitutionFeatureService
{
    // Tek bir feature'ın değerini oku (boolean için)
    bool IsFeatureEnabled(int institutionId, string featureKey, bool defaultValue = false);
    
    // Tek bir feature'ın değerini oku (string olarak)
    string GetFeatureValue(int institutionId, string featureKey, string defaultValue = "");
    
    // Kurumun tüm feature değerlerini getir (key -> value map)
    IDataResult<Dictionary<string, string>> GetAllForInstitution(int institutionId);
    
    // Kurumun bir feature değerini ayarla
    IResult SetFeatureValue(int institutionId, string featureKey, string value);
    
    // Kurumun birden fazla feature değerini toplu ayarla
    IResult SetFeatureValues(int institutionId, Dictionary<string, string> values);
    
    // Cache'i temizle
    void InvalidateCache(int institutionId);
}

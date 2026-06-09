namespace Core.Utilities.Context;

public interface ITenantProvider
{
    int?  InstitutionId  { get; }
    bool  IsGlobalAdmin  { get; }
}

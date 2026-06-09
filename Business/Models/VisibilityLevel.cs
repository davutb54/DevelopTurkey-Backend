namespace Business.Models;

public enum VisibilityLevel
{
    Closed = 0,
    Public = 1,
    AdminOnly = 2,
    AdminAndOwner = 3,
    OwnerOnly = 4
}

public static class VisibilityHelper
{
    public static VisibilityLevel Parse(string? value) => value switch
    {
        "public" => VisibilityLevel.Public,
        "admin_only" => VisibilityLevel.AdminOnly,
        "admin_and_owner" => VisibilityLevel.AdminAndOwner,
        "owner_only" => VisibilityLevel.OwnerOnly,
        _ => VisibilityLevel.Closed
    };

    public static bool CanView(VisibilityLevel level, int? requesterId, int ownerId, Core.Utilities.Authorization.ICapabilityResolver resolver)
    {
        return level switch
        {
            VisibilityLevel.Closed => false,
            VisibilityLevel.Public => true,
            VisibilityLevel.AdminOnly =>
                requesterId.HasValue && resolver.Allows(requesterId.Value, "admin.system_access", null),
            VisibilityLevel.AdminAndOwner =>
                (requesterId.HasValue && requesterId.Value == ownerId)
                || (requesterId.HasValue && resolver.Allows(requesterId.Value, "admin.system_access", null)),
            VisibilityLevel.OwnerOnly =>
                requesterId.HasValue && requesterId.Value == ownerId,
            _ => false
        };
    }
}

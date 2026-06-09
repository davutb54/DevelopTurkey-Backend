namespace Core.Utilities.Context;

/// <summary>
/// Ambient tenant context used by EF Core global query filters.
/// Set by WebTenantProvider at the start of each HTTP request via TenantResolutionMiddleware.
/// When not set (background services, seeders, migrations), ShouldBypassFilter returns true
/// so all data is visible — repository calls from those contexts are inherently trusted.
/// </summary>
public static class TenantScopeContext
{
    private static readonly AsyncLocal<bool?> _isSet       = new();
    private static readonly AsyncLocal<bool>  _isGlobalAdmin = new();
    private static readonly AsyncLocal<int?>  _institutionId = new();

    public static void Set(int? institutionId, bool isGlobalAdmin)
    {
        _isSet.Value          = true;
        _isGlobalAdmin.Value  = isGlobalAdmin;
        _institutionId.Value  = institutionId;
    }

    /// <summary>
    /// Returns true when:
    ///   • No HTTP context is active (background / seeder / migration), OR
    ///   • The current user has global-admin cross-tenant read privilege.
    /// EF Core query filters short-circuit when this is true.
    /// </summary>
    public static bool ShouldBypassFilter => _isSet.Value != true || _isGlobalAdmin.Value;

    public static int? InstitutionId => _institutionId.Value;
}

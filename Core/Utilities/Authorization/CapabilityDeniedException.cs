namespace Core.Utilities.Authorization;

public sealed class CapabilityDeniedException : Exception
{
    public string RequiredCapability { get; }

    public CapabilityDeniedException(string requiredCapability)
        : base($"Yetersiz yetki: '{requiredCapability}' gerekli.")
    {
        RequiredCapability = requiredCapability;
    }
}

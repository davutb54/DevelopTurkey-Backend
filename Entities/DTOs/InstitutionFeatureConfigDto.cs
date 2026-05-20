namespace Entities.DTOs;

public class InstitutionFeatureConfigDto
{
    public IdentityFeatures Identity { get; set; } = new();
    public ContentFeatures Content { get; set; } = new();
    public SocialFeatures Social { get; set; } = new();
    public ModerationFeatures Moderation { get; set; } = new();
}

public class IdentityFeatures
{
    public bool AllowGoogleLogin { get; set; } = true;
    public bool RequireEmailVerification { get; set; } = true;
}

public class ContentFeatures
{
    public bool RequireMapLocation { get; set; } = true;
    public bool AllowImageUpload { get; set; } = true;
    public bool AllowAnonymous { get; set; } = false;
}

public class SocialFeatures
{
    public bool EnableUpvote { get; set; } = true;
    public bool EnableNestedComments { get; set; } = true;
}

public class ModerationFeatures
{
    public bool RequireExpertApproval { get; set; } = false;
}

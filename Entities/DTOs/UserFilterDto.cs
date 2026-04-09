namespace Entities.DTOs;

public class UserFilterDto
{
    public string? SearchText { get; set; }     // userName, name, surname, email
    public string? RoleFilter { get; set; }      // "admin" | "expert" | "official" | "banned"
    public string? EmailStatus { get; set; }     // "verified" | "unverified"
    public int? InstitutionId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

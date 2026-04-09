namespace Entities.DTOs;

public class FeedbackFilterDto
{
    public string? SearchText { get; set; }     // title veya message içeriği
    public bool? IsRead { get; set; }            // null=hepsi, true=okunmuş, false=okunmamış

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

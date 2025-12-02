using Microsoft.AspNetCore.Http;

namespace Entities.DTOs.User;

public class UserImageUpdateDto
{
    public int UserId { get; set; }
    public IFormFile Image { get; set; }
}
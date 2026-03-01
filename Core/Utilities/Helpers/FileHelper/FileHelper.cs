using Microsoft.AspNetCore.Http;

namespace Core.Utilities.Helpers.FileHelper;

public class FileHelper
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public static string Add(IFormFile file, string root)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"Dosya boyutu {MaxFileSize / (1024 * 1024)} MB'dan büyük olamaz.");
        }

        string extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Sadece şu formatlar desteklenir: {string.Join(", ", AllowedImageExtensions)}");
        }

        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            throw new InvalidOperationException("Geçersiz dosya tipi.");
        }

        if (!Directory.Exists(root))
        {
            Directory.CreateDirectory(root);
        }

        string guid = Guid.NewGuid().ToString();
        string newFileName = guid + extension;

        string sanitizedRoot = Path.GetFullPath(root);
        string filePath = Path.Combine(sanitizedRoot, newFileName);

        if (!filePath.StartsWith(sanitizedRoot))
        {
            throw new InvalidOperationException("Path traversal saldırısı tespit edildi.");
        }

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fileStream);
        }

        return newFileName;
    }

    public static void Delete(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        string fullPath = Path.GetFullPath(filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
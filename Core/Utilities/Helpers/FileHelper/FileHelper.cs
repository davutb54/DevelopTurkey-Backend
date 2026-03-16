using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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

        if (!IsValidImageSignature(file))
        {
            throw new InvalidOperationException("Dosya içeriği geçerli bir resim formatında değil (Zararlı yazılım koruması).");
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

    private static bool IsValidImageSignature(IFormFile file)
    {
        using (var reader = new BinaryReader(file.OpenReadStream()))
        {
            var signatures = new Dictionary<string, byte[][]>
            {
                { ".jpeg", new byte[][] { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
                { ".jpg", new byte[][] { new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 } } },
                { ".png", new byte[][] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
                { ".gif", new byte[][] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
                { ".webp", new byte[][] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF prefix for webp
            };

            string ext = Path.GetExtension(file.FileName).ToLower();
            if(!signatures.ContainsKey(ext)) return false;

            var expectedSignatures = signatures[ext];
            int maxSignatureLength = expectedSignatures.Max(s => s.Length);
            byte[] headerBytes = reader.ReadBytes(maxSignatureLength);

            return expectedSignatures.Any(signature => 
                headerBytes.Take(signature.Length).SequenceEqual(signature)
            );
        }
    }
}
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Core.Utilities.Helpers.FileHelper;

public class FileHelper
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm" };
    private static readonly long MaxImageFileSize = 5  * 1024 * 1024;  // 5 MB
    private static readonly long MaxVideoFileSize = 100 * 1024 * 1024; // 100 MB

    // ── Görsel yükleme ──────────────────────────────────────────────────────────

    public static string Add(IFormFile file, string root)
    {
        if (file == null || file.Length == 0) return null!;

        if (file.Length > MaxImageFileSize)
            throw new InvalidOperationException($"Dosya boyutu {MaxImageFileSize / (1024 * 1024)} MB'dan büyük olamaz.");

        string extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedImageExtensions.Contains(extension))
            throw new InvalidOperationException($"Sadece şu formatlar desteklenir: {string.Join(", ", AllowedImageExtensions)}");

        var allowedMimes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
        if (!allowedMimes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("Geçersiz dosya tipi.");

        if (!IsValidImageSignature(file))
            throw new InvalidOperationException("Dosya içeriği geçerli bir resim formatında değil (Zararlı yazılım koruması).");

        return SaveFile(file, root);
    }

    // ── Video yükleme ───────────────────────────────────────────────────────────

    public static string AddVideo(IFormFile file, string root)
    {
        if (file == null || file.Length == 0) return null!;

        if (file.Length > MaxVideoFileSize)
            throw new InvalidOperationException($"Video boyutu {MaxVideoFileSize / (1024 * 1024)} MB'dan büyük olamaz.");

        string extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedVideoExtensions.Contains(extension))
            throw new InvalidOperationException($"Sadece şu video formatları desteklenir: {string.Join(", ", AllowedVideoExtensions)}");

        var allowedMimes = new[] { "video/mp4", "video/webm" };
        string contentType = file.ContentType.ToLower();
        if (!allowedMimes.Any(m => contentType.StartsWith(m)))
            throw new InvalidOperationException("Geçersiz video dosya tipi.");

        if (!IsValidVideoSignature(file, extension))
            throw new InvalidOperationException("Dosya içeriği geçerli bir video formatında değil (Zararlı yazılım koruması).");

        return SaveFile(file, root);
    }

    // ── Silme: tam yol ─────────────────────────────────────────────────────────

    public static void Delete(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        string fullPath = Path.GetFullPath(filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    // ── Silme: dosya adı + kök dizin (path-traversal korumalı) ────────────────

    public static void Delete(string fileName, string root)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(root)) return;

        string sanitizedRoot = Path.GetFullPath(root);
        // Path.GetFileName koruma: ../../../etc/passwd gibi girişleri dosya adına indirgeyip,
        // ardından tam yol kontrolü ile reddeder.
        string safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName)) return;

        string fullPath = Path.GetFullPath(Path.Combine(sanitizedRoot, safeFileName));
        if (!fullPath.StartsWith(sanitizedRoot + Path.DirectorySeparatorChar)
            && !fullPath.Equals(sanitizedRoot, StringComparison.OrdinalIgnoreCase))
            return; // path traversal girişimi — sessizce yoksay

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    // ── İç yardımcılar ─────────────────────────────────────────────────────────

    private static string SaveFile(IFormFile file, string root)
    {
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        string sanitizedRoot = Path.GetFullPath(root);
        string ext           = Path.GetExtension(file.FileName).ToLower();
        string newFileName   = Guid.NewGuid().ToString() + ext;
        string filePath      = Path.Combine(sanitizedRoot, newFileName);

        if (!filePath.StartsWith(sanitizedRoot))
            throw new InvalidOperationException("Path traversal saldırısı tespit edildi.");

        using var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);
        return newFileName;
    }

    private static bool IsValidImageSignature(IFormFile file)
    {
        using var reader = new BinaryReader(file.OpenReadStream());
        var signatures = new Dictionary<string, byte[][]>
        {
            { ".jpeg", [[ 0xFF, 0xD8, 0xFF, 0xE0 ], [ 0xFF, 0xD8, 0xFF, 0xE1 ], [ 0xFF, 0xD8, 0xFF, 0xE8 ]] },
            { ".jpg",  [[ 0xFF, 0xD8, 0xFF, 0xE0 ], [ 0xFF, 0xD8, 0xFF, 0xE1 ], [ 0xFF, 0xD8, 0xFF, 0xE8 ]] },
            { ".png",  [[ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A ]] },
            { ".gif",  [[ 0x47, 0x49, 0x46, 0x38 ]] },
            { ".webp", [[ 0x52, 0x49, 0x46, 0x46 ]] },
        };

        string ext = Path.GetExtension(file.FileName).ToLower();
        if (!signatures.ContainsKey(ext)) return false;

        var expected  = signatures[ext];
        int maxLen    = expected.Max(s => s.Length);
        byte[] header = reader.ReadBytes(maxLen);

        return expected.Any(sig => header.Take(sig.Length).SequenceEqual(sig));
    }

    private static bool IsValidVideoSignature(IFormFile file, string extension)
    {
        using var reader = new BinaryReader(file.OpenReadStream());

        if (extension == ".webm")
        {
            // EBML magic bytes
            byte[] h = reader.ReadBytes(4);
            return h.Length == 4 &&
                   h[0] == 0x1A && h[1] == 0x45 && h[2] == 0xDF && h[3] == 0xA3;
        }

        if (extension == ".mp4")
        {
            // MP4 box: 4 byte size + "ftyp" (0x66 0x74 0x79 0x70) at offset 4
            byte[] h = reader.ReadBytes(12);
            return h.Length >= 8 &&
                   h[4] == 0x66 && h[5] == 0x74 && h[6] == 0x79 && h[7] == 0x70;
        }

        return false;
    }
}

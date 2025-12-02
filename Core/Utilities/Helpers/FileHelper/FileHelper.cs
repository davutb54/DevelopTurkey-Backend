using Microsoft.AspNetCore.Http;

namespace Core.Utilities.Helpers.FileHelper;

public class FileHelper
{
    public static string Add(IFormFile file, string root)
    {
        if (file.Length > 0)
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            string extension = Path.GetExtension(file.FileName);

            string guid = Guid.NewGuid().ToString();
            string newFileName = guid + extension;

            string filePath = Path.Combine(root, newFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return newFileName;
        }
        return null;
    }

    public static void Delete(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
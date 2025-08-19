using iText.Kernel.Pdf;
using System;
using System.IO;
using System.Security.Cryptography;

namespace AudacityV2.Utils
{
    /// <summary>
    /// Static utility methods for PDF metadata extraction, hashing, and file path helpers.
    /// Moved here to avoid circular dependencies between Helpers and S3Helper.
    /// </summary>
    public static class HelperUtils
    {
        public static string GetPdfTitle(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);
            var info = pdf.GetDocumentInfo();
            return info.GetTitle()?.Replace('\'', '_') ?? "No title in metadata";
        }

        public static int GetPdfPageCount(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);
            return pdf.GetNumberOfPages();
        }

        public static string GetPdfAuthor(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);
            var info = pdf.GetDocumentInfo();
            return info.GetAuthor()?.Replace('\'', '_') ?? "No Author in metadata";
        }

        public static string GenHash(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static void EnsureDirExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static string GetDownloadPath(string fileName, string downloadDir)
        {
            EnsureDirExists(downloadDir);
            return Path.Combine(downloadDir, fileName);
        }
    }
}

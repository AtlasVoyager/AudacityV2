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
        /* private static readonly Dictionary<string, string> ActiveSelections = new();
 */
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

        public static IEnumerable<(int Index, string Hash, string Result)> SearchBooks(
     Dictionary<string, Metadata> books, string query)
        {
            int index = 1;

            // Title search
            var titleMatches = books
                .Where(kvp => kvp.Value.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kvp => kvp.Value.Title)
                .Select(kvp => (kvp.Key, $"{kvp.Value.Title} by {kvp.Value.Author}"));

            // Author search
            var authorMatches = books
                .Where(kvp => kvp.Value.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kvp => kvp.Value.Author)
                .Select(kvp => (kvp.Key, $"{kvp.Value.Title} by {kvp.Value.Author}"));

            // UploadedBy search
            var uploaderMatches = books
                .Where(kvp => kvp.Value.UploadedBy.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kvp => kvp.Value.UploadedBy)
                .Select(kvp => (kvp.Key, $"{kvp.Value.Title} by {kvp.Value.Author}"));

            // Combine results
            var combined = titleMatches.Concat(authorMatches).Concat(uploaderMatches).DistinctBy(x => x.Key);

            // Index them
            foreach (var (hash, result) in combined)
            {
                yield return (index, hash, result);
                index++;
            }
        }

    }
}

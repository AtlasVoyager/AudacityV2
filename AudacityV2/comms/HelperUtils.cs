using iText.Kernel.Pdf;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using VersOne.Epub;

namespace AudacityV2.Utils
{
    /// <summary>
    /// Static utility methods for PDF metadata extraction, hashing, and file path helpers.
    /// Moved here to avoid circular dependencies between Helpers and S3Helper.
    /// </summary>
    public static class HelperUtils
    {
        //storage for the books in use once the queue is empty delete read order
        public static Dictionary<ReadOrder, ConcurrentQueue<string>> ActiveSelections = new();

        public static string GetEpubTitle(string filePath)
        {
            EpubBook book = EpubReader.ReadBook(filePath);
            return string.IsNullOrWhiteSpace(book.Title) ? "No title in metadata" : book.Title.Replace('\'', '_');
        }


        public static string GetEpubAuthor(string filePath)
        {
            EpubBook book = EpubReader.ReadBook(filePath);
            if (book.AuthorList != null && book.AuthorList.Count > 0)
            {
                return string.Join(", ", book.AuthorList).Replace('\'', '_');
            }
            return "No Author in metadata";
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

        public static int GetEpubChapterCount(string filePath)
        {
            // Read the EPUB
            var book = EpubReader.ReadBook(filePath);
            // EPUB has a navigation list (like a Table of Contents)
            if (book.Navigation != null && book.Navigation.Count > 0)
            {
                return book.Navigation.Count;
            }
            // Fallback: if no nav, use reading order
            return book.ReadingOrder.Count;
        }
    }
}

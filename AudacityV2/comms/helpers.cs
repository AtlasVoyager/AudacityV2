using Amazon.Runtime.Internal.Transform;
using AudacityV2.AWS;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace AudacityV2.comms
{
    public class Helpers
    {
        //public static Dictionary<string, long> channelID = new Dictionary<string, long>
        //{
        //        { "general", 1397252659945148428 },
        //        { "uploads", 1399218934229635072 },
        //        { "stories", 1400491410960154827 }
        //};

        //public record BookMetadata(string Title, string Author, string UploadedBy);
        private S3Helper s3 = new S3Helper();

        public static IEnumerable<(int, string)> SearchBooks(
        Dictionary<string, Metadata> books, string query)
        {
            int index = 1;

            // Title search
            var titleMatches = books.Values
                .Where(b => b.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.Title)
                .Select(b => $"{b.Title} by {b.Author}");

            // Author search
            var authorMatches = books.Values
                .Where(b => b.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.Author)
                .Select(b => $"{b.Title} by {b.Author}");

            // UploadedBy search
            var uploaderMatches = books.Values
                .Where(b => b.UploadedBy.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.UploadedBy)
                .Select(b => $"{b.Title} by {b.Author}");

            // Combine & index results
            var combined = titleMatches.Concat(authorMatches).Concat(uploaderMatches);

            foreach (var result in combined)
            {
                yield return (index, result);
                index++;
            }
        }

        public void ParseBook(DiscordAttachment stuff)
        {
            throw new NotImplementedException();
        }

        public Metadata MakeMetaData(CommandContext ctx, DiscordAttachment stuff)
        {
            return new Metadata
            {
                Title = GetPdfTitle(stuff.Url),
                Author = GetPdfAuthor(stuff.Url),
                UploadedBy = ctx.User.ToString(),
                UploadDate = DateTime.UtcNow,
                FileName = stuff.FileName,
                PageCount = GetPdfPageCount(stuff.Url)
            };
        }
        /// <summary>
        /// Get the title of a PDF
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string GetPdfTitle(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);

            var info = pdf.GetDocumentInfo();
            return info.GetTitle().Replace('\'', '_') ?? "No title in metadata";
        }
        /// <summary>
        /// Count the number of pages in a PDF
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public int GetPdfPageCount(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);

            var info = pdf.GetNumberOfPages();
            return info;
        }
        /// <summary>
        /// Get the author of a PDF
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string GetPdfAuthor(string filePath)
        {
            using var reader = new PdfReader(filePath);
            using var pdf = new PdfDocument(reader);

            var info = pdf.GetDocumentInfo();
            return info.GetAuthor().Replace('\'', '_') ?? "No Author in metadata";
        }
        /// <summary>
        /// Generate a unique Hash to ID the book
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public string GenHash(Stream fileStream)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public void EnsureDirExists(string downloadDir)
        {
            if (!Directory.Exists(downloadDir))
            {
                Directory.CreateDirectory(downloadDir);
            }
        }

        // Get a full path for a file inside the download folder
        public string GetDownloadPath(string fileName, string downloadDir)
        {
            EnsureDirExists(downloadDir);
            return Path.Combine(downloadDir, fileName);
        }

        public async Task Resolve()
        {
            //goes throught the index first and checks if corresponding book exists
            //if not? delete index. if book exists but index doesn't delete book\

            //get the indexes
            var index = await GetIndex();
            //list of stuff in the bucket
            var bucketStuff = await s3.BucketContent("my_books/");

            Console.WriteLine($"Index count: {index.Count}, Bucket count: {bucketStuff.Count}");
            Console.WriteLine("index stuff:");
            foreach (var item in index)
            {
                Console.WriteLine($"{item.Key} : {item.Value.Title}");
            }

            Console.WriteLine("bucket stuff:");
            foreach (var item in bucketStuff)
            {
                Console.WriteLine(item);
            }
            //throw new NotImplementedException();
        }

        public async Task<Dictionary<string, Metadata>> GetIndex()
        {
            //get the updated bookindex file
            Console.WriteLine(await s3.DownloadAsync("SHA256_hashes/bookIndex.json"));
            //read the index file
            //check if exists
            EnsureDirExists("downloads/SHA256_hashes");
            using FileStream fs = File.OpenRead("downloads/SHA256_hashes/bookIndex.json");
            //string jsonString = await File.ReadAllTextAsync("downloads/SHA256_hashes/bookIndex.json");
            var data = await JsonSerializer.DeserializeAsync<Dictionary<string, Metadata>>(fs);
            fs.Dispose();

            return data ?? new Dictionary<string, Metadata>();
        }
    }
}

using Amazon.Runtime.Internal.Transform;
using AudacityV2.AWS;
using AudacityV2.Utils;
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
        private readonly S3Helper s3;

        public Helpers(S3Helper s3Helper)
        {
            s3 = s3Helper ?? throw new ArgumentException(nameof(s3Helper));
        }

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
                Title = HelperUtils.GetPdfTitle(stuff.Url),
                Author = HelperUtils.GetPdfAuthor(stuff.Url),
                UploadedBy = ctx.User.ToString(),
                UploadDate = DateTime.UtcNow,
                FileName = stuff.FileName,
                PageCount = HelperUtils.GetPdfPageCount(stuff.Url)
            };
        }

        public async Task Resolve()
        {
            //goes throught the index first and checks if corresponding book exists
            //if not? delete index. if book exists but index doesn't delete book\

            //get the indexes
            var index = await GetIndex();
            //list of stuff in the bucket
            var bucketStuff = await s3.BucketContent("my_books/");
            var bucketFiles = bucketStuff.Where(x => x.EndsWith(".pdf")).ToList();

            Console.WriteLine($"Index count: {index.Count}, Bucket count: {bucketStuff.Count}");
            Console.WriteLine("index stuff:");

          /*  foreach (var item in index)
            {
                Console.WriteLine($"{item.Key} : {item.Value.Title}");
            }

            Console.WriteLine("bucket stuff:");
            foreach (var item in bucketStuff)
            {
                Console.WriteLine(item);
            }*/

            //make a list of all the filenames in Index
            var fileNames = index.Values
                              .Select(x => Path.GetFileNameWithoutExtension(x.FileName))
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);


            foreach (var item in bucketStuff.Where(x => x.EndsWith(".pdf")))
            {
                var genFileName = Path.GetFileNameWithoutExtension(item);

               if(!fileNames.Contains(genFileName))
                {
                    //file doesn't exist in index, delete it
                    Console.WriteLine($"File {genFileName} doesn't exist in index, deleting it");
                    await s3.DeleteObjectAsync(item);
                 }

            }

            //delete the orphaned indexes 
            var bucketFileNames = bucketFiles.Select(x => Path.GetFileNameWithoutExtension(x))
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orphanedKeys = index.Where(kvp =>
                                    !bucketFileNames.Contains(Path.GetFileNameWithoutExtension(kvp.Value.FileName)))
                                    .Select(kvp => Path.GetFileNameWithoutExtension(kvp.Value.FileName))
                                    .ToList();

            foreach (var orphan in orphanedKeys)
            {
                Console.WriteLine($"Index entry {orphan} ({index[orphan].Title}) has no matching file in bucket. Removing from index.");
                index.Remove(orphan);
            }

            // 6. Save cleaned index locally
            string localIndexPath = HelperUtils.GetDownloadPath("bookIndex.json", "downloads/SHA256_hashes");
            await File.WriteAllTextAsync(localIndexPath,
                JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true }));

            // 7. Upload cleaned index back to S3
            await s3.UploadAsync("bookIndex.json", localIndexPath, "SHA256_hashes");

            Console.WriteLine($"Resolve complete. Removed {orphanedKeys.Count} orphaned index entries.");
        }

        public async Task<Dictionary<string, Metadata>> GetIndex()
        {
            //get the updated bookindex file
            Console.WriteLine(await s3.DownloadAsync("SHA256_hashes/bookIndex.json"));
            //read the index file
            //check if exists
            HelperUtils.EnsureDirExists("downloads/SHA256_hashes");
            using FileStream fs = File.OpenRead("downloads/SHA256_hashes/bookIndex.json");
            //string jsonString = await File.ReadAllTextAsync("downloads/SHA256_hashes/bookIndex.json");
            var data = await JsonSerializer.DeserializeAsync<Dictionary<string, Metadata>>(fs);
            fs.Dispose();

            return data ?? new Dictionary<string, Metadata>();
        }
    }
}

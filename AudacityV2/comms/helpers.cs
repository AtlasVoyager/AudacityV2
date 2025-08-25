using Amazon.Runtime.Internal.Transform;
using AudacityV2.AWS;
using AudacityV2.Utils;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using iText.Kernel.Pdf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace AudacityV2.comms
{
    public class Helpers
    {

        private readonly ConcurrentDictionary<ulong, ReadOrder> readOrders = new(); //our processing queue
        private readonly ConcurrentDictionary<ulong, List<BookMenuItem>> activeMenus = new(); //temp queue for active menus

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
            var indexedStuff = index.Values.Select(x => x.Title).OrderBy(x => x).ToList();
            var bucketStuff = (await s3.BucketContent("my_books/")).Where(x => x.EndsWith(".pdf")).OrderBy(x => x).ToList();
            List<string> alreadyValid = new List<string>();

            //check if the books in the bucket exist in the index
            foreach (var book in indexedStuff)
            {
                if (alreadyValid.Contains(book))
                    continue; //skip already validated books
                if (bucketStuff.Contains("my_books/" + book+".pdf"))
                {
                    alreadyValid.Add(book);
                }
                else
                {
                    //book doesn't exist in the bucket, remove from index
                    var itemToRemove = index.FirstOrDefault(x => x.Value.Title == book);
                    if (itemToRemove.Key != null)
                    {
                        index.Remove(itemToRemove.Key);
                        Console.WriteLine($"Removed {book} from index as it does not exist in the bucket.");
                    }
                }
            }

            //seriallize index
            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(index, options);
            //write the json back to the file
            await File.WriteAllTextAsync("downloads/SHA256_hashes/bookIndex.json", updatedJson);
            //upload the updated index file Path.Combine(AppContext.BaseDirectory, "downloads/SHA256_hashes/bookIndex.json")
            await s3.UploadAsync("bookIndex.json", Path.Combine(AppContext.BaseDirectory, "downloads/SHA256_hashes/bookIndex.json"), "SHA256_hashes");


            //deleting books that don't exist in the index from the bucket
            foreach (var book in bucketStuff)
            {
                var fileName = (book.Replace("my_books/", "")).Replace(".pdf","");
                if (alreadyValid.Contains(fileName))
                    continue; //skip already validated books
                if (!indexedStuff.Contains(fileName))
                {
                    //book doesn't exist in the index, delete from bucket
                    await s3.DeleteObjectAsync(book);
                    Console.WriteLine($"Deleted {fileName} from bucket as it does not exist in the index.");
                }
            }

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

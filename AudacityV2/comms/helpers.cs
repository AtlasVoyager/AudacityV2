using Amazon.Runtime.Internal.Transform;
using AudacityV2.AWS;
using AudacityV2.Utils;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using VersOne.Epub;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace AudacityV2.comms
{
    public class Helpers
    {
        private readonly S3Helper s3;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public Helpers(S3Helper s3Helper)
        {
            s3 = s3Helper ?? throw new ArgumentException(nameof(s3Helper));

        }

        public async void ProcessBookAsync(ReadOrder? r)
        {
            //check for null
            if (r == null)
                return;
            //1. Parse book
            if (r.Stage == ReadOrder.currentStage.unparsed)
            {
                if (await ParseBookAsync(r))
                    r.Stage = ReadOrder.currentStage.parsed;
            }
        }
        public async Task<bool> ParseBookAsync(ReadOrder? r)
        {
            if (r == null || r.SelectedBook == null)
                return false;
            //make the book
            await MakeBook(r.SelectedBook.Hash);

            //get index
            var index = await GetIndexAsync();
            //map the hash to the file path
            var filePath = index.Where(i => i.Key.Equals(r.SelectedBook.Hash)).Select(kvp => kvp.Value.Title + ".pdf").FirstOrDefault();
            //download the book from the S3 bucket
            if (filePath == null)
                return false;
            var localPath = await s3.DownloadAsync(filePath);
            //NOW we can parse the book
            //load the EPub file
            EpubBook book = EpubReader.ReadBook(localPath);

            //get table of contents
            if (book.Navigation.Count > 0)
            {
            }

            //cut the book into snippets





            return true;
        }

        public void SendToUser()
        {

        }

        public Metadata MakeMetaData(CommandContext ctx, DiscordAttachment stuff)
        {
            return new Metadata
            {
                Title = HelperUtils.GetEpubTitle(stuff.Url),
                Author = HelperUtils.GetEpubAuthor(stuff.Url),
                UploadedBy = ctx.User.ToString(),
                UploadDate = DateTime.UtcNow,
                FileName = stuff.FileName,
                ChapterCount = HelperUtils.GetEpubChapterCount(stuff.Url)
            };
        }

        public async Task Resolve()
        {
            //goes throught the index first and checks if corresponding book exists
            //if not? delete index. if book exists but index doesn't delete book\

            //get the indexes
            var index = await GetIndexAsync();
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
                var fileName = (book.Replace("my_books/", "")).Replace(".pdf", "");
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

        public async Task<Dictionary<string, Metadata>> GetIndexAsync()
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

        public async Task MakeBook(string hash)
        {
            //check if book exists in the bucket
            var exists = (await s3.BucketContent("parsed_books/")).Select(x => x.Equals(hash)).FirstOrDefault(false);

            if (exists)
                return; //book already exists
            //make an empty json file and upload to s3
            var data = new { };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);

            await s3.UploadAsync((hash + ".json", "parsed_books"), json);

        }

    }
}

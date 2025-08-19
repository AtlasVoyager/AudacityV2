using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using AudacityV2.AWS;
using AudacityV2.comms;
using AudacityV2.Utils;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Playwright;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AudacityV2.Comms
{
    public class Commands : BaseCommandModule
    {
        [Command("upload")]
        public async Task UploadFile(CommandContext ctx)//ctx needs to be in every command
        {   //idk how else to do this so
            S3Helper s3 = new S3Helper();
            Helpers helpers = new Helpers(s3);


            if (ctx.Channel.Id != 1399218934229635072)
            {
                await ctx.Channel.SendMessageAsync("(σ｀д′)σ This command can only be used in the uploads channel.");
                return;
            }
            else
            {
                var msg = ctx.Message;
                var attatchment = msg.Attachments;
                if (attatchment == null)
                {
                    await ctx.Channel.SendMessageAsync("(σ｀д′)σ Give me something to work with here.");
                    return;
                }
                if (attatchment.Count >= 1)
                {
                    if (attatchment.Count > 10)
                    {
                        await ctx.Channel.SendMessageAsync("(σ｀д′)σ Okay that's too much!!!");
                        return;
                    }

                    Dictionary<string, Metadata> data = await helpers.GetIndex();
                    //make a new task per book
                    foreach (var stuff in attatchment)
                    {
                        //I only want pdfs for now
                        if (!stuff.MediaType.Contains("pdf"))
                        {
                            await ctx.Channel.SendMessageAsync("(σ｀д′)σ The attatchment needs to be a pdf");
                            continue; //should this be continue?
                        }

                        //get the title of the book
                        string title = HelperUtils.GetPdfTitle(stuff.Url);
                        //await ctx.Channel.SendMessageAsync($"(σ｀д′)σ book title: {title}");
                        //generate hash first
                        using var httpClient = new HttpClient();
                        var stream = await httpClient.GetStreamAsync(stuff.Url);
                        string thisHash = HelperUtils.GenHash(stream);

                        //make make meta class
                        var thisMeta = helpers.MakeMetaData(ctx, stuff);

                        if (data.ContainsKey(thisHash))
                        {
                            await ctx.Channel.SendMessageAsync($"(σ｀д′)σ This book already exists in the database! {data[thisHash].Title} by {data[thisHash].Author}");
                            continue; //skip to the next attatchment
                        }



                        //add the new book to the index
                        data.Add(thisHash, thisMeta);
                        //serialize the index back to json
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        string updatedJson = JsonSerializer.Serialize(data, options);
                        //write the json back to the file
                        await File.WriteAllTextAsync("downloads/SHA256_hashes/bookIndex.json", updatedJson);
                        //upload the updated index file Path.Combine(AppContext.BaseDirectory, "downloads/SHA256_hashes/bookIndex.json")
                        await s3.UploadAsync("bookIndex.json", Path.Combine(AppContext.BaseDirectory, "downloads/SHA256_hashes/bookIndex.json"), "SHA256_hashes");
                        await ctx.Channel.SendMessageAsync($"(●'◡'●) Uploading {title} by {thisMeta.Author} with {thisMeta.PageCount} pages." +
                            $"\n==============================\n Uploaded by {thisMeta.UploadedBy} on {thisMeta.UploadDate}." +
                            $"\n==============================\n Hash: {thisHash}\n\n");

                        //upload the book to s3
                        await s3.UploadAsync(title + ".pdf", stuff.Url, "my_books");

                        //await s3.UploadAsync("SHA256_hashes/bookIndex.json", Path.Combine(AppContext.BaseDirectory, "downloads/SHA256_hashes/bookIndex.json"));

                    }
                    //s3.Dispose(); //dispose of the S3Helper instance
                    return;
                }
            }
        }

        [Command("readToMe")]
        public async Task ReadBook(CommandContext ctx, string searchTerm)
        {
            //search for the book
            using S3Helper s3 = new S3Helper();
            Helpers helpers = new Helpers(s3);

            //get the updated bookindex file
            Console.WriteLine(await s3.DownloadAsync("SHA256_hashes/bookIndex.json"));


            return;
        }

        [Command("resolve")]
        public async Task Resolve(CommandContext ctx)
        {
            Helpers helpers = new Helpers(new S3Helper());
            await helpers.Resolve();
        }

        [Command("deleteBook")]
        public async Task DeleteBook(CommandContext ctx, string sTerm)
        {
            //takes the search term, uses the serch helper function to find the closest book, user enters the ID number of the book they want to delete, deletes. Can only delete if u are atlas.pk or the original uploader
            //deleting removes it from the index as well
        }

        [Command("websearch")]
        public async Task BookSearch(CommandContext ctx, string searchTerm)
        {
            if (ctx.Channel.Id != 1399218934229635072)
            {
                await ctx.Channel.SendMessageAsync("(σ｀д′)σ This command can only be used in the uploads channel.");
                return;
            }
            else
            {
                string url = $"https://oceanofpdf.com/page/1/?s={Uri.EscapeDataString(searchTerm)}";
                string output = "Link for {searchTerm}:";
                for (int len = 0; len < url.Length + 7; len += 1)
                {
                    output += "=";
                }
                output += $"\n♪(´▽｀) {url} ♪(´▽｀)\n";
                await ctx.Channel.SendMessageAsync($"Link for {searchTerm}: \n==========================================\n♪(´▽｀) {url}\n==========================================");

            }

        }





    }
}
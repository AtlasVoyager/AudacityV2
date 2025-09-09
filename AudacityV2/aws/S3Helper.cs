using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AudacityV2.Utils;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace AudacityV2.AWS
{
    public class S3Helper : IDisposable
    {
        public static string BucketName = "atlasv-library";
        public static readonly RegionEndpoint region = RegionEndpoint.CACentral1;
        private static IAmazonS3 s3Client = new AmazonS3Client(region);

        public static readonly string DownloadDirectory = Path.Combine(AppContext.BaseDirectory, "downloads");

        /// <summary>
        /// Turns everything into a stream. Accepts Local files, URL's, text, bytes, and json 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static async Task<Stream> ToStreamAsync(object input)
        {
            switch (input)
            {
                case Stream s:
                    return s;

                case string filePath when File.Exists(filePath):
                    return File.OpenRead(filePath);

                case string url when Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps):
                    {
                        using var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var memStream = new MemoryStream();
                        await response.Content.CopyToAsync(memStream);
                        memStream.Position = 0;
                        return memStream;
                    }

                case string text:
                    return new MemoryStream(Encoding.UTF8.GetBytes(text));

                case byte[] bytes:
                    return new MemoryStream(bytes);

                default:
                    string json = JsonSerializer.Serialize(input);
                    return new MemoryStream(Encoding.UTF8.GetBytes(json));
            }
        }

        /// <summary>
        /// for local and online files (HTTP/HTTPS) that we want to upload to S3
        /// </summary>
        /// <param name="kName"></param>
        /// <param name="fPath"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /*public async Task UploadAsync(string kName, string fPath, string prefix)
        {
            try
            {
                bool isOnline = Uri.TryCreate(fPath, UriKind.Absolute, out var uriResult) &&
                                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                await using var memStream = new MemoryStream();

                if (isOnline)
                {
                    using var httpClient = new HttpClient();
                    using var response = await httpClient.GetAsync(fPath);
                    response.EnsureSuccessStatusCode();

                    await using var inputStream = await response.Content.ReadAsStreamAsync();
                    await inputStream.CopyToAsync(memStream);
                    memStream.Position = 0;
                }
                else
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(fPath);
                    memStream.Write(fileBytes, 0, fileBytes.Length);
                    memStream.Position = 0;
                }

                //Build full key with prefix
                string fullKey = string.IsNullOrEmpty(prefix)
                    ? kName.Replace("\\", "/")
                    : $"{prefix.TrimEnd('/')}/{kName.Replace("\\", "/")}";

                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = fullKey,
                    InputStream = memStream,
                    ContentType = GetContentType(fPath)
                    
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }*/

        /// <summary>
        /// Uploads something to my S3 bucket. 
        /// </summary>
        /// <param name="keyName">Name the file will take in the s3 bucket</param>
        /// <param name="objStream"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public async Task UploadAsync(string keyName, object src, string prefix = "")
        {
            using var stream = await ToStreamAsync(src); // your auto-stream converter
            var transfer = new TransferUtility(s3Client);
            await transfer.UploadAsync(stream, BucketName, prefix + keyName);

        }


        /// <summary>
        /// Uploads the specified content to an S3 bucket using the provided key and prefix.
        /// </summary>
        /// <remarks>This method uploads the content to the S3 bucket specified by the <c>BucketName</c>
        /// property. Ensure that the <c>s3Client</c> is properly configured and authenticated before calling this
        /// method.</remarks>
        /// <param name="fileStuff">A tuple containing the key and prefix for the object to be uploaded. The <c>key</c> represents the unique
        /// identifier for the object, and the <c>prefix</c> represents the folder or path within the bucket.</param>
        /// <param name="content">The content to be uploaded as a string. The content is stored with a content type of <c>application/json</c>.</param>
        /// <returns>A task that represents the asynchronous upload operation.</returns>
/*        public async Task UploadAsync((string key, string prefix) fileStuff, string content)
        {
            try
            {
                //Build full key with prefix
                string fullKey = string.IsNullOrEmpty(fileStuff.prefix)
                    ? fileStuff.key.Replace("\\", "/")
                    : $"{fileStuff.prefix.TrimEnd('/')}/{fileStuff.key.Replace("\\", "/")}";

                var request = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = fullKey,
                    ContentBody = content,
                    ContentType = "application/json"
                   
                };

                await s3Client.PutObjectAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
*/
        public async Task<string> DownloadAsync(string kName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = kName
                };

                var response = await s3Client.GetObjectAsync(request);

                string path = HelperUtils.GetDownloadPath(kName, DownloadDirectory);
                await using var responseStream = response.ResponseStream;
                await using var fileStream = File.Create(path);

                await responseStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                return path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task DeleteObjectAsync(string kName)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = BucketName,
                    Key = kName
                };
                await s3Client.DeleteObjectAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting object {kName}: {ex}");
            }
        }

        public void Dispose()
        {
            if (s3Client != null)
            {
                s3Client.Dispose();
                s3Client = null;
            }
        }

        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(path, out string contentType)
                ? contentType
                : "application/octet-stream";
        }

        public async Task<List<string>> BucketContent(string prefix = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
                    prefix += "/";

                var list = await s3Client.GetAllObjectKeysAsync(
                    bucketName: BucketName,
                    prefix: prefix,
                    additionalProperties: new Dictionary<string, object>()
                );

                return list.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing bucket contents: {ex}");
                return new List<string>();
            }
        }


    }
}

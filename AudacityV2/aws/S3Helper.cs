using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AudacityV2.comms;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2.AWS
{
    public class S3Helper : IDisposable
    {
        private Helpers helper = new Helpers();
        public static string BucketName = "atlasv-library";

        public static readonly RegionEndpoint region = RegionEndpoint.CACentral1;
        private static IAmazonS3 s3Client = new AmazonS3Client(region);

        //Directory to store downloaded files
        public static readonly string DownloadDirectory = Path.Combine(AppContext.BaseDirectory, "downloads");
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kName">Key name</param>
        /// <param name="fPath">File path (Url or file Dir)</param>
        /// <param name="online">is it a local or online file</param>
        /// <returns></returns>
        public async Task UploadAsync(string kName, string fPath)
        {
            try
            {
                // Detect if fPath is a URL (starts with http or https)
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
                    memStream.Position = 0; // Reset for reading


                    await inputStream.CopyToAsync(memStream);
                    memStream.Position = 0; // Reset position for reading
                }
                else
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(fPath);
                    memStream.Write(fileBytes, 0, fileBytes.Length);
                    memStream.Position = 0; // Reset for reading
                }


                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = kName.Replace("\\", "/"),
                    InputStream = memStream,
                    ContentType = GetContentType(fPath) // Assuming the content type is PDF
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public async Task<string> DownloadAsync(string kName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = kName
                };

                //get the object from aws
                var response = await s3Client.GetObjectAsync(request);

                await using var responseStream = response.ResponseStream; //get the response stream
                await using var fileStream = File.Create(helper.GetDownloadPath(kName, DownloadDirectory));

                await responseStream.CopyToAsync(fileStream); //copy the response stream to a memory stream
                await fileStream.FlushAsync();
                return helper.GetDownloadPath(kName, DownloadDirectory); //return the path to the file
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
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
            if (provider.TryGetContentType(path, out string contentType))
                return contentType;

            return "application/octet-stream"; // Default fallback
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

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AudacityV2.Utils;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudacityV2.AWS
{
    public class S3Helper : IDisposable
    {
        public static string BucketName = "atlasv-library";
        public static readonly RegionEndpoint region = RegionEndpoint.CACentral1;
        private static IAmazonS3 s3Client = new AmazonS3Client(region);

        public static readonly string DownloadDirectory = Path.Combine(AppContext.BaseDirectory, "downloads");

        public async Task UploadAsync(string kName, string fPath, string prefix)
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

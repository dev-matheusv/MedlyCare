using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SFA.Application.Storage;

namespace SFA.Api.Storage;

public class S3FileStorageService : IFileStorageService
{
  private readonly IAmazonS3 _s3;
  private readonly S3Options _options;

  public S3FileStorageService(IAmazonS3 s3, IOptions<S3Options> options)
  {
    _s3 = s3;
    _options = options.Value;

    if (string.IsNullOrWhiteSpace(_options.BucketName))
      throw new InvalidOperationException("AWS:S3:BucketName não configurado.");
  }

  public async Task<string> UploadAsync(
    string key,
    Stream content,
    string contentType,
    CancellationToken cancellationToken = default)
  {
    if (content.CanSeek)
      content.Position = 0;

    var request = new PutObjectRequest
    {
      BucketName = _options.BucketName,
      Key = key,
      InputStream = content,
      ContentType = contentType
    };

    await _s3.PutObjectAsync(request, cancellationToken);

    return key;
  }

  public async Task<Stream> DownloadAsync(
    string key,
    CancellationToken cancellationToken = default)
  {
    var response = await _s3.GetObjectAsync(_options.BucketName, key, cancellationToken);

    var memory = new MemoryStream();
    await response.ResponseStream.CopyToAsync(memory, cancellationToken);
    memory.Position = 0;

    return memory;
  }

  public async Task DeleteAsync(
    string key,
    CancellationToken cancellationToken = default)
  {
    var request = new DeleteObjectRequest
    {
      BucketName = _options.BucketName,
      Key = key
    };

    await _s3.DeleteObjectAsync(request, cancellationToken);
  }
}

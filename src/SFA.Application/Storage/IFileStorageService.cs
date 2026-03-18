namespace SFA.Application.Storage;

public interface IFileStorageService
{
  Task<string> UploadAsync(
    string key,
    Stream content,
    string contentType,
    CancellationToken cancellationToken = default);

  Task<Stream> DownloadAsync(
    string key,
    CancellationToken cancellationToken = default);

  Task DeleteAsync(
    string key,
    CancellationToken cancellationToken = default);
}

namespace SFA.Api.Storage;

public class S3Options
{
  public const string SectionName = "AWS:S3";

  public string BucketName { get; set; } = string.Empty;
}

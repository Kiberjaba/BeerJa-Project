using Amazon.S3;
using Amazon.S3.Model;

namespace BeejaServer.Services;

public sealed class YandexStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public YandexStorageService(IConfiguration config)
    {
        _bucket = config["YandexStorage:BucketName"]
            ?? throw new InvalidOperationException(
                "YandexStorage:BucketName не настроен");

        var accessKey = config["YandexStorage:AccessKey"]
            ?? throw new InvalidOperationException(
                "YandexStorage:AccessKey не настроен");

        var secretKey = config["YandexStorage:SecretKey"]
            ?? throw new InvalidOperationException(
                "YandexStorage:SecretKey не настроен");

        _s3 = new AmazonS3Client(
            accessKey,
            secretKey,
            new AmazonS3Config
            {
                ServiceURL = "https://s3.yandexcloud.net",
                ForcePathStyle = true
            });
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string folder = "images",
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Файл пустой");

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException(
                "Недопустимый формат изображения");

        var key = $"{folder}/{Guid.NewGuid():N}{extension}";

        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType
        };

        await _s3.PutObjectAsync(request, ct);

        return $"https://{_bucket}.storage.yandexcloud.net/{key}";
    }
}

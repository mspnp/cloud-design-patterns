
namespace Pnp.Samples.ClaimCheckPattern
{
    public interface ISampleBlobDataMover
    {
        Task<string> DownloadAsync(Uri blobUri, bool deleteAfter = true);
        Task<string> UploadAsync(string blobUri, string containerName, string content);
    }
}
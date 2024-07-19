using Azure.Storage.Blobs;
using LeafLINQWeb.Models;

namespace LeafLINQWeb.Utilities;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(BlobServiceClient blobServiceClient, string containerName)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = containerName;
    }

    public async Task<string> UploadImageAsync(IFormFile file, int userID)
    {
        // Get a reference to the container
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Create the container if it doesn't exist
        await containerClient.CreateIfNotExistsAsync();

        // Generate a unique name for the image blob
        string imageName = userID.ToString() + Path.GetExtension(file.FileName);

        // Get a reference to the blob
        BlobClient blobClient = containerClient.GetBlobClient(imageName);

        // Upload the image to the blob
        using (var fileStream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(fileStream, true);
        }

        // Return the URL of the uploaded image
        return $"https://leaflinq.blob.core.windows.net/leaf-linq-images/{@imageName}";
    }
}

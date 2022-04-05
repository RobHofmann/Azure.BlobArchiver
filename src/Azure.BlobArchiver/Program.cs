using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dasync.Collections;

public class Program
{
    static async Task Main(string[] args)
    {
        var program = new Program();
        try
        {
            await program.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task RunAsync()
    {
        int uploadThreads = 0;
        if (!int.TryParse(Environment.GetEnvironmentVariable("UPLOAD_THREADS"), out uploadThreads))
            throw new ArgumentException("Please define the environment variable UPLOAD_THREADS to define the number of blob upload threads.");
        bool deleteOnUpload = false;
        if (!bool.TryParse(Environment.GetEnvironmentVariable("DELETE_ON_UPLOAD"), out deleteOnUpload))
            throw new ArgumentException("Please define the environment variable DELETE_ON_UPLOAD=true/false to define if a uploaded file should be removed from the local disk.");
        string blobTierString = Environment.GetEnvironmentVariable("BLOB_TIER");
        AccessTier blobTier = default;
        switch (blobTierString)
        {
            case "Hot":
                blobTier = AccessTier.Hot;
                break;
            case "Cool":
                blobTier = AccessTier.Cool;
                break;
            case "Archive":
                blobTier = AccessTier.Archive;
                break;
            default:
                throw new ArgumentException("Please define the environment variable BLOB_TIER=Hot/Cool/Archive to define the uploaded file's access tier.");
        }
        int gracePeriodBeforeUploadingInSeconds = 0;
        if (!int.TryParse(Environment.GetEnvironmentVariable("GRACE_PERIOD_BEFORE_UPLOADING_IN_SECONDS"), out gracePeriodBeforeUploadingInSeconds))
            throw new ArgumentException("Please define the environment variable GRACE_PERIOD_BEFORE_UPLOADING_IN_SECONDS to define the number of days to keep as retention for your blobs.");
        string fileExclusionListString = Environment.GetEnvironmentVariable("EXCLUDE_FILES");
        string[] fileExclusionList = null;
        if (!string.IsNullOrWhiteSpace(fileExclusionListString))
            fileExclusionList = fileExclusionListString.Split(',');
        string baseDir = Environment.GetEnvironmentVariable("BASE_DIR");
        string blobStorageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTIONSTRING");
        string blobStorageContainerName = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONTAINERNAME");

        Console.WriteLine($"Hello, World! Azure.BlobArchiver starting to upload files from {baseDir}.");
        Console.WriteLine($"Connecting to blob storage account");
        BlobContainerClient blobContainerClient = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);
        Console.WriteLine($"Connected to blob storage account");
        Console.WriteLine($"Starting upload...");
        await RunUploadAsync(blobContainerClient, baseDir, uploadThreads, deleteOnUpload, blobTier, gracePeriodBeforeUploadingInSeconds, fileExclusionList);
        //RunUpload(blobContainerClient, baseDir, uploadThreads, deleteOnUpload, blobTier, gracePeriodBeforeUploadingInSeconds, fileExclusionList);
        Console.WriteLine($"Finished upload...");
    }

    public async Task RunUploadAsync(BlobContainerClient blobContainerClient, string baseDir, int uploadThreads, bool deleteOnUpload, AccessTier blobTier, int gracePeriodBeforeUploadingInSeconds, string[] fileExclusionList)
    {
        await Directory.EnumerateFiles(baseDir, "", SearchOption.AllDirectories).Select(x => new FileInfo(x)).Where(x => x.CreationTime < DateTime.Now.AddSeconds(gracePeriodBeforeUploadingInSeconds * -1)).ParallelForEachAsync(async file =>
        {
            if (fileExclusionList.Contains(file.FullName))
                return;

            Console.WriteLine($"Uploading {file.FullName}");
            var uploadResult = await UploadFileAsync(baseDir, file, blobContainerClient, deleteOnUpload, blobTier);
            if (uploadResult)
                Console.WriteLine($"Succesfully uploaded {file.FullName}");
            else
                Console.WriteLine($"Failed uploading {file.FullName}");
        }, maxDegreeOfParallelism: uploadThreads);
    }

    public void RunUpload(BlobContainerClient blobContainerClient, string baseDir, int uploadThreads, bool deleteOnUpload, AccessTier blobTier, int gracePeriodBeforeUploadingInSeconds, string[] fileExclusionList)
    {
        Parallel.ForEach(Directory.EnumerateFiles(baseDir, "", SearchOption.AllDirectories).Select(x => new FileInfo(x)).Where(x => x.CreationTime < DateTime.Now.AddSeconds(gracePeriodBeforeUploadingInSeconds * -1)),
            new ParallelOptions() { MaxDegreeOfParallelism = uploadThreads },
            file =>
        {
            if (fileExclusionList.Contains(file.FullName))
                return;

            Console.WriteLine($"Uploading {file.FullName}");
            var uploadResult = UploadFile(baseDir, file, blobContainerClient, deleteOnUpload, blobTier);
            if (uploadResult)
                Console.WriteLine($"Succesfully uploaded {file.FullName}");
            else
                Console.WriteLine($"Failed uploading {file.FullName}");
        });
    }

    public async Task<bool> UploadFileAsync(string baseDir, FileInfo file, BlobContainerClient blobContainerClient, bool deleteOnUpload, AccessTier blobTier)
    {
        var fileName = file.FullName.Replace(baseDir + Path.DirectorySeparatorChar, "");
        var blobClient = blobContainerClient.GetBlobClient(fileName);
        Console.WriteLine("Checking if file exists...");
        if (await blobClient.ExistsAsync())
        {
            if (deleteOnUpload)
            {
                file.Delete();
                Console.WriteLine("File exists, deleted locally...");
            }
            return true;
        }

        try
        {
            Console.WriteLine($"Uploading {file.FullName}...");
            await blobClient.UploadAsync(file.FullName, new BlobUploadOptions() { AccessTier = blobTier }).ConfigureAwait(false);
            Console.WriteLine($"Uploaded {file.FullName}...");

            if (deleteOnUpload)
            {
                Console.WriteLine("Deleting file...");
                file.Delete();
            }
        }
        catch (RequestFailedException requestFailedException)
        {
            Console.WriteLine($"{fileName} failed uploading because: {requestFailedException}");
            return false;
        }

        return true;
    }

    public bool UploadFile(string baseDir, FileInfo file, BlobContainerClient blobContainerClient, bool deleteOnUpload, AccessTier blobTier)
    {
        var fileName = file.FullName.Replace(baseDir + Path.DirectorySeparatorChar, "");
        var blobClient = blobContainerClient.GetBlobClient(fileName);

        if (blobClient.Exists())
        {
            if (deleteOnUpload)
                file.Delete();
            return true;
        }

        try
        {
            using (var filestream = file.OpenRead())
                blobClient.Upload(filestream, new BlobUploadOptions() { AccessTier = blobTier });

            if (deleteOnUpload)
                file.Delete();
        }
        catch (RequestFailedException requestFailedException)
        {
            Console.WriteLine($"{fileName} failed uploading because: {requestFailedException}");
            return false;
        }

        return true;
    }
}
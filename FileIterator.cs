using System.Collections;
using Amazon;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System.Configuration;

class FileIterator : IEnumerable<FileContent> 
{
    readonly IEnumerator<FileContent> fileEnumerator;
    public FileIterator(string path)
    {
        if (path.StartsWith("s3://", StringComparison.CurrentCultureIgnoreCase))
        {
            fileEnumerator = new S3FileEnumerator(path);
        }
        else
        {
            fileEnumerator = new LocalFileEnumerator(path);
        }
    }

    public IEnumerator<FileContent> GetEnumerator()
    {
        return fileEnumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return fileEnumerator;
    }
}

public class LocalFileEnumerator : IEnumerator<FileContent>
{
    readonly IEnumerator<string> myFiles;
    FileContent? currentFile;
    public LocalFileEnumerator(string path)
    {
        myFiles = Directory.EnumerateFiles(path).GetEnumerator();
        currentFile = null;
    }
    public FileContent Current 
    {
        get{
            if (currentFile == null)
            {
                throw new InvalidOperationException();
            }
            return currentFile;
        }
    }

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        myFiles.Dispose();
    }

    public bool MoveNext()
    {
        if (myFiles.MoveNext())
        {
            string filePath = myFiles.Current;
            currentFile = new FileContent(filePath, File.ReadAllText(filePath));
            return true;
        }
        else
        {
            currentFile = null;
            return false;
        }
    }

    public void Reset()
    {
        myFiles.Reset();
    }
}

public class S3FileEnumerator : IEnumerator<FileContent>
{
    readonly IEnumerator<S3Object> myFiles;
    readonly AmazonS3Client s3Client;
    FileContent? currentFile;
    readonly string bucketName;
    public S3FileEnumerator(string path)
    {
        //AWSConfigs.AWSProfileName = "884617963414_CH-Developer";
        //AWSConfigs.AWSRegion = "ca-central-1";
        string? profileName = ConfigurationManager.AppSettings["AWSProfileName"];

        AWSCredentials? awsCredentials = null;
        if (profileName!=null)
        {
            Console.WriteLine($"Trying to use supplied profile: {profileName}");
            var sharedCredentialsFile = new CredentialProfileStoreChain();
            if (sharedCredentialsFile.TryGetAWSCredentials(profileName, out awsCredentials))
            {
                Console.WriteLine("Profile was loaded successfully");
            } 
        }
        if (awsCredentials == null)
        {
            Console.WriteLine("Will use EC2 instance profile");
            awsCredentials = new InstanceProfileAWSCredentials();
        }

        s3Client = new AmazonS3Client(awsCredentials);
        AmazonS3Uri s3URI = new AmazonS3Uri(path);
        bucketName = s3URI.Bucket;
        var task = s3Client.ListObjectsAsync(bucketName, s3URI.Key);
        task.Wait();
        
        myFiles = task.Result.S3Objects.GetEnumerator();
        currentFile = null;
    }

    public FileContent Current 
    {
        get{
            if (currentFile == null)
            {
                throw new InvalidOperationException();
            }
            return currentFile;
        }
    }

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        myFiles.Dispose();
    }

    public bool MoveNext()
    {
        // filter out full sized files, for AWs it is always root path
        while (myFiles.MoveNext())
        {
            currentFile = fetchFile(myFiles.Current.Key);
            if (currentFile != null)
            {
                return true;
            }
        }
        currentFile = null;
        return false;
    }

    FileContent? fetchFile(string key)
    {
        var resp = s3Client.GetObjectAsync(bucketName, key);
        resp.Wait();
        if (resp.Result.ContentLength>0)
        {
            string? contents = null;
            using (StreamReader reader = new StreamReader(resp.Result.ResponseStream))
            {
                contents = reader.ReadToEnd();
            }
            return new FileContent(key, contents);
        }
        else
        {
            return null;
        }
    }
    public void Reset()
    {
        myFiles.Reset();
    }
}
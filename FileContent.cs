public class FileContent(string fileName, string content)
{
    readonly string fileName = fileName;
    readonly string content = content;

    public string FileName
    {
        get => fileName;
    }

    public string Content
    {
        get => content;
    }
}
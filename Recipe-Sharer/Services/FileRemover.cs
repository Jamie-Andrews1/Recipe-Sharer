public sealed class FileRemover
{
    private readonly string _filePath;

    public FileRemover(string filePath)
    {
        _filePath = filePath;
    }

    public bool Remove()
    {
        try
        {
            File.Delete(_filePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

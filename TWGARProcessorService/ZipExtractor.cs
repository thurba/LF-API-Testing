namespace TWGARProcessorService;

using System.IO;
using System.IO.Compression;

public static class ZipExtractor
{
    public static void Extract(string zipFilePath, string extractPath)
    {
        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, true);

        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
    }
}
using SamSharp;

using NAudio.Wave;

using System.Reflection;
using System.Security.Cryptography;

namespace SamSharp_Tests;

public class GoldenWordsTests
{
    private readonly Sam sam = new();

    private readonly static DirectoryInfo goldensDirectory;
    private readonly static DirectoryInfo goldenWordsDirectory;

    static GoldenWordsTests()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var assemblyName =
            executingAssembly.ManifestModule.Name.SubstringUpTo(".dll");

        var executingAssemblyDll = new FileInfo(executingAssembly.Location);
        var executingAssemblyDir = executingAssemblyDll.Directory;

        var currentDir = executingAssemblyDir;
        while (currentDir?.Name != assemblyName)
        {
            currentDir = currentDir?.Parent;
        }

        Assert.IsNotNull(currentDir);

        var gloTestsDir = currentDir;
        goldensDirectory = gloTestsDir.GetDirectories("goldens").Single();
        goldenWordsDirectory =
            goldensDirectory.GetDirectories("words").Single();
    }

    private static IEnumerable<string> GetGoldenWords() =>
        File.ReadAllLines(
                goldensDirectory.GetFiles("words/list.txt").Single().FullName)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

    [Test]
    [TestCaseSource(nameof(GetGoldenWords))]
    public async Task TestGoldens(string word)
    {
        var audio = await sam.SpeakAsync(word);

        var provider = new RawSourceWaveStream(new MemoryStream(audio),
            new WaveFormat(22050, 8, 1));

        var outputPath =
            Path.Join(goldenWordsDirectory.FullName, $"{word}.ogg");
        if (!File.Exists(outputPath))
        {
            WaveFileWriter.CreateWaveFile(outputPath, provider);
        }
        else
        {
            var expectedStream = new WaveFileReader(outputPath);
            var actualStream = provider;
            AssertStreamsEqual(expectedStream, actualStream);
        }
    }

    private static void AssertStreamsEqual(Stream expected, Stream actual) =>
        Assert.AreEqual(ComputeHash(expected), ComputeHash(actual));

    private static byte[] ComputeHash(Stream data)
    {
        using HashAlgorithm algorithm = MD5.Create();
        byte[] bytes = algorithm.ComputeHash(data);
        data.Seek(0,
            SeekOrigin
                .Begin); //I'll use this trick so the caller won't end up with the stream in unexpected position
        return bytes;
    }
}

static class StringExtensions
{
    public static string SubstringUpTo(this string str, string substr)
    {
        var indexTo = str.IndexOf(substr);
        return indexTo >= 0 ? str[..indexTo] : str;
    }
}
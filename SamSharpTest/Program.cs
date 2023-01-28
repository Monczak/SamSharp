using SamSharp;
using SamSharp.Parser;
using SamSharp.Reciter;

namespace SamSharpTest;

public static class Program
{
    public static void Main(string[] args)
    {
        Reciter reciter = new Reciter();
        Parser parser = new Parser();

        string phonemes = reciter.TextToPhonemes("Hello, my name is SAM and this is a test thing.");
        Console.WriteLine(phonemes);

        var parseResult = parser.Parse(phonemes);
        foreach (var data in parseResult)
        {
            Console.WriteLine($"{data.phoneme}\t{data.length}\t{data.stress}");
        }
    }
}
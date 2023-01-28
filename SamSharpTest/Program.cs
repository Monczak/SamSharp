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

        string phonemes = reciter.TextToPhonemes("Hello, my name is SAM.");
        Console.WriteLine(phonemes);

        parser.Parse(phonemes);
    }
}
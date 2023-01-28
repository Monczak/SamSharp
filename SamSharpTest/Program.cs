using SamSharp;
using SamSharp.Parser;
using SamSharp.Reciter;
using SamSharp.Renderer;

namespace SamSharpTest;

public static class Program
{
    public static void Main(string[] args)
    {
        Reciter reciter = new Reciter();
        Parser parser = new Parser();
        Renderer renderer = new Renderer();

        string phonemes = reciter.TextToPhonemes("if you're reading this, you're awesome.");
        Console.WriteLine(phonemes);

        var parseResult = parser.Parse(phonemes);
        foreach (var data in parseResult)
        {
            Console.WriteLine($"{data.Phoneme}\t{data.Length}\t{data.Stress}");
        }

        var renderResult = renderer.Render(parseResult, new Renderer.Options());
        File.WriteAllBytes("test.bin", renderResult);
    }
}
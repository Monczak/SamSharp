using SamSharp;
using SamSharp.Reciter;

namespace SamSharpTest;

public static class Program
{
    public static void Main(string[] args)
    {
        Reciter reciter = new Reciter();
        Console.WriteLine(reciter.TextToPhonemes("i'm not working"));
    }
}
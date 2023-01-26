using SamSharp;
using SamSharp.Reciter;

public static class Program
{
    public static void Main(string[] args)
    {
        Reciter reciter = new Reciter();
        Console.WriteLine(reciter.TextToPhonemes("Been spending most their lives, living in the Morshus Paradise."));
    }
}
// See https://aka.ms/new-console-template for more information

using SamSharp;
using SamSharp.Reciter;

public static class Program
{
    public static void Main(string[] args)
    {
        Reciter reciter = new Reciter();
        Console.WriteLine(reciter.TextToPhonemes("computer"));
    }
}
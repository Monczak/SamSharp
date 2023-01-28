using SamSharp;
using SamSharp.Renderer;

namespace SamSharpTest;

public static class Program
{
    public static void Main(string[] args)
    {
        string text = "this is some random text";

        Sam sam = new Sam();
        var audio = sam.Speak(text);
        File.WriteAllBytes("test.bin", audio);
    }
}
using CommandLine;

namespace SamSharpCli;

public class CommandLineOptions
{
    [Value(0, Required = true, HelpText = "The text to speak")]
    public string Text { get; set; } = "";
    
    [Option('p', "pitch", Required = false, Default = 64, HelpText = "Pitch parameter (0 - 255)")]
    public int Pitch { get; set; }
    
    [Option('s', "speed", Required = false, Default = 72, HelpText = "Speed parameter (0 - 255)")]
    public int Speed { get; set; }
    
    [Option('m', "mouth", Required = false, Default = 128, HelpText = "Mouth parameter (0 - 255)")]
    public int Mouth { get; set; }
    
    [Option('t', "throat", Required = false, Default = 128, HelpText = "Throat parameter (0 - 255)")]
    public int Throat { get; set; }
    
    [Option("phonetic", Required = false, Default = false, HelpText = "Use phonetic mode")]
    public bool Phonetic { get; set; }
    
    [Option("sing", Required = false, Default = false, HelpText = "Use sing mode")]
    public bool SingMode { get; set; }

    [Option('o', "output", Required = false, Default = null,
        HelpText = "Path to output .wav file. If not present, will play the audio directly.")]
    public string? OutputFile { get; set; }
}
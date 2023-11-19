using SamSharp;
using CommandLine;
using NAudio.Wave;

namespace SamSharpCli;

public static class Program
{
    public static void Main(string[] args) =>
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(RunOptions);

    private static void RunOptions(CommandLineOptions options)
    {
        string text = options.Text;

        if (options.Pitch is < 0 or > 255)
        {
            Console.WriteLine("Error: pitch must be between 0 and 255");
            return;
        }
        if (options.Mouth is < 0 or > 255)
        {
            Console.WriteLine("Error: mouth must be between 0 and 255");
            return;
        }
        if (options.Throat is < 0 or > 255)
        {
            Console.WriteLine("Error: throat must be between 0 and 255");
            return;
        }
        if (options.Speed is < 0 or > 255)
        {
            Console.WriteLine("Error: speed must be between 0 and 255");
            return;
        }

        Sam sam = new Sam(new Options((byte)options.Pitch, (byte)options.Mouth, (byte)options.Throat, (byte)options.Speed, options.SingMode));
        var audio = options.Phonetic ? sam.SpeakPhonetic(text) : sam.Speak(text);

        IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(audio), new WaveFormat(22050, 8, 1));

        if (options.OutputFile is null)
        {
            using WaveOutEvent waveOut = new WaveOutEvent();
            waveOut.Init(provider);
            waveOut.Play();
            while (waveOut.PlaybackState == PlaybackState.Playing) { }
        }
        else
        { 
            WaveFileWriter.CreateWaveFile(options.OutputFile, provider);
        }
    }
}
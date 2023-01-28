using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SamSharp
{
    public class Sam
    {
        public Renderer.Renderer.Options Options { get; set; }

        public Sam(Renderer.Renderer.Options options)
        {
            Options = options;
        }

        public Sam() : this(new Renderer.Renderer.Options())
        {
        }

        public byte[] Speak(string input)
        {
            Reciter.Reciter reciter = new Reciter.Reciter();
            return SpeakPhonetic(reciter.TextToPhonemes(input));
        }

        public byte[] SpeakPhonetic(string phoneticInput)
        {
            Parser.Parser parser = new Parser.Parser();
            Renderer.Renderer renderer = new Renderer.Renderer();

            var data = parser.Parse(phoneticInput);
            return renderer.Render(data, Options);
        }

        public Task<byte[]> SpeakAsync(string input)
        {
            return Task<byte[]>.Factory.StartNew(() => Speak(input));
        }

        public Task<byte[]> SpeakPhoneticAsync(string phoneticInput)
        {
            return Task<byte[]>.Factory.StartNew(() => SpeakPhonetic(phoneticInput));
        }
    }
}
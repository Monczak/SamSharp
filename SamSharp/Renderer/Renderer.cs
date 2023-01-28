using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        public class Options
        {
            public byte Pitch { get; }
            public byte Mouth { get; }
            public byte Throat { get; }
            public byte Speed { get; }
            public bool SingMode { get; }

            public Options(byte pitch = 64, byte mouth = 128, byte throat = 128, byte speed = 72, bool singMode = false)
            {
                Pitch = pitch;
                Mouth = mouth;
                Throat = throat;
                Speed = speed;
                SingMode = singMode;
            }
        }

        private class Formants
        {
            public Dictionary<int, int> Mouth { get; set; }
            public Dictionary<int, int> Throat { get; set; }
            public Dictionary<int, int> Formant3 { get; set; }

            public Formants()
            {
                Mouth = new Dictionary<int, int>();
                Throat = new Dictionary<int, int>();
                Formant3 = new Dictionary<int, int>();
            }
        }
        
                
        private class FramesData
        {
            public Dictionary<int, int> Pitches { get; set; }
            public Formants Frequencies { get; set; }
            public Formants Amplitudes { get; set; }
            public Dictionary<int, int> SampledConsonantFlags { get; set; }
            
            public int T { get; set; }

            public FramesData()
            {
                Pitches = new Dictionary<int, int>();
                Frequencies = new Formants();
                Amplitudes = new Formants();
                SampledConsonantFlags = new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// Renders audio from an array of phoneme data. 
        /// </summary>
        /// <param name="phonemes">The phoneme data output by the parser.</param>
        /// <param name="options">Speech options such as pitch, mouth/throat, speed and sing mode.</param>
        /// <returns>A byte buffer with audio data.</returns>
        public byte[] Render(Parser.Parser.PhonemeData[] phonemes, Options options)
        {
            var sentences = PrepareFrames(phonemes, options);

            var output = new OutputBuffer(
                (int)(176.4f  // 22050 / 125
                * phonemes.Sum(data => data.Length!.Value)
                * options.Speed)
            );
            
            PrintOutput(sentences);
            
            ProcessFrames(output, sentences.T, options.Speed, sentences);

            return output.Get();
        }

        private void PrintOutput(FramesData framesData)
        {
            Debug.WriteLine("===============================================");
            Debug.WriteLine("Final data for speech output:");
            Debug.WriteLine("flags ampl1 freq1 ampl2 freq2 ampl3 freq3 pitch");
            Debug.WriteLine("-----------------------------------------------");
            for (int i = 0; i < framesData.SampledConsonantFlags.Count; i++)
            {
                Debug.WriteLine($" {framesData.SampledConsonantFlags[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Amplitudes.Mouth[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Frequencies.Mouth[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Amplitudes.Throat[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Frequencies.Throat[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Amplitudes.Formant3[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Frequencies.Formant3[i].ToString().PadLeft(5, '0')}" +
                                $" {framesData.Pitches[i].ToString().PadLeft(5, '0')}");
            }
            Debug.WriteLine("===============================================");
        }
    }
}
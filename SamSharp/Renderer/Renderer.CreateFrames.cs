using System.Collections.Generic;

namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        private const int RisingInflection = 255;
        private const int FallingInflection = 1;

        /**
         * <summary>
         * CREATE FRAMES
         *
         * The length parameter in the list corresponds to the number of frames
         * to expand the phoneme to. At the default speed, each frame represents
         * about 10 milliseconds of time.
         * So a phoneme with a length of 7 = 7 frames = 70 milliseconds duration.
         *
         * The parameters are copied from the phoneme to the frame verbatim.
         *
         * </summary>
         *
         * <param name="pitch">The pitch input.</param>
         * <param name="phonemes">The phoneme data.</param>
         * <param name="frequencies">The frequency data.</param>
         *
         * <returns>A FramesData object with pitches, frequencies, amplitudes and sampled consonant flags.</returns>
         */
        private FramesData CreateFrames(byte pitch, Parser.Parser.PhonemeData[] phonemes,
            Formants frequencies)
        {
            // Create a rising or falling inflection 30 frames prior to index X
            // A rising inflection is used for questions, and a falling inflection is used for statements
            void AddInflection(int inflection, int pos, Dictionary<int, int> pitches)
            {
                // Store the location of the punctuation
                int end = pos;
                if (pos < 30)
                    pos = 0;
                else
                    pos -= 30;

                int a;
                
                // FIXME: Explain this fix better, it's not obvious
                // ML : A =, fixes a problem with invalid pitch with '.'
                while ((a = pitches[pos]) == 127)
                    pos++;

                while (pos != end)
                {
                    // Add the inflection direction
                    a += inflection;
                    
                    // Set the inflection
                    pitches[pos] = a & 0xFF;
                    
                    while (++pos != end && pitches[pos] == 255) { } // Keep looping
                }
            }

            FramesData framesData = new FramesData();
            int x = 0;

            foreach (var data in phonemes)
            {
                // Get the phoneme at the index
                var phoneme = data.Phoneme;

                if (phoneme == PhonemePeriod)
                    AddInflection(FallingInflection, x, framesData.Pitches);
                else if (phoneme == PhonemeQuestion)
                    AddInflection(RisingInflection, x, framesData.Pitches);

                // Get the stress amount (more stress = higher pitch)
                var phase1 = stressPitch_tab47492[data.Stress!.Value];
                
                // Get number of frames to write
                // Copy from the source to the frames list
                for (int frames = data.Length!.Value; frames > 0; frames--)
                {
                    framesData.Frequencies.Mouth[x] = frequencies.Mouth[phoneme!.Value];                // F1 frequency
                    framesData.Frequencies.Throat[x] = frequencies.Throat[phoneme!.Value];              // F2 frequency
                    framesData.Frequencies.Formant3[x] = frequencies.Formant3[phoneme!.Value];          // F3 frequency

                    framesData.Amplitudes.Mouth[x] = amplitudeData[phoneme!.Value] & 0xFF;              // F1 amplitude
                    framesData.Amplitudes.Throat[x] = (amplitudeData[phoneme!.Value] >> 8) & 0xFF;      // F2 amplitude
                    framesData.Amplitudes.Formant3[x] = (amplitudeData[phoneme!.Value] >> 16) & 0xFF;   // F3 amplitude

                    framesData.SampledConsonantFlags[x] = sampledConsonantFlags[phoneme!.Value];        // Phoneme data for sampled consonants
                    framesData.Pitches[x] = (pitch + phase1) & 0xFF;                                    // Pitch

                    x++;
                }
            }
            return framesData;
        }
    }
}
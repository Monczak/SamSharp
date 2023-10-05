namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        /**
         * <summary>
         * RENDER THE PHONEMES IN THE LIST
         *
         * The phoneme list is converted into sound through the steps:
         *
         * 1. Copy each phoneme (length) number of times into the frames list.
         *
         * 2. Determine the transitions lengths between phonemes, and linearly
         *    interpolate the values across the frames.
         *
         * 3. Offset the pitches by the fundamental frequency.
         *
         * 4. Render each frame.
         * </summary>
         *
         * <param name="phonemes">The phoneme data.</param>
         * <param name="options">The input options.</param>
         */
        private FramesData PrepareFrames(Parser.Parser.PhonemeData[] phonemes, Options options)
        {
            var freqData = SetMouthThroat(options.Mouth, options.Throat);
            var frameData = CreateFrames(options.Pitch, phonemes, freqData);
            var t = CreateTransitions(frameData, phonemes);

            if (!options.SingMode)
            {
                /* ASSIGN PITCH CONTOUR
                 *
                 * This subtracts the F1 frequency from the pitch to create a
                 * pitch contour. Without this, the output would be at a single
                 * pitch level (monotone).
                 */
                for (int i = 0; i < frameData.Pitches.Count; i++)
                {
                    // Subtract half the frequency of the mouth formant
                    // This adds variety to the voice
                    frameData.Pitches[i] -= frameData.Frequencies.Mouth[i] >> 1;
                }
            }
            
            /*
             * RESCALE AMPLITUDE
             *
             * Rescale volume from decibels to the linear scale.
             */
            var amplitudeRescale = new[]
            {
                0x00, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03, 0x04,
                0x04, 0x05, 0x06, 0x08, 0x09, 0x0B, 0x0D, 0x0F,
            };

            for (int i = frameData.Amplitudes.Mouth.Count - 1; i >= 0; i--)
            {
                frameData.Amplitudes.Mouth[i] = amplitudeRescale[frameData.Amplitudes.Mouth[i]];
                frameData.Amplitudes.Throat[i] = amplitudeRescale[frameData.Amplitudes.Throat[i]];
                frameData.Amplitudes.Formant3[i] = amplitudeRescale[frameData.Amplitudes.Formant3[i]];
            }

            frameData.T = t;
            return frameData;
        }
    }
}
using System;

namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        /**
         * CREATE TRANSITIONS
         *
         * Linear transitions are now created to smoothly connect each
         * phoneme. This transition is spread between the ending frames
         * of the old phoneme (outBlendLength), and the beginning frames
         * of the new phoneme (inBlendLength).
         *
         * To determine how many frames to use, the two phonemes are
         * compared using the blendRank[] table. The phoneme with the
         * smaller score is used. In case of a tie, a blend of each is used:
         *
         *      if blendRank[phoneme1] == blendRank[phoneme2]
         *          // use lengths from each phoneme
         *          outBlendFrames = outBlend[phoneme1]
         *          inBlendFrames = outBlend[phoneme2]
         *      else if blendRank[phoneme1] less than blendRank[phoneme2]
         *          // use lengths from first phoneme
         *          outBlendFrames = outBlendLength[phoneme1]
         *          inBlendFrames = inBlendLength[phoneme1]
         *      else
         *          // use lengths from the second phoneme
         *          // note that in and out are swapped around!
         *          outBlendFrames = inBlendLength[phoneme2]
         *          inBlendFrames = outBlendLength[phoneme2]
         *
         * Blend lengths can't be less than zero.
         *
         * For most of the parameters, SAM interpolates over the range of the last
         * outBlendFrames-1 and the first inBlendFrames.
         *
         * The exception to this is the Pitch[] parameter, which is interpolates the
         * pitch from the center of the current phoneme to the center of the next
         * phoneme.
         *
         * <param name="framesData">The frame data.</param>
         * <param name="phonemes">The phoneme data.</param>
         * <returns>The length of the transition.</returns>
         */
        private int CreateTransitions(FramesData framesData, Parser.Parser.PhonemeData[] phonemes)
        {
            // Tables:
            // 0  pitches
            // 1  frequency1
            // 2  frequency2
            // 3  frequency3
            // 4  amplitude1
            // 5  amplitude2
            // 6  amplitude3
            var tables = new[] { framesData.Pitches, framesData.Frequencies.Mouth, framesData.Frequencies.Throat, framesData.Frequencies.Formant3, framesData.Amplitudes.Mouth, framesData.Amplitudes.Throat, framesData.Amplitudes.Formant3};

            int Read(int table, int pos)
            {
                if (table < 0 || table >= tables.Length)
                {
                    throw new Exception($"Invalid table in Read: {table}");
                }

                return tables[table][pos];
            }
            
            // Linearly interpolate values
            void Interpolate(int width, int table, int frame, int change)
            {
                bool sign = change < 0;
                int remainder = Math.Abs(change) % width;
                int div = change / width;

                int error = 0;
                int pos = width;

                while (--pos > 0)
                {
                    int val = Read(table, frame) + div;
                    error += remainder;
                    if (error >= width)
                    {
                        // Accumulated a whole integer error, so adjust output
                        error -= width;
                        if (sign)
                            val--;
                        else if (val != 0)
                            val++;  // If input is 0, we always leave it alone
                    }
                    
                    // Write updated value back to next frame
                    if (table < 0 || table >= tables.Length)
                        throw new Exception($"(Interpolate) Invalid table in Read: {table}");
                    if (frame + 1 < tables[table].Count)
                        tables[table][++frame] = val;
                    val += div; // WTF: This is in the JS code, but this does nothing useful? 
                }
            }

            int boundary = 0;

            for (int pos = 0; pos < phonemes.Length - 1; pos++)
            {
                var phoneme = phonemes[pos].Phoneme;
                var nextPhoneme = phonemes[pos + 1].Phoneme;
                
                // Get the ranking of each phoneme
                var nextRank = blendRank[nextPhoneme!.Value];
                var rank = blendRank[nextPhoneme.Value];
                
                // Compare the rank - lower rank value is stronger
                int outBlendFrames;
                int inBlendFrames;
                if (rank == nextRank)
                {
                    // Same rank, so use out blend lengths from each phoneme
                    outBlendFrames = outBlendLength[phoneme!.Value];
                    inBlendFrames = outBlendLength[nextPhoneme.Value];
                }
                else if (rank < nextRank)
                {
                    // Next phoneme is stronger, so use its blend lengths
                    outBlendFrames = inBlendLength[nextPhoneme.Value];
                    inBlendFrames = inBlendLength[nextPhoneme.Value];
                }
                else
                {
                    // Current phoneme is stronger, so use its blend lengths
                    // Note the out/in are swapped
                    outBlendFrames = outBlendLength[phoneme!.Value];
                    inBlendFrames = inBlendLength[phoneme.Value];
                }

                boundary += phonemes[pos].Length!.Value;

                int transEnd = boundary + inBlendFrames;
                int transStart = boundary - outBlendFrames;
                int transLength = outBlendFrames + inBlendFrames;   // Total transition length

                if (((transLength - 2) & 128) == 0)
                {
                    // Unlike the other values, the pitches[] interpolates from
                    // the middle of the current phoneme to the middle of the
                    // next phoneme
                    
                    // Half the width of the current and next phoneme
                    int currentWidth = phonemes[pos].Length!.Value >> 1;
                    int nextWidth = phonemes[pos + 1].Length!.Value >> 1;
                    int pitch = framesData.Pitches[boundary + nextWidth] - framesData.Pitches[boundary - currentWidth];
                    
                    // Interpolate the values
                    Interpolate(currentWidth + nextWidth, 0, transStart, pitch);

                    for (int table = 1; table <= 6; table++)
                    {
                        // Tables:
                        // 0  pitches
                        // 1  frequency1
                        // 2  frequency2
                        // 3  frequency3
                        // 4  amplitude1
                        // 5  amplitude2
                        // 6  amplitude3
                        var value = Read(table, transEnd) - Read(table, transStart);
                        Interpolate(transLength, table, transStart, value);
                    }
                }
            }
            
            // Add the length of the last phoneme
            return boundary + phonemes[^1].Length!.Value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        sbyte Sine(int x, bool useLookup = true) => 
            useLookup 
                ? (sbyte)sinus[x]
                : (sbyte)(Math.Sin(2 * Math.PI * (x / 256.0)) * 127);
        
        private int RenderSample(OutputBuffer output, int lastSampleOffset, int consonantFlag, int pitch)
        {
            // Mask low 3 bits and subtract 1 to get kind
            int kind = (consonantFlag & 7) - 1;
            
            // Determine which value to use from table { 0x18, 0x1A, 0x17, 0x17, 0x17 }
            // T', S, Z               0          0x18   coronal
            // CH', J', SH, ZH        1          0x1A   palato-alveolar
            // P', F, V, TH, DH       2          0x17   [labio]dental
            // /H                     3          0x17   palatal
            // /X                     4          0x17   glottal
            int samplePage = kind * 256 & 0xFFFF;
            int offset = consonantFlag & 248;
            
            Debug.WriteLine(offset);

            void RenderTheSample(int index1, int value1, int index0, int value0)
            {
                int bit = 8;
                int sample = sampleTable[samplePage + offset];

                do
                {
                    if ((sample & 128) != 0)
                        output.Write(index1, value1);
                    else
                        output.Write(index0, value0);
                    sample <<= 1;
                } while (--bit > 0);
            }

            if (offset == 0)
            {
                // Voiced phoneme: Z*, ZH, V*, DH
                int phase1 = ((pitch >> 4) ^ 255) & 0xFF;
                offset = lastSampleOffset & 0xFF;

                do
                {
                    RenderTheSample(3, 26, 4, 6);
                    offset++;
                    offset &= 0xFF;
                } while ((++phase1 & 0xFF) > 0);

                return offset;
            }
            
            // Unvoiced
            offset = offset ^ 255 & 0xFF;
            int value0 = sampledConsonantValues0[kind] & 0xFF;

            do
            {
                RenderTheSample(2, 5, 1, value0);
            } while ((++offset & 0xFF) > 0);

            return lastSampleOffset;
        }

        /**
         * <summary>
         * PROCESS THE FRAMES
         *
         * In traditional vocal synthesis, the glottal pulse drives filters, which
         * are attenuated to the frequencies of the formants.
         *
         * SAM generates these formants directly with sine and rectangular waves.
         * To simulate them being driven by the glottal pulse, the waveforms are
         * reset at the beginning of each glottal pulse.
         * </summary>
         *
         * <param name="output">The output buffer.</param>
         * <param name="frameCount">The frame count.</param>
         * <param name="speed">The speed input.</param>
         * <param name="framesData">The frame data.</param>
         */
        private void ProcessFrames(OutputBuffer output, int frameCount, int speed, FramesData framesData)
        {
            int speedCounter = speed;
            int phase1 = 0, phase2 = 0, phase3 = 0;
            int lastSampleOffset = 0;
            int pos = 0;
            int glottalPulse = framesData.Pitches[0];
            int mem38 = (int)(glottalPulse * .75f);

            while (frameCount > 0)
            {
                var flags = framesData.SampledConsonantFlags[pos];
                
                // Unvoiced sampled phoneme?
                if ((flags & 248) != 0)
                {
                    lastSampleOffset = RenderSample(output, lastSampleOffset, flags, framesData.Pitches[pos & 0xFF]);
                    // Skip ahead 2 in the phoneme buffer
                    pos += 2;
                    frameCount -= 2;
                    speedCounter = speed;
                }
                else
                {
                    // Rectangle wave consisting of:
                    //   0-128 = 0x90
                    // 128-255 = 0x70
                    
                    // Simulate the glottal pulse and formants
                    int[] ary = new int[5];
                    int p1 = phase1 * 256;
                    int p2 = phase2 * 256;
                    int p3 = phase3 * 256;

                    for (int k = 0; k < 5; k++)
                    {
                        sbyte sp1 = Sine(0xFF & (p1 >> 8));
                        sbyte sp2 = Sine(0xFF & (p2 >> 8));
                        sbyte rp3 = (sbyte)((0xFF & (p3 >> 8)) < 129 ? -0x70 : 0x70);

                        int sin1 = sp1 * (byte)(framesData.Amplitudes.Mouth[pos] & 0x0F);
                        int sin2 = sp2 * (byte)(framesData.Amplitudes.Throat[pos] & 0x0F);
                        int rect = rp3 * (byte)(framesData.Amplitudes.Formant3[pos] & 0x0F);

                        int mux = sin1 + sin2 + rect;
                        mux /= 32;
                        mux += 128; // Go from signed to unsigned amplitude
                        // mux &= 0xF0;

                        ary[k] = mux;
                        p1 += framesData.Frequencies.Mouth[pos] * 256 / 4;  // Compromise, this becomes a shift and works well
                        p2 += framesData.Frequencies.Throat[pos] * 256 / 4;
                        p3 += framesData.Frequencies.Formant3[pos] * 256 / 4; 
                    }
                    output.Ary(0, ary);
                    
                    speedCounter--;
                    if (speedCounter == 0)
                    {
                        pos++;          // Go to next amplitude
                        frameCount--;
                        if (frameCount == 0) return;

                        speedCounter = speed;
                    }

                    glottalPulse--;
                    if (glottalPulse != 0)
                    {
                        // Not finished with a glottal pulse
                        mem38--;
                    
                        // Within the first 75% of a glottal pulse?
                        // Is the count non-zero and the sampled flag is zero?  // WTF: Using || instead of &&?
                        if (mem38 != 0 || flags == 0)
                        {
                            // Update the phase of the formants
                            // TODO: We should have a switch to disable this, it causes a pretty nice voice without the masking!
                            phase1 = phase1 + framesData.Frequencies.Mouth[pos]; // & 0xFF;
                            phase2 = phase2 + framesData.Frequencies.Throat[pos]; // & 0xFF;
                            phase3 = phase3 + framesData.Frequencies.Formant3[pos]; // & 0xFF;
                            continue;
                        }
                    
                        // Voiced sampled phonemes interleave the sample with the glottal pulse
                        // The sample flag is non-zero, so render the sample for the phoneme
                        lastSampleOffset = RenderSample(output, lastSampleOffset, flags, framesData.Pitches[pos & 0xFF]);
                    }
                }
                glottalPulse = framesData.Pitches[Math.Min(pos, framesData.Pitches.Count - 1)];
                mem38 = (int)(glottalPulse * .75f);
            
                // Reset the formant wave generators to keep them in sync with the glottal pulse
                phase1 = 0;
                phase2 = 0;
                phase3 = 0;
            }
        }
    }
}
using System;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        /// <summary>
        /// Makes plosive stop consonants longer by inserting the next two following phonemes from the table
        /// right behind the consonant.
        /// </summary>
        /// <param name="getPhoneme">Callback for retrieving phonemes.</param>
        /// <param name="insertPhoneme">Callback for inserting phonemes.</param>
        /// <param name="getStress">Callback for retrieving stress.</param>
        private void ProlongPlosiveStopConsonantsCode41240(GetPhonemeDelegate getPhoneme,
            InsertPhonemeDelegate insertPhoneme, GetStressDelegate getStress)
        {
            int pos = -1;
            int? phoneme;

            while ((phoneme = getPhoneme(++pos)) != null)
            {
                // Not a stop consonant, move to the next one
                if (!PhonemeHasFlag(phoneme, PhonemeFlags.StopConsonant))
                    continue;
                
                // If plosive, move to the next non-empty phoneme and validate the flags
                if (PhonemeHasFlag(phoneme, PhonemeFlags.UnvoicedStopConsonant))
                {
                    int? nextNonEmpty;
                    int x = pos;

                    do
                    {
                        nextNonEmpty = getPhoneme(++x);
                    } while (nextNonEmpty == 0);
                    
                    // If not END and either flag 0x0008 or "/H" or "/X"
                    if (nextNonEmpty != null && (PhonemeHasFlag(nextNonEmpty, PhonemeFlags._0x0008) ||
                                                 nextNonEmpty == 36 || nextNonEmpty == 37))
                        continue;
                }

                insertPhoneme(pos + 1, phoneme.Value + 1, getStress(pos),
                    combinedPhonemeLengthTable[phoneme.Value + 1] & 0xFF);
                insertPhoneme(pos + 2, phoneme.Value + 2, getStress(pos),
                    combinedPhonemeLengthTable[phoneme.Value + 2] & 0xFF);
                pos += 2;
            }
        }
    }
}
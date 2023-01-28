namespace SamSharp.Parser
{
    public partial class Parser
    {
        /**
         * <summary>
         * Iterates through the phoneme buffer, copying the stress value from
         * the following phoneme under the following circumstance:
         *     1. The current phoneme is voiced, excluding plosives and fricatives
         *     2. The following phoneme is voiced, excluding plosives and fricatives, and
         *     3. The following phoneme is stressed
         *
         *  In those cases, the stress value+1 from the following phoneme is copied.
         *
         * For example, the word LOITER is represented as LOY5TER, with as stress
         * of 5 on the diphthong OY. This routine will copy the stress value of 6 (5+1)
         * to the L that precedes it.
         * </summary>
         *
         * <param name="getPhoneme">Callback for retrieving phonemes.</param>
         * <param name="getStress">Callback for retrieving phoneme stress.</param>
         * <param name="setStress">Callback for setting phoneme stress.</param>
         */
        private void CopyStress(GetPhonemeDelegate getPhoneme, GetStressDelegate getStress, SetStressDelegate setStress)
        {
            // Loop through all the phonemes to be output
            int position = 0;
            int? phoneme;

            while ((phoneme = getPhoneme(position)) != null)
            {
                // If Consonant flag set, skip - only vowels get stressed
                if (PhonemeHasFlag(phoneme, PhonemeFlags.Consonant))
                {
                    phoneme = getPhoneme(position + 1);
                    // If the following phoneme is the end, or a vowel, skip
                    if (phoneme != null && PhonemeHasFlag(phoneme, PhonemeFlags.Vowel))
                    {
                        // Get the stress value at the next position
                        var stress = getStress(position + 1);
                        if (stress != 0 && stress < 0x80)
                        {
                            // If next phoneme is stressed and a Vowel or ER
                            // Copy stress from next phoneme to this one
                            setStress(position, stress + 1);
                        }
                    }
                }

                position++;
            }
        }
    }
}
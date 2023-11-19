using System.Diagnostics;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        /**
         * <summary>
         * Applies various rules that adjust the lengths of phonemes.
         *
         * Lengthen (!FRICATIVE) or (VOICED) between (VOWEL) and (PUNCTUATION) by 1.5
         * (VOWEL) (RX | LX) (CONSONANT) - decrease (VOWEL) length by 1
         * (VOWEL) (UNVOICED PLOSIVE) - decrease vowel by 1/8th
         * (VOWEL) (VOICED CONSONANT) - increase vowel by 1/4 + 1
         * (NASAL) (STOP CONSONANT) - set nasal = 5, consonant = 6
         * (STOP CONSONANT) {optional silence} (STOP CONSONANT) - shorten both to 1/2 + 1
         * (STOP CONSONANT) (LIQUID) - decrease (LIQUID) by 2
         * </summary>
         *
         * <param name="getPhoneme">Callback for retrieving phonemes.</param>
         * <param name="setLength">Callback for setting phoneme length.</param>
         * <param name="getLength">Callback for retrieving phoneme length.</param>
         */
        private void AdjustLengths(GetPhonemeDelegate getPhoneme, SetLengthDelegate setLength,
            GetLengthDelegate getLength)
        {
            Debug.WriteLine("AdjustLengths()");
            
            // LENGTHEN VOWELS PRECEDING PUNCTUATION
            //
            // Search for punctuation. If found, back up to the first vowel, then
            // process all phonemes between there and up to (but not including) the punctuation.
            // If any phoneme is found that is a either a fricative or voiced, the duration is
            // increased by (length * 1.5) + 1
            
            // Loop index
            for (int position = 0; getPhoneme(position) != null; position++)
            {
                // Not punctuation?
                if (!PhonemeHasFlag(getPhoneme(position), PhonemeFlags.Punctuation))
                {
                    continue;
                }

                int loopIndex = position;
                while (--position > 1 && !PhonemeHasFlag(getPhoneme(position), PhonemeFlags.Vowel)) { } // Back up while not a vowel
                // If beginning of phonemes, exit loop
                if (position == 0)
                {
                    break;
                }

                // Now handle everything between position and loopIndex
                int vowel = position;
                for (; position < loopIndex; position++)
                {
                    // Test for not fricative/unvoiced or not voiced
                    if (!PhonemeHasFlag(getPhoneme(position), PhonemeFlags.Fricative) ||
                        PhonemeHasFlag(getPhoneme(position), PhonemeFlags.Voiced))
                    {
                        int a = getLength(position);
                        
                        // Change phoneme length to (length * 1.5) + 1
                        Debug.WriteLine($"{position} RULE: Lengthen <!FRICATIVE> or <VOICED> {GetPhonemeName(getPhoneme(position))} " +
                                        $"between VOWEL: {GetPhonemeName(getPhoneme(vowel))} and PUNCTUATION: {GetPhonemeName(getPhoneme(position))} " +
                                        $"by 1.5");
                        setLength(position, (a >> 1) + a + 1);
                    }
                }
            }
            
            // Similar to the routine above, but shorten vowels under some circumstances
            // Loop through all phonemes
            int loopIndex2 = -1;
            int? phoneme;

            while ((phoneme = getPhoneme(++loopIndex2)) != null)
            {
                int position = loopIndex2;
                
                // Vowel?
                if (PhonemeHasFlag(phoneme, PhonemeFlags.Vowel))
                {
                    // Get next phoneme
                    phoneme = getPhoneme(++position);

                    // Not a consonant
                    if (!PhonemeHasFlag(phoneme, PhonemeFlags.Consonant))
                    {
                        // "RX" or "LX"?
                        if ((phoneme == 18 || phoneme == 19) &&
                            PhonemeHasFlag(getPhoneme(++position), PhonemeFlags.Consonant))
                        {
                            // Followed by consonant?
                            Debug.WriteLine(
                                $"{loopIndex2} RULE: <VOWEL {GetPhonemeName(getPhoneme(loopIndex2))}> {GetPhonemeName(phoneme)} <CONSONANT: {GetPhonemeName(getPhoneme(position))}> - decrease length of vowel by 1");
                            setLength(loopIndex2, getLength(loopIndex2) - 1);
                        }

                        continue;
                    }


                    // Got here is not <VOWEL>
                    // FIXME: JS - The case when phoneme == END is taken over by !PhonemeHasFlag(phoneme, Consonant)
                    var flags = phonemeFlags[phoneme.Value];

                    // Unvoiced
                    if (!Utils.MatchesBitmask((int)flags, (int)PhonemeFlags.Voiced))
                    {
                        // *, .*, ?*, ,*, -*, DX, S*, SH, F*, TH, /H, /X, CH, P*, T*, K*, KX

                        // Unvoiced plosive
                        if (Utils.MatchesBitmask((int)flags, (int)PhonemeFlags.UnvoicedStopConsonant))
                        {
                            // RULE: <VOWEL> <UNVOICED PLOSIVE>
                            // <VOWEL> <P*, T*, K*, KX>
                            Debug.WriteLine($"{loopIndex2} RULE: <VOWEL> <UNVOICED PLOSIVE> - decrease vowel by 1/8th");

                            int a = getLength(loopIndex2);
                            setLength(loopIndex2, a - (a >> 3));
                        }

                        continue;
                    }
                    
                    // RULE: <VOWEL> <VOWEL or VOICED CONSONANT>
                    // <VOWEL> <IY, IH, EH, AE, AA, AH, AO, UH, AX, IX, ER, UX, OH, RX, LX, WX, YX, WH, R*, L*, W*,
                    //          Y*, M*, N*, NX, Q*, Z*, ZH, V*, DH, J*, EY, AY, OY, AW, OW, UW, B*, D*, G*, GX>
                    Debug.WriteLine(
                        $"{loopIndex2} RULE: <VOWEL> <VOWEL or VOICED CONSONANT> - increase vowel by 1/4 + 1");
                    int a2 = getLength(loopIndex2);
                    setLength(loopIndex2, (a2 >> 2) + a2 + 1); // 5/4 A + 1
                    continue;
                }
                
                //  *, .*, ?*, ,*, -*, WH, R*, L*, W*, Y*, M*, N*, NX, DX, Q*, S*, SH, F*,
                // TH, /H, /X, Z*, ZH, V*, DH, CH, J*, B*, D*, G*, GX, P*, T*, K*, KX
                
                // Nasal?
                if (PhonemeHasFlag(phoneme, PhonemeFlags.Nasal))
                {
                    // RULE: <NASAL> <STOP CONSONANT>
                    //       Set punctuation length to 6
                    //       Set stop consonant length to 5
                    
                    // M*, N*, NX
                    phoneme = getPhoneme(++position);
                    // Is next phoneme a stop consonant?
                    if (phoneme != null && PhonemeHasFlag(phoneme, PhonemeFlags.StopConsonant))
                    {
                        // B*, D*, G*, GX, P*, T*, K*, KX
                        Debug.WriteLine($"{position} RULE: <NASAL> <STOP CONSONANT> - set nasal = 5, consonant = 6");
                        setLength(position, 6);
                        setLength(position - 1, 5);
                    }
                    continue;
                }
                
                //  *, .*, ?*, ,*, -*, WH, R*, L*, W*, Y*, DX, Q*, S*, SH, F*, TH,
                // /H, /X, Z*, ZH, V*, DH, CH, J*, B*, D*, G*, GX, P*, T*, K*, KX

                // Stop consonant?
                if (PhonemeHasFlag(phoneme, PhonemeFlags.StopConsonant))
                {
                    // B*, D*, G*, GX
                    
                    // RULE: <STOP CONSONANT> {optional silence} <STOP CONSONANT>
                    //       Shorten both to (length/2 + 1)
                    
                    while ((phoneme = getPhoneme(++position)) == 0) { } // Move past silence
                    
                    // If another stop consonant, process
                    if (phoneme != null && PhonemeHasFlag(phoneme, PhonemeFlags.StopConsonant))
                    {
                        // RULE: <STOP CONSONANT> {optional silence} <STOP CONSONANT>
                        Debug.WriteLine($"{position} RULE: <STOP CONSONANT> {{optional silence}} <STOP CONSONANT> - shorten both to 1/2 + 1");
                        setLength(position, (getLength(position) >> 1) + 1);
                        setLength(loopIndex2, (getLength(loopIndex2) >> 1) + 1);
                    }
                    continue;
                }
                
                //  *, .*, ?*, ,*, -*, WH, R*, L*, W*, Y*, DX, Q*, S*, SH, F*, TH,
                // /H, /X, Z*, ZH, V*, DH, CH, J*
                
                // Liquid consonant?
                if (position > 0 && PhonemeHasFlag(phoneme, PhonemeFlags.Liquid) &&
                    PhonemeHasFlag(getPhoneme(position - 1), PhonemeFlags.StopConsonant))
                {
                    // R*, L*, W*, Y*
                    // RULE: <STOP CONSONANT> <LIQUID>
                    //       Decrease <LIQUID> by 2
                    // prior phoneme is a stop consonant
                    Debug.WriteLine($"{position} RULE: <STOP CONSONANT> <LIQUID> - decrease by 2");
                    setLength(position, getLength(position) - 2);
                }
            }
        }
    }
}
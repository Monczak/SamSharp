using System;
using System.Diagnostics;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        /**
         * Rewrites the phonemes using the following rules:
         *
         * (DIPHTHONG ENDING WITH WX) -> (DIPHTHONG ENDING WITH WX) WX
         * (DIPHTHONG NOT ENDING WITH WX) -> (DIPHTHONG NOT ENDING WITH WX) YX
         * UL -> AX L
         * UM -> AX M
         * UN -> AX N
         * (STRESSED VOWEL) (SILENCE) (STRESSED VOWEL) -> (STRESSED VOWEL) (SILENCE) Q (VOWEL)
         * T R -> CH R
         * D R -> J R
         * (VOWEL) R -> (VOWEL) RX
         * (VOWEL) L -> (VOWEL) LX
         * G S -> G Z
         * K (VOWEL OR DIPHTHONG NOT ENDING WITH IY) -> KX (VOWEL OR DIPHTHONG NOT ENDING WITH IY)
         * G (VOWEL OR DIPHTHONG NOT ENDING WITH IY) -> GX (VOWEL OR DIPHTHONG NOT ENDING WITH IY)
         * S P -> S B
         * S T -> S D
         * S K -> S G
         * S KX -> S GX
         * (ALVEOLAR) UW -> (ALVEOLAR) UX
         * CH -> CH CH' (CH requires two phonemes to represent it)
         * J -> J J' (J requires two phonemes to represent it)
         * (UNSTRESSED VOWEL) T (PAUSE) -> (UNSTRESSED VOWEL) DX (PAUSE)
         * (UNSTRESSED VOWEL) D (PAUSE) -> (UNSTRESSED VOWEL) DX (PAUSE)
         *
         * <param name="insertPhoneme">The callback to use when inserting phonemes.</param>
         * <param name="setPhoneme">The callback to use when setting phonemes.</param>
         * <param name="getPhoneme">The callback to use when getting phonemes.</param>
         * <param name="getStress">The callback to use when getting stresses.</param>
         */
        private void Parser2(InsertPhonemeDelegate insertPhoneme, SetPhonemeDelegate setPhoneme,
            GetPhonemeDelegate getPhoneme, GetStressDelegate getStress)
        {
            /*
            * Rewrites:
            *  'UW' => 'UX' if alveolar flag set on previous phoneme.
            *  'CH' => 'CH' '**'(43)
            *  'J*' => 'J*' '**'(45)
            */
            void HandleUwChJ(int phoneme, int pos)
            {
                switch (phoneme)
                {
                    // "UW" Examples: NEW, DEW, SUE, ZOO, THOO, TOO
                    case 53:
                    {
                        // Alveolar flag set?
                        if (PhonemeHasFlag(getPhoneme(pos - 1), PhonemeFlags.Alveolar))
                        {
                            Debug.WriteLine($"{pos} RULE: <ALVEOLAR> UW -> <ALVEOLAR> UX");
                            setPhoneme(pos, 16);
                        }
                        break;
                    }
                    // "CH" Example: CHEW
                    case 42:
                    {
                        Debug.WriteLine($"{pos} RULE: CH -> CH CH+1");
                        insertPhoneme(pos + 1, 43, getStress(pos));
                        break;
                    }
                    // "J*" Example: JAY
                    case 44:
                    {
                        Debug.WriteLine($"{pos} RULE: J -> J J+1");
                        insertPhoneme(pos + 1, 45, getStress(pos));
                        break;
                    }
                }
            }

            void ChangeAx(int position, int suffix)
            {
                Debug.WriteLine($"{position} RULE: {GetPhonemeName(getPhoneme(position))} -> AX {GetPhonemeName(suffix)}");
                setPhoneme(position, 13);
                insertPhoneme(position + 1, suffix, getStress(position));
            }

            int pos = -1;
            int? phoneme;

            while ((phoneme = getPhoneme(++pos)) != null)
            {
                // Is phoneme pause?
                if (phoneme == 0)
                    continue;

                if (PhonemeHasFlag(phoneme, PhonemeFlags.Diphthong))
                {
                    // <DIPHTHONG ENDING WITH WX> -> <DIPHTHONG ENDING WITH WX> WX
                    // <DIPHTHONG NOT ENDING WITH WX> -> <DIPHTHONG NOT ENDING WITH WX> YX
                    // Example: OIL, COW
                    Debug.WriteLine(!PhonemeHasFlag(phoneme, PhonemeFlags.DiphthongYx)
                        ? $"{pos} RULE: insert WX following diphthong NOT ending in IY sound"
                        : $"{pos} RULE: insert WX following diphthong ending in IY sound"
                    );
                    // If ends with IY, use YX, else use WX
                    // Insert at WX or YX following, copying the stress
                    // "WX" = 20 "YX" = 21
                    insertPhoneme(pos + 1, PhonemeHasFlag(phoneme, PhonemeFlags.DiphthongYx) ? 21 : 20, getStress(pos));
                    HandleUwChJ(phoneme.Value, pos);
                    continue;
                }

                if (phoneme == 78)
                {
                    // "UL" => "AX" "L*"
                    // Example: MEDDLE
                    ChangeAx(pos, 24);
                    continue;
                }

                if (phoneme == 79)
                {
                    // "UM" => "AX" "M*"
                    // Example: ASTRONOMY
                    ChangeAx(pos, 27);
                    continue;
                }

                if (phoneme == 80)
                {
                    // "UN" => "AX" "N*"
                    ChangeAx(pos, 28);
                    continue;
                }

                if (PhonemeHasFlag(phoneme, PhonemeFlags.Vowel) && getStress(pos) != 0)
                {
                    // Example: FUNCTION
                    // RULE:
                    //       <STRESSED VOWEL> <SILENCE> <STRESSED VOWEL> -> <STRESSED VOWEL> <SILENCE> Q <VOWEL>
                    // EXAMPLE: AWAY EIGHT
                    if (getPhoneme(pos + 1) == 0)   // If following phoneme is a pause, get next
                    {
                        phoneme = getPhoneme(pos + 2);
                        if (phoneme != null && PhonemeHasFlag(phoneme, PhonemeFlags.Vowel) && getStress(pos + 2) != 0)
                        {
                            Debug.WriteLine($"{pos + 2} RULE: Insert glottal stop between two stressed vowels with space between them");
                        }

                        insertPhoneme(pos + 2, 31, 0); // 31 == "Q"
                    }
                    continue;
                }

                var priorPhoneme = (pos == 0) ? null : getPhoneme(pos - 1);
                if (phoneme == pR)
                {
                    // Rules for phonemes before R
                    switch (priorPhoneme)
                    {
                        case pT:
                        {
                            // Example: TRACK
                            Debug.WriteLine($"{pos} RULE: T* R* -> CH R*");
                            setPhoneme(pos - 1, 42);
                            break;
                        }
                        case pD:
                        {
                            // Example: DRY
                            Debug.WriteLine($"{pos} RULE: D* R* -> J* R*");
                            setPhoneme(pos - 1, 44);
                            break;
                        }
                        default:
                        {
                            if (PhonemeHasFlag(priorPhoneme, PhonemeFlags.Vowel))
                            {
                                // Example: ART
                                Debug.WriteLine($"{pos} RULE: <VOWEL> R* -> <VOWEL> RX");
                                setPhoneme(pos, 18);
                            }

                            break;
                        }
                    }
                    continue;
                }

                // "L*"
                if (phoneme == 24 && PhonemeHasFlag(priorPhoneme, PhonemeFlags.Vowel))
                {
                    // Example: ALL
                    Debug.WriteLine($"{pos} RULE: <VOWEL> L* -> <VOWEL> LX");
                    setPhoneme(pos, 19);
                    continue;
                }

                // "G*" "S*"
                if (priorPhoneme == 60 && phoneme == 32)
                {
                    // G <VOWEL OR DIPHTHONG NOT ENDING WITH IY> -> GX <VOWEL OR DIPHTHONG NOT ENDING WITH IY>
                    // Example: GO
                    
                    var phoneme2 = getPhoneme(pos + 1);
                    
                    // If diphthong ending with YX, move continue processing next phoneme
                    if (!PhonemeHasFlag(phoneme2, PhonemeFlags.DiphthongYx) && phoneme2 != null)
                    {
                        // Replace G with GX and continue processing next phoneme
                        Debug.WriteLine($"{pos} RULE: G <VOWEL OR DIPTHONG NOT ENDING WITH IY> -> GX <VOWEL OR DIPHTHONG NOT ENDING WITH IY>");
                        setPhoneme(pos, 63);
                    }

                    continue;
                }
                
                // "K*"
                if (phoneme == 72)
                {
                    // K <VOWEL OR DIPHTHONG NOT ENDING WITH IY> -> KX <VOWEL OR DIPHTHONG NOT ENDING WITH IY>
                    // Example: COW
                    var phoneme2 = getPhoneme(pos + 1);
                    
                    // If at end, replace current phoneme with KX
                    if (!PhonemeHasFlag(phoneme2, PhonemeFlags.DiphthongYx) || phoneme2 == null)
                    {
                        // VOWELS AND DIPHTHONGS ENDING WITH IY SOUND flag set?
                        Debug.WriteLine($"{pos} RULE: K <VOWEL OR DIPTHONG NOT ENDING WITH IY> -> KX <VOWEL OR DIPHTHONG NOT ENDING WITH IY>");
                        setPhoneme(pos, 75);
                        phoneme = 75;
                    }
                }
                
                // Replace with softer version?
                if (PhonemeHasFlag(phoneme, PhonemeFlags.UnvoicedStopConsonant) && priorPhoneme == 32) // "S*"
                {
                    // RULE:
                    //   'S*' 'P*' -> 'S*' 'B*'
                    //   'S*' 'T*' -> 'S*' 'D*'
                    //   'S*' 'K*' -> 'S*' 'G*'
                    //   'S*' 'KX' -> 'S*' 'GX'
                    //   'S*' 'UM' -> 'S*' '**'
                    //   'S*' 'UN' -> 'S*' '**'
                    // Examples: SPY, STY, SKY, SCOWL
                    Debug.WriteLine($"{pos} RULE: S* {GetPhonemeName(phoneme)} -> S* {GetPhonemeName(phoneme - 12)}");
                    setPhoneme(pos, phoneme.Value - 12);
                }
                else if (!PhonemeHasFlag(phoneme, PhonemeFlags.UnvoicedStopConsonant))
                {
                    HandleUwChJ(phoneme.Value, pos);
                }
                
                // "T*", "D*"
                if (phoneme == 69 || phoneme == 57)
                {
                    // RULE: Soften T following vowel
                    // NOTE: This rule fails for cases such as "ODD"
                    //       <UNSTRESSED VOWEL> T <PAUSE> -> <UNSTRESSED VOWEL> DX <PAUSE>
                    //       <UNSTRESSED VOWEL> D <PAUSE>  -> <UNSTRESSED VOWEL> DX <PAUSE>
                    // Example: PARTY, TARDY
                    if (pos > 0 && PhonemeHasFlag(getPhoneme(pos - 1), PhonemeFlags.Vowel))
                    {
                        phoneme = getPhoneme(pos + 1);
                        if (phoneme == 0)
                        {
                            phoneme = getPhoneme(pos + 2);
                        }

                        if (PhonemeHasFlag(phoneme, PhonemeFlags.Vowel) && getStress(pos + 1) == 0)
                        {
                            Debug.WriteLine($"{pos} RULE: Soften T or D following vowel or ER and preceding a pause -> DX");
                            setPhoneme(pos, 30);
                        } 
                    }

                    continue;
                }
                
                Debug.WriteLine($"{pos}: {GetPhonemeName(phoneme)}");
            }
        }
    }
}
using System;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        [Flags]
        private enum PhonemeFlags
        {
            _0x8000 = 0x8000,
            _0x4000 = 0x4000,
            Fricative = 0x2000,
            Liquid = 0x1000,
            Nasal = 0x0800,
            Alveolar = 0x0400,
            _0x0200 = 0x0200,
            Punctuation = 0x0100,
            Vowel = 0x0080,
            Consonant = 0x0040,
            DiphthongYx = 0x0020,
            Diphthong = 0x0010,
            _0x0008 = 0x0008,
            Voiced = 0x0004,
            StopConsonant = 0x0002,
            UnvoicedStopConsonant = 0x0001
        }

        private const int pR = 23;
        private const int pD = 57;
        private const int pT = 69;
    }
}
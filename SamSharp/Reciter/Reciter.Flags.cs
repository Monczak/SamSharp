using System;

namespace SamSharp.Reciter
{
    public partial class Reciter
    {
        [Flags]
        private enum CharFlags
        {
            Numeric = 0x01,
            Ruleset2 = 0x02,
            Voiced = 0x04,
            _0x08 = 0x08,
            Diphthong = 0x10,
            Consonant = 0x20,
            VowelOrY = 0x40,
            AlphaOrQuot = 0x80,
        }
    }
}
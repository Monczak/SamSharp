using System;

namespace SamSharp
{
    internal class Utils
    {
        internal static bool MatchesBitmask(int bits, int mask) => (bits & mask) != 0;
    }
}
using System;
using System.Diagnostics;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        /// <summary>
        /// Match both characters but not with wildcards.
        /// </summary>
        /// <param name="char1">The first character.</param>
        /// <param name="char2">The second character.</param>
        /// <returns>Whether the characters form a valid phoneme but not with wildcards.</returns>
        private int? FullMatch(char char1, char char2)
        {
            int index = Array.FindIndex(phonemeNameTable, value => value == $"{char1}{char2}" && value[1] != '*');
            return index != -1 ? index : (int?)null;
        }

        private int? WildMatch(char char1)
        {
            int index = Array.FindIndex(phonemeNameTable, value => value == char1 + "*");
            return index != -1 ? index : (int?)null;
        }

        /**
         * The input[] buffer contains a string of phonemes and stress markers along
         * the lines of:
         *
         *     DHAX KAET IHZ AH5GLIY.
         *
         * Some phonemes are 2 bytes long, such as "DH" and "AX".
         * Others are 1 byte long, such as "T" and "Z".
         * There are also stress markers, such as "5" and ".".
         *
         * The characters of the phonemes are stored in the table PhonemeNameTable.
         * The stress characters are arranged in low to high stress order in StressTable[].
         *
         * The following process is used to parse the input buffer:
         *
         * Repeat until the end is reached:
         * 1. First, a search is made for a 2 character match for phonemes that do not
         *    end with the '*' (wildcard) character. On a match, the index of the phoneme
         *    is added to the result and the buffer position is advanced 2 bytes.
         *
         * 2. If this fails, a search is made for a 1 character match against all
         *    phoneme names ending with a '*' (wildcard). If this succeeds, the
         *    phoneme is added to result and the buffer position is advanced
         *    1 byte.
         *
         * 3. If this fails, search for a 1 character match in the stressInputTable[].
         *   If this succeeds, the stress value is placed in the last stress[] table
         *   at the same index of the last added phoneme, and the buffer position is
         *   advanced by 1 byte.
         *
         * If this fails, return false.
         *
         * On success:
         *
         *    1. phonemeIndex[] will contain the index of all the phonemes.
         *    2. The last index in phonemeIndex[] will be 255.
         *    3. stress[] will contain the stress value for each phoneme
         *
         * input holds the string of phonemes, each two bytes wide
         * signInputTable1[] holds the first character of each phoneme
         * signInputTable2[] holds the second character of each phoneme
         * phonemeIndex[] holds the indexes of the phonemes after parsing input[]
         *
         * The parser scans through the input[], finding the names of the phonemes
         * by searching signInputTable1[] and signInputTable2[]. On a match, it
         * copies the index of the phoneme into the phonemeIndexTable[].
         *
         * <param name="input">Holds the string of phonemes, each two bytes wide.</param>
         * <param name="addPhoneme">The callback to use to store phoneme index values.</param>
         * <param name="addStress">The callback to use to store stress index values.</param>
         */
        private void Parser1(string input, Action<int> addPhoneme, Action<int> addStress)
        {
            for (int srcPos = 0; srcPos < input.Length; srcPos++)
            {
                string tmp = input.ToLower();
                Debug.WriteLine(
                    $"Processing \"{tmp.JsSubstring(0, srcPos)}{tmp.JsSubstring(srcPos, 2).ToUpper()}{tmp.JsSubstring(srcPos + 2)}\"");

                char char1 = input[srcPos];
                char char2 = srcPos + 1 >= input.Length ? ' ' : input[srcPos + 1];

                int? match;
                if ((match = FullMatch(char1, char2)) != null)
                {
                    // Matched both characters (no wildcards)
                    srcPos++; // Skip the second character of the input as we've matched it
                    addPhoneme(match.Value);
                    continue;
                }

                if ((match = WildMatch(char1)) != null)
                {
                    // Matched just the first character (with second character matching '*')
                    addPhoneme(match.Value);
                    continue;
                }

                // Should be a stress character. Search through the stress table backwards
                int i = stressTable.Length - 1;
                while (char1 != stressTable[i] && i > 0) i--;
                if (i == 0)
                    throw new Exception($"Could not parse char {char1}");
                addStress(i);
            }
        }
    }
}
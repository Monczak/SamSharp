using System;
using System.Diagnostics;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        private int?[] stresses;
        private int?[] phonemeLengths;
        private int?[] phonemeIndexes;

        private int? GetPhoneme(int pos)
        {
            if (pos < 0 || pos > phonemeIndexes.Length)
                throw new Exception($"Out of bounds: {pos}");
            return pos == phonemeIndexes.Length ? (int?)null : phonemeIndexes[pos];
        }

        private void SetPhoneme(int pos, int value)
        {
            // TODO: Is JS stupid here? (index is undefined)
            Debug.WriteLine($"{pos} CHANGE: {phonemeNameTable[phonemeIndexes[pos]]} -> {phonemeNameTable[value]}");
            phonemeIndexes[pos] = value;
        }

        private void InsertPhoneme(int pos, int value, int stressValue, int length = 0)
        {
            Debug.WriteLine($"{pos} INSERT: {phonemeNameTable[value]}");
            for (int i = phonemeIndexes.Length - 1; i >= pos; i--)
            {
                phonemeIndexes[i + 1] = phonemeIndexes[i];
                phonemeLengths[i + 1] = GetLength(i);
                stresses[i + 1] = GetStress(i);
            }

            phonemeIndexes[pos] = value;
            phonemeLengths[pos] = length;
            stresses[pos] = stressValue;
        }

        private int GetLength(int pos) => phonemeLengths[pos] ?? 0;

        /// <summary>
        /// Parses speech data.
        ///
        /// Returns array of (phoneme, length, stress).
        /// </summary>
        /// <param name="input">The data to parse.</param>
        /// <returns>The parsed data.</returns>
        public (string phoneme, int length, int stress)[]? Parse(string? input)
        {
            if (input is null)
                return null;

            return null;
        }
    }
}
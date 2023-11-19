using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SamSharp.Parser
{
    public partial class Parser
    {
        private Dictionary<int, int?> stresses;        // Numbers from 0 to 8
        private Dictionary<int, int?> phonemeLengths;
        private Dictionary<int, int?> phonemeIndexes;
        
        
        private string? GetPhonemeNamePos(int? pos) => pos is null || phonemeIndexes[pos.Value] is null ? null : phonemeNameTable[phonemeIndexes[pos.Value]!.Value];
        private string? GetPhonemeName(int? phoneme) => phoneme is null ? null : phonemeNameTable[phoneme.Value];
        
        private int? GetPhoneme(int pos)
        {
            if (pos == phonemeIndexes.Count)
                return null;
            
            if (pos < 0 || pos > phonemeIndexes.Count)
                throw new Exception($"Out of bounds: {pos}");
            
            return pos == phonemeIndexes.Count ? null : phonemeIndexes[pos];
        }

        private void SetPhoneme(int pos, int value)
        {
            Debug.WriteLine($"{pos} CHANGE: {GetPhonemeNamePos(pos)} -> {phonemeNameTable[value]}");
            phonemeIndexes[pos] = value;
        }

        private void InsertPhoneme(int pos, int value, int stressValue, int length = 0)
        {
            Debug.WriteLine($"{pos} INSERT: {phonemeNameTable[value]}");
            for (int i = phonemeIndexes.Count - 1; i >= pos; i--)
            {
                phonemeIndexes[i + 1] = phonemeIndexes[i];
                phonemeLengths[i + 1] = GetLength(i);
                stresses[i + 1] = GetStress(i);
            }

            phonemeIndexes[pos] = value;
            phonemeLengths[pos] = length;
            stresses[pos] = stressValue;
        }

        private int GetStress(int pos) => stresses[pos] ?? 0;

        private void SetStress(int pos, int stressValue)
        {
            Debug.WriteLine($"{pos} \"{GetPhonemeNamePos(pos)}\" SET STRESS: {stresses[pos]} -> {stressValue}");
            stresses[pos] = stressValue;
        }

        private int GetLength(int pos) => phonemeLengths[pos] ?? 0;

        private void SetLength(int pos, int length)
        {
            Debug.WriteLine($"{pos} \"{GetPhonemeNamePos(pos)}\" SET LENGTH: {phonemeLengths[pos]} -> {length}");

            if ((length & 0x80) != 0)
            {
                throw new Exception("Got the flag 0x80, see CopyStress() and SetPhonemeLength() comments!");
            }

            if (pos < 0 || pos > phonemeIndexes.Count)
            {
                throw new Exception($"Out of bounds: {pos}");
            }

            phonemeLengths[pos] = length;
        }
        
        private delegate void InsertPhonemeDelegate(int pos, int value, int stressValue, int length = 0);
        private delegate void SetPhonemeDelegate(int pos, int value);
        private delegate int? GetPhonemeDelegate(int pos);
        private delegate int GetStressDelegate(int pos);
        private delegate void SetStressDelegate(int pos, int stressValue);
        private delegate int GetLengthDelegate(int pos);
        private delegate void SetLengthDelegate(int pos, int length);

        private bool PhonemeHasFlag(int? phoneme, PhonemeFlags flag) =>
            phoneme is { } && Utils.MatchesBitmask((int)phonemeFlags[phoneme.Value], (int)flag);

        public struct PhonemeData
        {
            public int? Phoneme { get; }
            public int? Length { get; }
            public int? Stress { get; }

            public PhonemeData(int? phoneme, int? length, int? stress)
            {
                Phoneme = phoneme;
                Length = length;
                Stress = stress;
            }
        }

        /// <summary>
        /// Parses speech data.
        ///
        /// Returns array of (phoneme, length, stress).
        /// </summary>
        /// <param name="input">The data to parse.</param>
        /// <returns>The parsed data.</returns>
        public PhonemeData[] Parse(string? input)
        {
            if (input is null)
                return null;
            
            stresses = new Dictionary<int, int?>();
            phonemeLengths = new Dictionary<int, int?>();
            phonemeIndexes = new Dictionary<int, int?>();

            int pos = 0;
            Parser1(input, 
                value =>
                {
                    stresses[pos] = 0;
                    phonemeLengths[pos] = 0;
                    phonemeIndexes[pos++] = value;
                },
                value =>
                {
                    if ((value & 128) != 0)
                        throw new Exception("Got the flag 0x80, see CopyStress() and SetPhonemeLengths() comments!");
                    stresses[pos - 1] = value; // Set stress for prior phoneme
                });
            
            // TODO: Refactor all this not to use callbacks (unnecessary in this way)
            Parser2(InsertPhoneme, SetPhoneme, GetPhoneme, GetStress);
            CopyStress(GetPhoneme, GetStress, SetStress);
            SetPhonemeLength(GetPhoneme, GetStress, SetLength);
            AdjustLengths(GetPhoneme, SetLength, GetLength);
            ProlongPlosiveStopConsonantsCode41240(GetPhoneme, InsertPhoneme, GetStress);
            
            PrintPhonemes();

            List<PhonemeData> result = new List<PhonemeData>();

            for (int i = 0; i < phonemeIndexes.Count; i++)
            {
                if (phonemeIndexes[i] != null && phonemeIndexes[i] != 0)
                    result.Add(new PhonemeData(phonemeIndexes[i], phonemeLengths[i], stresses[i]));
            }

            return result.ToArray();
        }

        private void PrintPhonemes()
        {
            Debug.WriteLine("==================================");
            Debug.WriteLine("Internal Phoneme Presentation:");
            Debug.WriteLine(" pos  idx  phoneme  length  stress");
            Debug.WriteLine("----------------------------------");

            for (int i = 0; i < phonemeIndexes.Count; i++)
            {
                string Name() => (phonemeIndexes[i] < 81 ? GetPhonemeNamePos(i) : "??")!;
                
                Debug.WriteLine($" {i.ToString().PadLeft(3, '0')}" +
                                $"  {phonemeIndexes[i].ToString().PadLeft(3, '0')}" +
                                $"  {Name()}" +
                                $"       {phonemeLengths[i].ToString().PadLeft(3, '0')}" +
                                $"     {stresses[i].ToString().PadLeft(3, '0')}");
            }
            
            Debug.WriteLine("==================================");
        }
    }
}
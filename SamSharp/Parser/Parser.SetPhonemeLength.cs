namespace SamSharp.Parser
{
    public partial class Parser
    {
        /// <summary>
        /// Changes phoneme length dependent on stress. 
        /// </summary>
        /// <param name="getPhoneme">Callback for retrieving phonemes.</param>
        /// <param name="getStress">Callback for retrieving phoneme stress.</param>
        /// <param name="setLength">Callback for setting phoneme length.</param>
        private void SetPhonemeLength(GetPhonemeDelegate getPhoneme, GetStressDelegate getStress,
            SetLengthDelegate setLength)
        {
            int position = 0;
            int? phoneme;

            while ((phoneme = getPhoneme(position)) != null)
            {
                var stress = getStress(position);
                if (stress == 0 || stress > 0x7F)
                {
                    setLength(position, combinedPhonemeLengthTable[phoneme.Value] & 0xFF);
                }
                else
                {
                    setLength(position, combinedPhonemeLengthTable[phoneme.Value] >> 8);
                }

                position++;
            }
        }
    }
}
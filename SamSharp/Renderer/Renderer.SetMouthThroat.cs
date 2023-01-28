using System.Runtime.CompilerServices;

namespace SamSharp.Renderer
{
    public partial class Renderer
    {
        private Formants SetMouthThroat(byte mouth, byte throat)
        {
            int Transpose(int factor, int initialFreq) => (((factor * initialFreq) >> 8) & 0xFF) << 1;

            Formants formants = new Formants();
            for (int i = 0; i < frequencyData.Length; i++)
            {
                formants.Mouth[i] = frequencyData[i] & 0xFF;
                formants.Throat[i] = (frequencyData[i] >> 8) & 0xFF;
                formants.Formant3[i] = (frequencyData[i] >> 16) & 0xFF;
            }
            
            // Recalculate formant frequencies 5..29 for the mouth (F1) and throat (F2)
            for (int pos = 5; pos <= 29; pos++)
            {
                // Recalculate mouth frequency
                formants.Mouth[pos] = Transpose(mouth, formants.Mouth[pos]);
                
                // Recalculate throat frequency
                formants.Throat[pos] = Transpose(throat, formants.Throat[pos]);
            }
            
            // Recalculate formant frequencies 48..53
            for (int pos = 48; pos <= 53; pos++)
            {
                // Recalculate mouth frequency
                formants.Mouth[pos] = Transpose(mouth, formants.Mouth[pos]);
                
                // Recalculate throat frequency
                formants.Throat[pos] = Transpose(throat, formants.Throat[pos]);
            }

            return formants;
        }
    }
}
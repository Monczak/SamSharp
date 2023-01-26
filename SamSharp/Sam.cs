using System;
using System.Diagnostics;

namespace SamSharp
{
    public class Sam
    {
        public void Test()
        {
            Reciter.Reciter reciter = new Reciter.Reciter();
            string result = reciter.TextToPhonemes("computer");
            Debug.WriteLine(result);
        }
    }
}
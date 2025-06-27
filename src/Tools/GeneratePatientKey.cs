using System;

namespace PvkBroker.Tools
{
   public class PatientKey
    {
        public static string GeneratePatientKey() { 
            // random hex7

            var random = new Random();
            int num = random.Next();
            string hexString = num.ToString("X7").Substring(0, 7);

            return hexString;
        }
    }
}

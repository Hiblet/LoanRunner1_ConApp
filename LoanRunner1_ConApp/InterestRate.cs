using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace NZ01
{
    public class InterestRate
    {

        // MEMBERS
        private static CustomLog logger = new CustomLog("InterestRate", true);

        private static SortedList<DateTime, decimal> _rates 
            = new SortedList<DateTime, decimal>(new ByDateTimeAscending());


        // STATIC CTOR
        static InterestRate()
        {
            loadRates();
        }





        ///////////////////// 
        // MEMBER FUNCTIONS

        private static void loadRates()
        {
            var prefix = "loadRates() - ";

            int countRates = 0;
            int countLines = 0;
            string filename = "./Data/InterestRates.CSV";

            try
            {
                var lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    // Expect format like "YYYY-MM-DD,Rate" 
                    // where Rate is a percentage rate decimal value like "5.3"
                    // eg "2017-03-14,4.8"

                    ++countLines;

                    // Split the line on a comma 
                    var components = line.Split(',');
                    if (components.Count() == 2)
                    {
                        string sDate = components[0];
                        string sRate = components[1];

                        DateTime dt;
                        decimal rate = 0;

                        if (DateTime.TryParse(sDate, out dt) && 
                            Decimal.TryParse(sRate, out rate))
                        {
                            _rates[dt] = rate;
                            ++countRates;
                        }                        
                    }
                    else
                    {
                        logger.Debug(prefix + "Line " + countLines + " does not have two items.");
                    }
                }

                logger.Debug(prefix + countLines + " Interest Rate Lines Iterated.");
                logger.Debug(prefix + countRates + " Interest Rates Loaded.");
            }
            catch (Exception ex)
            {
                logger.Error(prefix + "Failed to read [" + filename + "] file; Error Message: " + ex.GetBaseException().Message);
            }
        }


        public static decimal GetRate(DateTime dt, bool bAsPercentage = false)
        {
            decimal rate = 5; // Default to 5

            foreach (KeyValuePair<DateTime, decimal> kvpA in _rates)
            {
                DateTime dtCurrent = kvpA.Key;
                Decimal rateCurrent = kvpA.Value;

                // If we have exceeded the sample date,
                // exit, leaving rate as the last rate seen.                
                if (dtCurrent > dt)
                    break;
                else
                    rate = rateCurrent;
            }

            if (bAsPercentage)
                return rate;
            else
                return rate / 100m;
        }

    } // end of "public class InterestRate"

} // end of "namespace NZ01"

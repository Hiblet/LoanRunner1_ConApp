using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace NZ01
{
    public class MarketStrike
    {


        ////////////
        // MEMBERS

        private static CustomLog logger = new CustomLog("MarketStrike", true);

        private static SortedList<DateTime, int> _strikes
            = new SortedList<DateTime, int>(new ByDateTimeAscending());        

        // STATIC CTOR
        static MarketStrike()
        {
            loadStrikes();
        }




        /////////////////////
        // MEMBER FUNCTIONS

        private static void loadStrikes()
        {
            var prefix = "loadStrikes() - ";

            int countStrikes = 0;
            int countLines = 0;
            string filename = "./Data/MarketStrikes.CSV";

            try
            {
                var lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    // Expect format like "YYYY-MM-DD", a market strike date
                    // eg "2017-01-01"

                    ++countLines;

                    // Split the line on a comma 
                    var components = line.Split(',');
                    string sDate = components[0];

                    DateTime dt;

                    if (DateTime.TryParse(sDate, out dt))
                    {
                        _strikes[dt] = 0;
                        ++countStrikes;
                    }
                }

                logger.Debug(prefix + countLines + " Market Strike Lines Iterated.");
                logger.Debug(prefix + countStrikes + " Market Strikes Loaded.");
            }
            catch (Exception ex)
            {
                logger.Error(prefix + "Failed to read [" + filename + "] file; Error Message: " + ex.GetBaseException().Message);
            }
        }


        public static bool IsValidStrikeDate(DateTime strike)
        {
            KeyValuePair<DateTime, int> kvp = new KeyValuePair<DateTime, int>(strike, 0);
            if (_strikes.Contains(kvp))
                return true;
            else
                return false;
        }

        public static DateTime GetNextStrike(DateTime today)
        {
            var prefix = "GetNextStrike() - ";

            foreach (KeyValuePair<DateTime, int> kvpA in _strikes)
            {
                DateTime strike = kvpA.Key;

                if (strike > today)
                    return strike;
            }

            logger.Error(prefix + "Not enough MarketStrikes are available; Extend the MarketStrikes.CSV data file.");

            return DateTime.MinValue;
        }

        // Get the strike n days hence based on todays date
        public static DateTime GetStrikeNDaysHence(DateTime today, int nDaysHence)
        {
            DateTime dtNDaysHence = today.AddDays((double)nDaysHence);
            return GetNextStrike(dtNDaysHence);
        }

        // For a given strike, get the market price today
        public static decimal GetMarketPrice(DateTime today, DateTime strike)
        {
            //var prefix = " GetMarketPrice() - ";

            double diffDays = Math.Abs((strike.Date - today.Date).TotalDays);

            decimal interestRateYearly = InterestRate.GetRate(today);
            decimal interestRateDaily = interestRateYearly / 365;

            decimal price = 1m - (interestRateDaily * (decimal)diffDays);

            return price;
        }


    } // end of "public class MarketStrike"

} // end of "namespace NZ01"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace NZ01
{

    public class Balance
    {
        ////////////
        // MEMBERS

        private static CustomLog logger = new CustomLog("Balance", true);

        private static SortedList<DateTime, decimal> _changes
            = new SortedList<DateTime, decimal>(new ByDateTimeAscending());


        // STATIC CTOR
        static Balance()
        {
            loadChanges();
        }


        

        /////////////////////
        // MEMBER FUNCTIONS
        //

        private static void loadChanges()
        {
            var prefix = "loadChanges() - ";

            int countChanges = 0;
            int countLines = 0;
            string filename = "./Data/BalanceChanges.CSV";

            try
            {
                var lines = File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    // Expect format like "YYYY-MM-DD,Change" 
                    // where Change is a monetary decimal value like "200.50"
                    // eg "2017-03-14,200.50"

                    ++countLines;

                    // Split the line on a comma 
                    var components = line.Split(',');
                    if (components.Count() == 2)
                    {
                        string sDate = components[0];
                        string sChange = components[1];

                        DateTime dt;
                        decimal change = 0;

                        if (DateTime.TryParse(sDate, out dt) &&
                            Decimal.TryParse(sChange, out change))
                        {
                            _changes[dt] = change;
                            ++countChanges;
                        }
                    }
                    else
                    {
                        logger.Debug(prefix + "Line " + countLines + " does not have two items.");
                    }
                }

                logger.Debug(prefix + countLines + " Balance Change Lines Iterated.");
                logger.Debug(prefix + countChanges + " Balance Changes Loaded.");
            }
            catch (Exception ex)
            {
                logger.Error(prefix + "Failed to read [" + filename + "] file; Error Message: " + ex.GetBaseException().Message);
            }
        }

        public static SortedList<DateTime, decimal> GetChanges()
        {
            return new SortedList<DateTime, decimal>(_changes);
        }



    } // end of "public class Balance"

} // end of "namespace NZ01"

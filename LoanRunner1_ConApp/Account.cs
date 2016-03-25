using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZ01
{
    public class Account
    {

        ////////////
        // MEMBERS

        private static CustomLog logger = new CustomLog("Account", true);

        

        private Int64 _position = 0;
        private DateTime _strike = DateTime.MinValue;
        private decimal _px = 0.0m;
        private decimal _balance = 0.0m;
        private string _name = "";
        private bool _active = true;

        private int _decPlaces = 2;

        private decimal _cumulativePositive = 0m;
        private decimal _cumulativeNegative = 0m;

        

        public Account(string name)
        {
            _name = name;
        }

        public Account(string name, Int64 position, decimal balance)
        {
            _name = name;
            _position = position;
            _balance = balance;
        }


        // PROPERTIES

        public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public string Name
        {
            get { return _name; }
        }

        public Int64 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public decimal Balance
        {
            get { return _balance; }
        }



        public DateTime Strike
        {
            get { return _strike; }
            set { _strike = value; }
        }

        /////////////////////
        // MEMBER FUNCTIONS
        //

        public void ChangeBalance(decimal delta)
        {
            double multiplier = Math.Pow(10, _decPlaces);
            decimal deltaRounded = (Math.Floor(delta * (decimal)multiplier)) / (decimal)multiplier;

            _balance = _balance + deltaRounded;

            if (deltaRounded > 0)
                _cumulativePositive += deltaRounded;

            if (deltaRounded < 0)
                _cumulativeNegative += deltaRounded;
        }

        public bool HasPosition()
        {
            if (_strike == DateTime.MinValue)
                return false;
            else
                return true;
        }

        public void Reset()
        {
            _balance = 0.0m;
            _cumulativeNegative = 0.0m;
            _cumulativePositive = 0.0m;
            this.Position = 0;
            this.Strike = DateTime.MinValue;
            this.Active = true;
        }


        
        /// <summary>
        /// Convert the current balance to a position in the given strike
        /// </summary>
        /// <param name="today">DateTime; The day that the position is taken</param>
        /// <param name="strike">DateTime; The date to start from when looking for the next strike</param>
        public void TakePosition(DateTime today, DateTime strike, decimal repayment = 0.0m)
        {
            var prefix = "TakePosition() - ";

            if (!MarketStrike.IsValidStrikeDate(strike))
            {
                logger.Error(prefix + "Date for the strike [" + strike + "] is not a valid strike date.");
                return;
            }

            if (today >= strike)
            {
                logger.Error(prefix + "Date for this day [" + today + "] is wrong relative to the Strike [" + strike + "]");
                return;
            }

            string sStateBefore = this.DumpToString();

            // Get the price for the target strike
            decimal price = MarketStrike.GetMarketPrice(today, strike);

            logger.Debug(prefix + "Date: [" + today.ToString(Constants.DATEONLYFORMAT) + "], Market Price for [" + strike.ToString(Constants.DATEONLYFORMAT) + "] strike is [" + price + "]");

            // Round price to dec places to get balance limit
            double multiplier = Math.Pow(10, _decPlaces);
            decimal balanceLimit = 0m;
            if (this.Balance > price)
                balanceLimit = (Math.Ceiling(price * (decimal)multiplier)) / (decimal)multiplier;
            else
                balanceLimit = (Math.Floor(price * (decimal)multiplier)) / (decimal)multiplier;

            // Convert balance to position, leaving zero or positive minimal balance
            decimal available = this.Balance - balanceLimit;
            decimal contracts = (available / price);
            contracts = Math.Ceiling(contracts);

            // If the number of contracts is positive, and the position is negative,
            // we are buying contracts, and we are paying off a loan.
            // If the number of contracts we can buy with this balance is greater
            // than or equal to the current negative position, then we are closing the
            // loan.  If we are closing the loan, only buy enough contracts to close
            // zero the position.
            if (contracts > 0 &&
                _position < 0 && 
                contracts > Math.Abs(_position))
            {
                // Reduce the number of contracts to buy to the position, so that
                // we zero the position, and close the account
                contracts = (-1) * _position; // Buy exactly the number to close
                this.Active = false; // close
            }

            // Cost or amount returned, rounded 
            decimal cash = contracts * price;
            cash = (Math.Ceiling(cash * (decimal)multiplier)) / (decimal)multiplier;

            // Update members
            _balance = _balance - cash;

            _position = _position + (int)contracts;

            _px = price;

            if (_position != 0)
                _strike = strike;
            else
                _strike = DateTime.MinValue;

            string sStateAfter = this.DumpToString();

            logger.Debug(prefix + "Current State: [" + sStateAfter + "], Previous State: [" + sStateBefore + "]");



            ////////////////////////////
            // Generate data for Excel

            string sExcel = "#EXCEL,";
            sExcel += today.ToString(Constants.DATEONLYFORMAT) + ",";
            sExcel += _position.ToString() + " in " + _strike.ToString(Constants.DATEONLYFORMAT) + ",";            
            sExcel += _px.ToString("F8") + ",";
            sExcel += (repayment == 0 ? "ROLL" : repayment.ToString("F2")) + ","; // Whether a roll or a repayment
            sExcel += (_position * _px).ToString("F2") + ","; // Notional cash value
            sExcel += _balance.ToString("F2") + ","; // Residual

            
            logger.Debug(prefix + sExcel);
        }

        /* V1 - Prior to exit routine for loan being paid off
        /// <summary>
        /// Convert the current balance to a position in the given strike
        /// </summary>
        /// <param name="today">DateTime; The day that the position is taken</param>
        /// <param name="strike">DateTime; The date to start from when looking for the next strike</param>
        public void TakePosition(DateTime today, DateTime strike)
        {
            var prefix = "TakePosition() - ";

            if (!MarketStrike.IsValidStrikeDate(strike))
            {
                logger.Error(prefix + "Date for the strike [" + strike + "] is not a valid strike date.");
                return;
            }

            if (today >= strike)
            {
                logger.Error(prefix + "Date for this day [" + today + "] is wrong relative to the Strike [" + strike + "]");
                return;
            }

            string sStateBefore = "Balance=" + this.Balance + ", Position=" + _position + ", Strike=" + _strike.ToString(Constants.DATEONLYFORMAT);

            // Get the price for the target strike
            decimal price = MarketStrike.GetMarketPrice(today, strike);

            logger.Debug(prefix + "Date: [" + today.ToString(Constants.DATEONLYFORMAT) + "], Market Price for [" + strike.ToString(Constants.DATEONLYFORMAT) + "] strike is [" + price + "]");

            // Round price to dec places to get balance limit
            double multiplier = Math.Pow(10, _decPlaces);
            //decimal balanceLimit = (Math.Ceiling(price * (decimal)multiplier)) / (decimal)multiplier;
            decimal balanceLimit = 0m;
            if (this.Balance > price)
                balanceLimit = (Math.Ceiling(price * (decimal)multiplier)) / (decimal)multiplier;
            else
                balanceLimit = (Math.Floor(price * (decimal)multiplier)) / (decimal)multiplier;

            // Convert balance to position, leaving zero or positive minimal balance
            decimal available = this.Balance - balanceLimit;
            decimal contracts = (available / price);
            contracts = Math.Ceiling(contracts);

            //int sign = (contracts >= 0) ? 1 : -1;

            // Cost or amount returned, rounded 
            //decimal cost = (contracts * price);
            decimal cash = contracts * price;
            cash = (Math.Ceiling(cash * (decimal)multiplier)) / (decimal)multiplier;

            this.Balance = this.Balance - cash;
            _position = _position + (int)contracts;

            if (_position != 0)
                _strike = strike;
            else
                _strike = DateTime.MinValue;

            string sStateAfter = "Balance=" + this.Balance + ", Position=" + _position + ", Strike=" + _strike.ToString(Constants.DATEONLYFORMAT);

            logger.Debug(prefix + "Current State: [" + sStateAfter + "], Previous State: [" + sStateBefore + "]");
        }                
        */


        // Convert the current position to a balance
        public void LiquidatePosition(DateTime today)
        {
            var prefix = "LiquidatePosition() - ";

            if (_position == 0)
            {
                logger.Debug(prefix + "Position is zero; No change.");
                return;
            }

            if (!this.Active)
            {
                logger.Debug(prefix + "Account is not active.");
                return;
            }

            if (today >= _strike)
            {
                logger.Error(prefix + "Date for this day [" + today + "] is wrong relative to the Strike [" + _strike + "]");
                return;
            }

            string sStateBefore = this.DumpToString();

            // Get the price for this strike
            decimal price = MarketStrike.GetMarketPrice(today, _strike);

            logger.Debug(prefix + "Date: [" + today.ToString(Constants.DATEONLYFORMAT) + "], Market Price for [" + _strike + "] strike is [" + price + "]");

            double multiplier = Math.Pow(10, _decPlaces);

            decimal balanceChange = _position * price;

            balanceChange = (Math.Ceiling(balanceChange * (decimal)multiplier)) / (decimal)multiplier;

            _balance = _balance + balanceChange;

            _position = 0;

            _strike = DateTime.MinValue;

            string sStateAfter = this.DumpToString();

            logger.Debug(prefix + "Current State: [" + sStateAfter + "], Previous State: [" + sStateBefore + "]");
        }

        // Roll the position forwards until the current strike is 
        // at least nDaysHence ahead of the target date.
        public bool RollOn(DateTime dtTarget, int nDaysHence)
        {
            var prefix = "RollOn() - ";

            if (!this.Active)
            {
                logger.Debug(prefix + "Account is not active; Not rolling closed account.");
                return false;
            }

            // Work out the date that a strike must be greater 
            // than to respect the nDaysHence limit
            DateTime dtMinimumStrike = dtTarget.AddDays((double)nDaysHence);

            // Work out the date that we should roll on for the 
            // current position, which is nDaysHence before the strike
            DateTime dtRollDate = _strike.AddDays((double)(-1 * nDaysHence));

            bool bKeepGoing = true;
            
            while (bKeepGoing)
            {
                // Roll (exit on failure to roll)
                if (!Roll(dtRollDate, nDaysHence))
                    return false;
                
                if (_strike > dtMinimumStrike)
                    bKeepGoing = false;
                else
                    dtRollDate = _strike.AddDays((double)(-1 * nDaysHence));
            }

            return true;
        }

        // Liquidise the current position and try to take position and 
        // return to minimally positive balance.
        public bool Roll(DateTime dtRollDate, int nDaysHence)
        {
            var prefix = "Roll() - ";

            if (this.Position == 0)
            {
                logger.Debug(prefix + "Position is zero; Not rolling zero position.");
                return false;
            }

            if (!this.Active)
            {
                logger.Debug(prefix + "Account is not active; Not rolling closed account.");
                return false;
            }

            LiquidatePosition(dtRollDate);

            // Find the strike that is at least nDaysHence
            DateTime dtMinimumStrike = dtRollDate.AddDays((double)nDaysHence);

            DateTime dtNextStrike = MarketStrike.GetNextStrike(dtMinimumStrike);

            if (dtNextStrike != DateTime.MinValue)
            {
                TakePosition(dtRollDate, dtNextStrike);
                return true;
            }

            return false;
        }

        public void Dump(string sText)
        {
            var prefix = (string.IsNullOrWhiteSpace(sText)) ? "Dump() - " : "Dump() [" + sText + "] - ";
            logger.Debug(prefix + DumpToString());
        }

        public string DumpToString()
        {
            string sReturn = "";
            sReturn += "Balance=" + this.Balance.ToString("F2") + ", ";
            sReturn += "Position=" + _position + ", ";
            sReturn += "Strike=" + (_strike == DateTime.MinValue ? "NULL" : _strike.ToString(Constants.DATEONLYFORMAT)) + ", ";
            sReturn += "Active=" + _active.ToString() + ", ";
            sReturn += "CumulativeNeg=" + _cumulativeNegative.ToString("F2") + ", ";
            sReturn += "CumulativePos=" + _cumulativePositive.ToString("F2") + ", ";
            sReturn += "Name=" + _name;
            return sReturn;
        }

    } // end of "public class Account"

} // end of "namespace NZ01"




///////////////////////////////////////////////////////////////////////////////
// GARBAGE

/*
public void TakePosition(DateTime today, DateTime strike)
{
var prefix = "TakePosition() - ";

if (!MarketStrike.IsValidStrikeDate(strike))
{
    logger.Error(prefix + "Date for the strike [" + strike + "] is not a valid strike date.");
    return;
}

if (today >= strike)
{
    logger.Error(prefix + "Date for this day [" + today + "] is wrong relative to the Strike [" + strike + "]");
    return;
}

string sStateBefore = "Balance=" + this.Balance + ", Position=" + _position + ", Strike=" + _strike.ToString(Constants.DATEONLYFORMAT);

// Get the price for the target strike
decimal price = MarketStrike.GetMarketPrice(today, strike);

logger.Debug(prefix + "Date: [" + today.ToString(Constants.DATEONLYFORMAT) + "], Market Price for [" + strike.ToString(Constants.DATEONLYFORMAT) + "] strike is [" + price + "]");

// Round price to dec places to get balance limit
double multiplier = Math.Pow(10, _decPlaces);
decimal balanceLimit = (Math.Floor(price * (decimal)multiplier)) / (decimal)multiplier;

if (this.Balance > balanceLimit)
{
    // Lender; This person can afford to buy at least one contract
    // Buy contracts with balance
    decimal available = this.Balance - balanceLimit;
    decimal contracts = (available / price);
    contracts = Math.Ceiling(contracts);

    // Work out the cost, round up
    decimal cost = contracts * price;
    cost = ((Math.Ceiling(cost * (decimal)multiplier)) / (decimal)multiplier);

    this.Balance = this.Balance - cost;
    _position = _position + (int)contracts;

    if (_position != 0)
        _strike = strike;
    else
        _strike = DateTime.MinValue;

    string sStateAfter = "Balance=" + this.Balance + ", Position=" + _position + ", Strike=" + _strike.ToString(Constants.DATEONLYFORMAT);

    logger.Debug(prefix + "LENDER Current State: [" + sStateAfter + "], Previous State: [" + sStateBefore + "]");
}
else {
    // Borrower; This person has less than the price of one contract

    decimal available = this.Balance - balanceLimit;
    decimal contracts = Math.Abs((available / price));
    contracts = Math.Floor(contracts);
    decimal yield = contracts * price;

    // Work out the yielded return, round up
    yield = ((Math.Ceiling(yield * (decimal)multiplier)) / (decimal)multiplier);

    this.Balance = this.Balance + yield;
    _position = _position - (int)contracts;

    if (_position != 0)
        _strike = strike;
    else
        _strike = DateTime.MinValue;

    string sStateAfter = "Balance=" + this.Balance + ", Position=" + _position + ", Strike=" + _strike.ToString(Constants.DATEONLYFORMAT);

    logger.Debug(prefix + "BORROWER Current State: [" + sStateAfter + "], Previous State: [" + sStateBefore + "]");
}
}
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NZ01;

namespace LoanRunner1_ConApp
{
    public class Program
    {
        private static CustomLog logger = new CustomLog("Program", true);

        static void Main(string[] args)
        {
            var prefix = "Main() - ";
            logger.Debug(prefix + "Entering");

            //Test1();
            //Test2();
            //Test3();

            RunLoan();


            // Boilerplate Exit
            Console.WriteLine(prefix + "Press any key...");
            Console.ReadLine();
            CustomLog.Stop();
            Console.WriteLine(prefix + "Exiting");
        }

        public static void RunLoan()
        {
            var prefix = "RunLoan() - ";

            // To maintain a future position, keep this many days
            // at least ahead of today.
            int nDaysHence = 60;

            // Set up an account
            Account account = new Account("TestA");

            // Get the expected balance changes
            SortedList<DateTime, decimal> changes = Balance.GetChanges();

            // Iterate changes and run loan or savings plan
            int countChange = 0;
            foreach (KeyValuePair<DateTime, decimal> change in changes)
            {
                ++countChange;
                DateTime dtChange = change.Key;
                decimal valueChange = change.Value;
                string sChangeNumber = countChange.ToString("D3");

                // If the account is a loan and the account is closed when
                // it hits zero position, stop processing changes.
                if (!account.Active)
                {
                    logger.Debug(prefix + "#ACCOUNT [CLOSED]:" + account.DumpToString());
                    break;
                }

                logger.Debug(prefix + "#CHANGE " + sChangeNumber + ": Date=" + dtChange.ToString(Constants.DATEONLYFORMAT) + ", BalanceChange=" + valueChange.ToString("F2"));
                logger.Debug(prefix + "#ACCOUNT [BEFORE]:" + account.DumpToString());

                // When we make a change, we should have rolled to a strike
                // that is at least nDaysHence.  To check this, work out the
                // date nDaysHence, and check that the current strike is greater.
                // If the current strike is less, then we need to roll until
                // it is greater.
                DateTime dtMinimumStrike = dtChange.AddDays((double)nDaysHence);

                // Is there a position?
                if (account.HasPosition())
                {
                    // Does the date of this change imply that we should have rolled
                    // the position before this date?

                    DateTime strikeCurrent = account.Strike;

                    if (strikeCurrent < dtMinimumStrike)
                    {                        
                        // The current position is in a strike that is too close
                        // so we need to roll to a further strike until we are OK.
                        // This will do all the rolls required in the past to bring 
                        // us up to the date of the current change.
                        account.RollOn(dtChange, nDaysHence);
                    }

                    // The current position is in a strike that has at least
                    // nDaysHence to go, so we can just change this position
                    // without having to roll.
                    account.ChangeBalance(valueChange); // WAS // account.Balance = account.Balance + valueChange;
                    account.TakePosition(dtChange, account.Strike, valueChange);
                }
                else
                {
                    // Add the change to the account, and then take a position 
                    // in a strike at least nDaysHence.
                    account.ChangeBalance(valueChange); // WAS // account.Balance = account.Balance + valueChange;

                    // Get the strike date that is at least nDaysHence away
                    DateTime dtStrike = MarketStrike.GetStrikeNDaysHence(dtChange, nDaysHence);

                    account.TakePosition(dtChange, dtStrike, valueChange);
                }

                logger.Debug(prefix + "#ACCOUNT [AFTER]:" + account.DumpToString());
            }
        }

        
        // Test date load and lookup
        public static void Test1()
        {
            var prefix = "Test1() - ";

            DateTime dt1 = new DateTime(2014, 01, 01); // 5
            DateTime dt2 = new DateTime(2016, 02, 01); // 4.35
            DateTime dt3 = new DateTime(2016, 07, 01); // 5.4
            DateTime dt4 = new DateTime(2017, 02, 01); // 6.9
            DateTime dt5 = new DateTime(2018, 05, 01); // 6

            logger.Debug(prefix + "Date: " + dt1.ToString() + ", Rate: " + InterestRate.GetRate(dt1));
            logger.Debug(prefix + "Date: " + dt2.ToString() + ", Rate: " + InterestRate.GetRate(dt2));
            logger.Debug(prefix + "Date: " + dt3.ToString() + ", Rate: " + InterestRate.GetRate(dt3));
            logger.Debug(prefix + "Date: " + dt4.ToString() + ", Rate: " + InterestRate.GetRate(dt4));
            logger.Debug(prefix + "Date: " + dt5.ToString() + ", Rate: " + InterestRate.GetRate(dt5));

        }


        // Testing the TakePosition and Liquidate functions
        public static void Test2()
        {
            //var prefix = "Test2() - ";

            // Test taking a position.
            DateTime today = new DateTime(2015, 03, 12);
            DateTime strike = new DateTime(2016, 02, 01);

            Account acc1 = new Account("Acc1");
            acc1.Dump("1: No Balance");

            // Take a position with no balance - should fail, result in no position
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("2: After taking position with no balance");

            // Add a tiny balance            
            acc1.ChangeBalance(0.01m); // WAS // acc1.Balance = 0.01m;
            acc1.Dump("3: Added tiny balance");

            // Take position with tiny balance - should fail
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("4: After taking position with tiny balance");

            // Take position with positive balance (LENDING)
            acc1.Reset();
            acc1.ChangeBalance(1.00m); // 1 quid
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("5: After taking position with small but feasible balance");
            // Buying 1 contract, Try immediately selling
            acc1.LiquidatePosition(today);


            acc1.Reset();
            acc1.ChangeBalance(5.00m); // 5 quid
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("6: After taking position with small but feasible balance");
            acc1.LiquidatePosition(today);


            // Take position with negative balance (BORROWING)
            acc1.Reset();
            acc1.ChangeBalance(-1.00m);
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("7: After taking position with small negative balance");
            acc1.LiquidatePosition(today);

            acc1.Reset();
            acc1.ChangeBalance(-5.00m);
            acc1.TakePosition(today, strike); // On 12th March take position in strike Feb 2016
            acc1.Dump("8: After taking position with small negative balance");
            acc1.LiquidatePosition(today);

        }


        public static void Test3()
        {
            //var prefix = "Test3() - ";

            int nDaysHence = 60;
            DateTime today = new DateTime(2015, 03, 12);
            DateTime strike = new DateTime(2015, 06, 01);

            Account acc1 = new Account("Acc1");

            // Have no position, call roll.
            acc1.Roll(today, nDaysHence);
            acc1.Dump("No position, expecting no position after roll");

            // Have small value in balance, roll.
            // Expect to take position in next sensible strike (01-JUN-2015)
            acc1.ChangeBalance(5.00m);
            acc1.Roll(today, nDaysHence);
            acc1.Dump("No position, expecting no change"); // Should use TakePosition to establish starting position

            acc1.Reset();
            acc1.ChangeBalance(-5m);
            acc1.Roll(today, nDaysHence);
            acc1.Dump("No position, expecting no change"); // Should use TakePosition to establish starting position


            // ROLLING POSITION WITH NO BALANCE

            // Have small positive position with no balance and roll
            // Move the day to 61 days away, should have no effect
            DateTime testDay = strike.AddDays(-1 * (nDaysHence+1)); // 61 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = 5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to buy 5 contracts in June strike, no change.");

            testDay = strike.AddDays(-1 * (nDaysHence)); // 60 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = 5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to buy 5 contracts in July strike, cheaper, leaving residual balance");

            testDay = strike.AddDays(-1 * (nDaysHence-1)); // 59 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = 5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to buy 5 contracts in July strike, cheaper, leaving residual balance");


            // Have small negative position with no balance and roll
            // Move the day to 61 days away, should have no effect
            testDay = strike.AddDays(-1 * (nDaysHence + 1)); // 61 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = -5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to sell 5 contracts, no change");

            testDay = strike.AddDays(-1 * (nDaysHence)); // 60 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = -5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to sell 6 contracts in July strike, leaving residual positive balance");
            // Our balance was zero.
            // The cost of buying 5 near months at 0.98 was -5*0.98 = -4.90
            // The money received by selling 5 far months was 5*.97 = +4.85
            // This leaves us slightly negative, which means we have to borrow more.
            // This is to be expected because we are paying interest, and the way to borrow is to take 
            // a short position.  Over time, with no repayment, one would expect the short position
            // to extend indefinitely.


            testDay = strike.AddDays(-1 * (nDaysHence - 1)); // 59 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = -5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to sell 6 contracts in July strike, leaving residual positive balance");





            // ROLLING POSITION WITH BALANCE

            // Have small positive position with small positive balance, roll 
            // Expect the small balance change to allow the position to change
            testDay = strike.AddDays(-1 * (nDaysHence - 10)); // 50 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = 5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.ChangeBalance(3.00m);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to buy more contracts (total=8) in July strike, using additional 3 pounds");

            // Have small positive position with small negative balance, roll 
            // Expect the small balance change to allow the position to change
            testDay = strike.AddDays(-1 * (nDaysHence - 10)); // 50 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = 5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.ChangeBalance(-3.00m);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to buy fewer contracts (total=1) in July strike, using 3 fewer pounds"); // Balance should not go negative

            // Have small negative position with small positive balance, roll 
            // Expect the small balance change to allow the position to change
            testDay = strike.AddDays(-1 * (nDaysHence - 10)); // 50 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = -5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.ChangeBalance(3.00m);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to sell more contracts (total=-2) in July strike");

            // Have small negative position with small negative balance, roll 
            // Expect the small balance change to allow the position to change
            testDay = strike.AddDays(-1 * (nDaysHence - 10)); // 50 days before the strike date that we are taking a position in
            acc1.Reset();
            acc1.Position = -5;
            acc1.Strike = new DateTime(2015, 06, 01);
            acc1.ChangeBalance(-3.00m);
            acc1.Roll(testDay, nDaysHence);
            acc1.Dump("Expect to sell more contracts (total=-9) in July strike");

        }

    } // end of "public class Program"

} // end of "namespace LoanRunner1_ConApp


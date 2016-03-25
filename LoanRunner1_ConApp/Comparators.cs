using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZ01
{

    














    //////////////////////////
    // Date and Time sorters

    public class ByDateTimeAscending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x > y)
                return 1;
            else if (x < y)
                return -1;
            else
                return 0;
        }
    }

    public class ByDateTimeDescending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x < y)
                return 1;
            else if (x > y)
                return -1;
            else
                return 0;
        }
    }


    // End of Date and Time sorters
    /////////////////////////////////



    /////////////////
    // Date sorters

    public class ByDateOnlyAscending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x.Date > y.Date)
                return 1;
            else if (x.Date < y.Date)
                return -1;
            else
                return 0;
        }
    }

    public class ByDateOnlyDescending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x.Date < y.Date)
                return 1;
            else if (x.Date > y.Date)
                return -1;
            else
                return 0;
        }
    }

    // End of Date sorters
    ////////////////////////




    /////////////////
    // Time sorters

    public class ByTimeOnlyAscending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x.TimeOfDay > y.TimeOfDay)
                return 1;
            else if (x.TimeOfDay < y.TimeOfDay)
                return -1;
            else
                return 0;
        }
    }

    public class ByTimeOnlyDescending : IComparer<DateTime>
    {
        public int Compare(DateTime x, DateTime y)
        {
            if (x.TimeOfDay < y.TimeOfDay)
                return 1;
            else if (x.TimeOfDay > y.TimeOfDay)
                return -1;
            else
                return 0;
        }
    }



    //////////////////
    // Int64 sorters

    public class ByInt64Ascending : IComparer<Int64>
    {
        public int Compare(Int64 x, Int64 y)
        {
            if (x == y) return 0;

            if (x > y) return 1;
            else return -1;
        }
    }

    public class ByInt64Descending : IComparer<Int64>
    {
        public int Compare(Int64 x, Int64 y)
        {
            if (x == y) return 0;

            if (x > y) return -1;
            else return 1;
        }
    }

    // Int64 sorters
    //////////////////



} // end of namespace NZ01

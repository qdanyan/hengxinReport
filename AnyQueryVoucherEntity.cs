using System;

namespace ANY.HX.K3.Report
{
    public class QueryVoucherEntity
    {
        public long AcctBook;

        public bool UsePeriod;

        public bool UseDate;

        public int StartYear;

        public int EndYear;

        public int StartPeriod;

        public int EndPeriod;

        public bool IncludeUnPost;

        public DateTime StartDate = DateTime.MinValue;

        public DateTime EndDate = DateTime.MinValue;
    }
}

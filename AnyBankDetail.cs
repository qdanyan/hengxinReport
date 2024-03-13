using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANY.HX.K3.Report
{
    public class BankInfo
    {
        public string Name { get; set; }
        public string Currency { get; set; }
        public decimal SumDr { get; set; }
        public decimal SumCr { get; set; }
        public decimal yesterdayBalance { get; set; }
        public decimal todayBalance { get; set; }

    }
}

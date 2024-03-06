using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ANY.HX.K3.Report
{

    public class AccountBookInfor
    {
        public long CurrencyId { get; set; }

        public int StartYear { get; set; }

        public int StartPeriod { get; set; }

        public int CurrentYear { get; set; }

        public int CurrentPeriod { get; set; }

        public DateTime MinPeriodDate { get; set; }

        public DateTime MaxPeriodDate { get; set; }

        public bool InitialStatus { get; set; }

        public long AccountTableID { get; set; }
    }

}

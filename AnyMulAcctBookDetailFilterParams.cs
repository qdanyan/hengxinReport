using System.Collections.Generic;
using Kingdee.K3.FIN.GL.Common.Core;

namespace ANY.HX.K3.Report
{
    public class MulAcctBookDetailFilterParams
    {
        private long _accountId;

        private bool _distinguishAcct;

        private long _flexItem;

        private string _valueSource = "";

        private string _startDetail = "";

        private string _endDetail = "";

        private bool _useDetailGrp;

        private List<DetailGroup> _detailGrp = new List<DetailGroup>();

        private string _filter = "";

        private string _balancefilter = "";

        private bool _includeForbidItem;

        private bool _includeZeroItem = true;

        public Dictionary<long, List<long>> AcctSystemIdDic { get; set; }

        public long AccountId
        {
            get
            {
                return _accountId;
            }
            set
            {
                _accountId = value;
            }
        }

        public string AccountNumber { get; set; }

        public bool DistinguishAcct
        {
            get
            {
                return _distinguishAcct;
            }
            set
            {
                _distinguishAcct = value;
            }
        }

        public long FlexItem
        {
            get
            {
                return _flexItem;
            }
            set
            {
                _flexItem = value;
            }
        }

        public string ValueSource
        {
            get
            {
                return _valueSource;
            }
            set
            {
                _valueSource = value;
            }
        }

        public string StartDetail
        {
            get
            {
                return _startDetail;
            }
            set
            {
                _startDetail = value;
            }
        }

        public string EndDetail
        {
            get
            {
                return _endDetail;
            }
            set
            {
                _endDetail = value;
            }
        }

        public bool UseDetailGrp
        {
            get
            {
                return _useDetailGrp;
            }
            set
            {
                _useDetailGrp = value;
            }
        }

        public List<DetailGroup> DetailGroupData
        {
            get
            {
                return _detailGrp;
            }
            set
            {
                _detailGrp = value;
            }
        }

        public string Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
            }
        }

        public string BalanceFilter
        {
            get
            {
                return _balancefilter;
            }
            set
            {
                _balancefilter = value;
            }
        }

        public bool IncludeForbidItem
        {
            get
            {
                return _includeForbidItem;
            }
            set
            {
                _includeForbidItem = value;
            }
        }

        public bool IncludeZeroItem
        {
            get
            {
                return _includeZeroItem;
            }
            set
            {
                _includeZeroItem = value;
            }
        }

        public MulAcctBookDetailFilterParams()
        {
            AcctSystemIdDic = new Dictionary<long, List<long>>();
        }
    }
}

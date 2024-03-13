using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.Report.PlugIn;
using Kingdee.BOS.Core.Report.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.Util;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.CN.App.Core;
using Kingdee.K3.FIN.Core.Object.CN;
using Kingdee.K3.FIN.CN.App.Report;
using Kingdee.K3.FIN.Core.FilterCondition;
using Kingdee.BOS.Resource;
using System.Data;
using Kingdee.K3.FIN.Core;

namespace ANY.HX.K3.Report
{
    [Description("ANY | 银行流水账（横向）")]
    [HotUpdate]
    public class AnyBankDetailRepot: SysReportBaseService
    {
        private DateTime _startDate;
        private DateTime _endDate;
        private List<long> _orgIds;
        private List<long> _currencyIds;
        private List<long> _bankAcctIds;
        private bool _isShowLocCurrency;
        private List<long> _innerAcctIds;
        private int _cashOrBankFlag;
        private bool _isAudit;
        private int fseq = 1;
        private bool FNoAccrual;
        private bool FNoBalance;

        //private Dictionary<string, string> bankList = new Dictionary<string, string>()
        //    {
        //        {"416034", "支付宝备用金"},
        //        {"121035", "农信0892"},
        //        {"121036", "农信3865"},
        //        {"121037", "民生3504"},
        //        {"121038", "农商0477美元"},
        //        {"121039", "民生1731美金"},
        //        {"121040", "中行6817"},
        //        {"121041", "青岛银行9317"},
        //        {"121042", "中行5800美金"},
        //        {"126556", "农行9769"},
        //        {"126574", "电子承兑892"},
        //        {"127247", "农商行存单（履约保证金）"},
        //        {"142814", "姜贷款卡7038"},
        //        {"278103", "0054欧元"},
        //        {"299102", "花旗银行"},
        //        {"389554", "农商0089"},
        //        {"433149", "浦发2248"},
        //        {"455852", "交通4423"},
        //        {"455853", "交通5419美元"}
        //    };

        private Dictionary<string, BankInfo> bankList = new Dictionary<string, BankInfo>()
            {
                {"416034", new BankInfo {Name = "支付宝备用金", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121035", new BankInfo {Name = "农信0892", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121036", new BankInfo {Name = "农信3865", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121037", new BankInfo {Name = "民生3504", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121038", new BankInfo {Name = "农商0477美元", Currency = "USD", SumDr = 0m, SumCr = 0m}},
                {"121039", new BankInfo {Name = "民生1731美金", Currency = "USD", SumDr = 0m, SumCr = 0m}},
                {"121040", new BankInfo {Name = "中行6817", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121041", new BankInfo {Name = "青岛银行9317", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"121042", new BankInfo {Name = "中行5800美金", Currency = "USD", SumDr = 0m, SumCr = 0m}},
                {"126556", new BankInfo {Name = "农行9769", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"126574", new BankInfo {Name = "电子承兑892", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"127247", new BankInfo {Name = "农商行存单（履约保证金）", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"142814", new BankInfo {Name = "姜贷款卡7038", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"278103", new BankInfo {Name = "0054欧元", Currency = "EUR", SumDr = 0m, SumCr = 0m}},
                {"299102", new BankInfo {Name = "花旗银行", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"389554", new BankInfo {Name = "农商0089", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"433149", new BankInfo {Name = "浦发2248", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"455852", new BankInfo {Name = "交通4423", Currency = "CNY", SumDr = 0m, SumCr = 0m}},
                {"455853", new BankInfo {Name = "交通5419美元", Currency = "USD", SumDr = 0m, SumCr = 0m}}
            };


        public override void Initialize()
        {
            base.Initialize();
            // 简单账表类型：普通、树形、分页
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            // 报表名称
            this.ReportProperty.ReportName = new LocaleValue("ANY | 银行流水账（横向）", base.Context.UserLocale.LCID);
            // 
            this.IsCreateTempTableByPlugin = true;
            // 
            this.ReportProperty.IsUIDesignerColumns = false;
            // 
            this.ReportProperty.IsGroupSummary = true;
            // 
            this.ReportProperty.SimpleAllCols = false;
            // 单据主键：两行FID相同，则为同一单的两条分录，单据编号可以不重复显示
            this.ReportProperty.PrimaryKeyFieldName = "FSeq";
            // 
            this.ReportProperty.IsDefaultOnlyDspSumAndDetailData = true;

            // 报表主键字段名：默认为FIDENTITYID，可以修改
            this.ReportProperty.IdentityFieldName = "FSeq";
            //
            // 设置精度控制
            List<DecimalControlField> list = new List<DecimalControlField>();
            // 数量
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FQty",
                DecimalControlFieldName = "FUnitPrecision"
            });
            // 单价
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FTAXPRICE",
                DecimalControlFieldName = "FPRICEDIGITS"
            });
            // 金额
            list.Add(new DecimalControlField
            {
                ByDecimalControlFieldName = "FALLAMOUNT",
                DecimalControlFieldName = "FAMOUNTDIGITS"
            });
            this.ReportProperty.DecimalControlFieldList = list;

        }

        public override string GetTableName()
        {
            var result = base.GetTableName();
            return result;
        }


		private void GetFilterCondition(IRptParams filter)
		{
             _startDate = Convert.ToDateTime(filter.FilterParameter.CustomFilter["FStartDate"]);
            _endDate = Convert.ToDateTime(filter.FilterParameter.CustomFilter["FEndDate"]);
            _bankAcctIds = ReportCommonFunction.GetBankOrInnerAccounts(base.Context, filter, "FBankAccountID", "FBankAccountID_Id", false, "CN_BANKACNT");
            _orgIds = ReportCommonFunction.GetOrgIds(filter.FilterParameter.CustomFilter, "FOrgID", "FOrgID_ID");
            string text = filter.FilterParameter.CustomFilter["FCurrencyIds"] as string;
            FNoAccrual = Convert.ToBoolean(filter.FilterParameter.CustomFilter["FNoAccrual"]);
            FNoBalance = Convert.ToBoolean(filter.FilterParameter.CustomFilter["FNoBalance"]);
            List<long> currencyIds;
            if (!text.IsNullOrEmptyOrWhiteSpace())
            {
                currencyIds = (from a in text.Split(new char[]
                {
                    ','
                })
                select Convert.ToInt64(a)).ToList<long>();
            }
            else
            {
                currencyIds = ReportCommonFunction.GetCurrencyId(base.Context);
            }
            _currencyIds = currencyIds;
        }
        /// <summary>
        /// 向报表临时表，插入报表数据
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            if (filter == null || filter.FilterParameter.CustomFilter == null)
            {
                return;
            }

            using (new SessionScope())
            {
                string tempTable = DBUtils.CreateSessionTemplateTable(base.Context, "ANY_BankDetailReport", this.CreateRptTempTable());
                this.InsertTempTable(filter, tempTable);
                this.FillRptTable(tableName, tempTable);
                DBUtils.DropSessionTemplateTable(base.Context, "ANY_BankDetailReport");
            }
        }

        protected string CreateRptTempTable()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("(");
            stringBuilder.AppendLine("    FSeq                  INT                NULL,");
            stringBuilder.AppendLine("    FDC                   NVARCHAR(10)       NULL,");
            stringBuilder.AppendLine("    FEXPLANATION          NVARCHAR(255)      NULL,");
            stringBuilder.AppendLine("    PARTNER               NVARCHAR(255)      NULL,");
            // 遍历 bankList 并添加行
            foreach (var key in bankList.Keys)
            {
                stringBuilder.AppendLine($"    F{key}           DECIMAL(33, 10)    NULL,");
            }
            stringBuilder.AppendLine("    FRemarksRmb           DECIMAL(33, 10)    NULL,");
            stringBuilder.AppendLine("    FRemarksUsd           DECIMAL(33, 10)    NULL");
            stringBuilder.AppendLine(")");
            return stringBuilder.ToString();
        }

        protected void InsertTempTable(IRptParams filter, string tempTable)
        {
            GetBlc(filter, tempTable, "Yesterday");
            GetDetial(filter, tempTable);
            GetBlc(filter, tempTable, "Today");
        }

        protected void GetBlc(IRptParams filter, string tempTable, string day)
        {
            List<DetailBillResult> result = new List<DetailBillResult>();
            this.GetFilterCondition(filter);
            bool isShowLocCurrency = true;
            List<long> innerAcctIds = new List<long>();
            int cashOrBankFlag = 1;
            bool isAudit = false;
            string desc = "昨日余额";
            decimal sumRmb = 0;
            decimal sumUsd = 0;
            if (day == "Today")
            {
                _startDate = _endDate.AddDays(1);
                desc = "今日余额";
            }

            result = CNCommonFunction.GetBalanceAmount(this.Context, _startDate, _orgIds, cashOrBankFlag,
                _currencyIds, _bankAcctIds, isShowLocCurrency, innerAcctIds, isAudit);
            // 创建一个字典来保存每个键对应的值
            Dictionary<string, decimal> values = new Dictionary<string, decimal>();
            foreach (string key in bankList.Keys)
            {
                values.Add(key, 0);
            }

            foreach (DetailBillResult detailBillResult in result)
            {
                long accId = detailBillResult.AcctMasterId;
                string key = accId.ToString();
                if (values.ContainsKey(key))
                {
                    values[key] = detailBillResult.BalanceAmount;
                }

                if (bankList.ContainsKey(key))
                {
                    if (day == "Today")
                    {
                        bankList[key].todayBalance = detailBillResult.BalanceAmount;
                    }
                    else
                    {
                        bankList[key].yesterdayBalance = detailBillResult.BalanceAmount;
                    }
                }

                var currency = bankList[key].Currency;

                if (currency == "CNY")
                {
                    sumRmb += detailBillResult.BalanceAmount;
                    sumRmb = Math.Round(sumRmb, 2);
                }
                else
                {
                    sumUsd += detailBillResult.BalanceAmount;
                    sumUsd = Math.Round(sumUsd, 2);
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            var fieldList = string.Join(", ", values.Keys.Select(key => "F" + key)) + ", FRemarksRmb, FRemarksUSD";
            var valueList = string.Join(", ", values.Values) + ", " + sumRmb + ", " + sumUsd;
            stringBuilder.AppendLine("INSERT INTO " + tempTable + " (FSeq, FEXPLANATION, " + fieldList + ")");
            stringBuilder.AppendLine("VALUES (" + fseq++ + ",'" + desc + "', " + valueList + ")");
            DBUtils.ExecuteDynamicObject(this.Context, stringBuilder.ToString());
        }

        private void GetDetial(IRptParams filter, string tempTable)
        {
            DetailReportCondition filterCondition = this.GetFilterConditionObj(filter);
            DailyDataHelper dailyDataHelper = new DailyDataHelper(base.Context)
            {
                IsJournalRpt = filterCondition.IsJournalRpt,
                IsDetailRpt = true
            };
            List<DetailBillResult> detailList = dailyDataHelper.GetDailyList(filterCondition);
            //遍历DebitAmount>0的detailList记录 收入
            foreach (DetailBillResult detailBillResult in detailList)
            {
                if (detailBillResult.DebitAmount > 0)
                {
                    string partnerName = GetPartnerName(detailBillResult.ContactUnitType, detailBillResult.ContactUnit);

                    string sql = string.Format("INSERT INTO {0} (FSeq, FDC,FEXPLANATION, PARTNER, F{1}) VALUES ({2}, '收入', '{3}', '{4}', {5})",
                                               tempTable, detailBillResult.AcctMasterId, fseq++, detailBillResult.Desc, partnerName, detailBillResult.DebitAmount);
                    DBUtils.ExecuteDynamicObject(this.Context, sql);
                }
            }

            //插入小计和日合计
            this.InsertSummaryAndTotal(tempTable);

            //遍历CreditAmount>0的detailList记录 支出
            foreach (DetailBillResult detailBillResult in detailList)
            {
                if (detailBillResult.CreditAmount > 0)
                {
                    string partnerName = GetPartnerName(detailBillResult.ContactUnitType, detailBillResult.ContactUnit);
                    string sql = string.Format("INSERT INTO {0} (FSeq, FDC, FEXPLANATION, PARTNER, F{1}) VALUES ({2}, '支出', '{3}', '{4}', {5})",
                                                                      tempTable, detailBillResult.AcctMasterId, fseq++, detailBillResult.Desc, partnerName, detailBillResult.CreditAmount);
                    DBUtils.ExecuteDynamicObject(this.Context, sql);
                }
            }

            InsertSummaryAndTotalCr(tempTable);
            //string sqlSumCr = string.Format("INSERT INTO {0} (FSeq, FEXPLANATION) VALUES ({1}, '{2}')",
            //                                              tempTable, fseq++, "小计");
            //DBUtils.ExecuteDynamicObject(this.Context, sqlSumCr);
        }

        protected void InsertSummaryAndTotalCr(string tempTable)
        {
            var bankColumns = bankList.Keys.Select(key => $"F{key}").ToList();

            // 帮助方法，生成特定货币类型的SQL SUM字符串
            string GenerateSumString(string currencyType) =>
                string.Join(" + ", bankColumns.Where(column =>
                {
                    var key = column.Substring(1);
                    return bankList.ContainsKey(key) && bankList[key]?.Currency == currencyType;
                }).Select(column => $"ISNULL(SUM({column}), 0)"));

            string sqlSubtotalCr = $@"
                INSERT INTO {tempTable} (FSeq, FEXPLANATION, {string.Join(", ", bankColumns)}, FRemarksRmb, FRemarksUSD)
                SELECT {fseq++}, '小计', {string.Join(", ", bankColumns.Select(column => $"SUM({column})"))}, 
                    {GenerateSumString("CNY")}, 
                    {GenerateSumString("USD")}
                FROM {tempTable}
                WHERE FDC = '支出';";
            DBUtils.ExecuteDynamicObject(this.Context, sqlSubtotalCr);

            foreach (var bankId in bankList.Keys)
            {
                string sqlQueryCr = $@"
                    SELECT SUM(F{bankId}) AS SumCr
                    FROM {tempTable}
                    WHERE FDC = '支出';";
                var sumCr = DBUtils.ExecuteScalar<decimal>(this.Context, sqlQueryCr, 0);
                bankList[bankId].SumCr = sumCr;
            }
        }


        protected void InsertSummaryAndTotal(string tempTable)
        {
            var bankColumns = bankList.Keys.Select(key => $"F{key}").ToList();
            string sqlSubtotal = $@"
                INSERT INTO {tempTable} (FSeq, FEXPLANATION, {string.Join(", ", bankColumns)}, FRemarksRmb, FRemarksUSD)
                SELECT {fseq++}, '小计', {string.Join(", ", bankColumns.Select(column => $"SUM({column})"))}, 
                    {string.Join(" + ", bankColumns.Where(column => bankList.ContainsKey(column.Substring(1)) && bankList[column.Substring(1)]?.Currency == "CNY").Select(column => $"ISNULL(SUM({column}), 0)"))}, 
                    {string.Join(" + ", bankColumns.Where(column => bankList.ContainsKey(column.Substring(1)) && bankList[column.Substring(1)]?.Currency != "CNY").Select(column => $"ISNULL(SUM({column}), 0)"))}
                FROM {tempTable}
                WHERE FDC = '收入';";
            DBUtils.ExecuteDynamicObject(this.Context, sqlSubtotal);

            foreach (var bankId in bankList.Keys)
            {
                string sqlQueryCr = $@"
            SELECT SUM(F{bankId}) AS SumCr
            FROM {tempTable}
            WHERE FDC = '收入';";
                var sumDr = DBUtils.ExecuteScalar<decimal>(this.Context, sqlQueryCr, 0);
                bankList[bankId].SumDr = sumDr;
            }

            string sqlYesterdayBalance = $@"
                SELECT {string.Join(", ", bankColumns)}
                FROM {tempTable}
                WHERE FEXPLANATION = '昨日余额';";
            var yesterdayBalances = DBUtils.ExecuteDynamicObject(this.Context, sqlYesterdayBalance).FirstOrDefault();

            // 构建插入日合计行的SQL
            string sqlTotal = $@"
                INSERT INTO {tempTable} (FSeq, FEXPLANATION, {string.Join(", ", bankColumns)}, FRemarksRmb, FRemarksUSD)
                SELECT {fseq++}, '日合计', {string.Join(", ", bankColumns.Select(column => $"ISNULL({column}Subtotal, 0) + ISNULL({yesterdayBalances[column]}, 0)"))}, 
                    {string.Join(" + ", bankColumns.Where(column => bankList[column.Substring(1)].Currency == "CNY").Select(column => $"ISNULL({column}Subtotal, 0) + ISNULL({yesterdayBalances[column]}, 0)"))}, 
                    {string.Join(" + ", bankColumns.Where(column => bankList[column.Substring(1)].Currency != "CNY").Select(column => $"ISNULL({column}Subtotal, 0) + ISNULL({yesterdayBalances[column]}, 0)"))}
                FROM (
                    SELECT {string.Join(", ", bankColumns.Select(column => $"SUM({column}) AS {column}Subtotal"))}
                    FROM {tempTable}
                    WHERE FDC = '收入'
                ) AS Subtotals;";
            DBUtils.ExecuteDynamicObject(this.Context, sqlTotal);
        }


        protected DetailReportCondition GetFilterConditionObj(IRptParams filter)
        {
            DetailReportCondition detailReportCondition = new DetailReportCondition();
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            DateTime date = Convert.ToDateTime(customFilter["FStartDate"]).Date;
            DateTime date2 = Convert.ToDateTime(customFilter["FEndDate"]).Date;
            DateTime systemTime = CommonFunction.GetSystemTime(base.Context);
            detailReportCondition.StartDate = ((date == DateTime.MinValue) ? systemTime : date);
            detailReportCondition.EndDate = ((date2 == DateTime.MinValue) ? systemTime : date2);
            detailReportCondition.OrgIds = ReportCommonFunction.GetOrgIds(filter.FilterParameter.CustomFilter, "FOrgID", "FOrgID_ID");
            detailReportCondition.IsNotAudit = Convert.ToBoolean(customFilter["FNotAudit"]);
            detailReportCondition.IsShowStdCurrency = Convert.ToBoolean(customFilter["FMyCurrency"]);
            detailReportCondition.BankAcctMasterIds = _bankAcctIds;
            detailReportCondition.IsShowSumMonth = false;
            detailReportCondition.IsShowSumYear = false;
            detailReportCondition.IsGroupByBillNo = false;
            detailReportCondition.IsJournalRpt = false;
            detailReportCondition.CashBankBizType = CashBankBusinessType.Bank;
            return detailReportCondition;
        }

        protected void FillRptTable(string tableName, string tempTable)
        {
            string sqlStr = string.Format("SELECT * INTO {0} FROM {1}",
                               tableName,tempTable);
            DBUtils.ExecuteDynamicObject(Context, sqlStr);
        }

        protected string GetPartnerName(string partnerType, long partnerId)
        {
            string tableName = string.Empty;
            string keyField = string.Empty;
            switch (partnerType)
            {
                case "BD_Customer":
                    tableName = "T_BD_Customer_L";
                    keyField = "FCUSTID";
                    break;
                case "BD_Supplier":
                    tableName = "T_BD_SUPPLIER_L";
                    keyField = "FSUPPLIERID";
                    break;
                case "BD_Empinfo":
                    tableName = "T_HR_EMPINFO_L";
                    keyField = "FID";
                    break;
                case "BD_Department":
                    tableName = "T_BD_DEPARTMENT_L";
                    keyField = "FDEPTID";
                    break;
                case "FIN_OTHERS":
                    tableName = "T_FIN_OTHERS_L";
                    keyField = "FID";
                    break;
                default:
                    return string.Empty;

            }
            string sqlQuery = string.Format("SELECT FName FROM {0} WHERE {1} = {2} and  FLOCALEID=2052", tableName, keyField, partnerId);
            return DBUtils.ExecuteScalar<string>(this.Context, sqlQuery, string.Empty);
        }


        protected override string GetIdentityFieldIndexSQL(string tableName)
        {
            string result = base.GetIdentityFieldIndexSQL(tableName);
            return result;
        }
        protected override void ExecuteBatch(List<string> listSql)
        {
            base.ExecuteBatch(listSql);
        }

        /// <summary>
        /// 构建出报表列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <remarks>
        /// // 如下代码，演示如何设置同一分组的分组头字段合并
        /// // 需配合Initialize事件，设置分组依据字段(PrimaryKeyFieldName)
        /// ReportHeader header = new ReportHeader();
        /// header.Mergeable = true;
        /// int width = 80;
        /// ListHeader headChild1 = header.AddChild("FBILLNO", new LocaleValue("应付单号"));
        /// headChild1.Width = width;
        /// headChild1.Mergeable = true;
        ///             
        /// ListHeader headChild2 = header.AddChild("FPURMAN", new LocaleValue("采购员"));
        /// headChild2.Width = width;
        /// headChild2.Mergeable = true;
        /// </remarks>
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            var FSeq = header.AddChild("FSeq", new LocaleValue("序号"));
            FSeq.ColIndex = 0;
            FSeq.Visible = false;
            var dc = header.AddChild("FDC", new LocaleValue("收付"));
            dc.ColIndex = 1;
            var status = header.AddChild("FEXPLANATION", new LocaleValue("摘要"));
            status.ColIndex = 2;

            var partner = header.AddChild("PARTNER", new LocaleValue("往来单位"));
            status.ColIndex = 3;
            // 遍历 bankList 并添加报告头
            int index = 4;
            foreach (var item in bankList)
            {
                if (FNoAccrual && (item.Value.SumCr + item.Value.SumDr == 0))
                {
                    continue;
                }

                if (FNoBalance && (item.Value.yesterdayBalance + item.Value.todayBalance == 0) && (item.Value.SumCr + item.Value.SumDr == 0))
                {
                    continue;
                }

                var bankHeader = header.AddChild("F" + item.Key, new LocaleValue(item.Value.Name), SqlStorageType.SqlDecimal);
                bankHeader.ColIndex = index++;
            }

            var FRemarksRmb = header.AddChild("FRemarksRmb", new LocaleValue("备注(人民币合计)"), SqlStorageType.SqlDecimal);
            FRemarksRmb.ColIndex = index++;

            var FRemarksUsd = header.AddChild("FRemarksUsd", new LocaleValue("备注(美金合计)"), SqlStorageType.SqlDecimal);
            FRemarksUsd.ColIndex = index++;

            return header;
        }

        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            var result = base.GetReportTitles(filter);
            //DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            //if (dyFilter != null)
            //{
            //    if (result == null)
            //    {
            //        result = new ReportTitles();
            //    }
            //    result.AddTitle("F_JD_Date", Convert.ToString(dyFilter["F_JD_Date"]));
            //}
            return result;
        }

        protected override string AnalyzeDspCloumn(IRptParams filter, string tablename)
        {
            string result = base.AnalyzeDspCloumn(filter, tablename);
            return result;
        }
        protected override void AfterCreateTempTable(string tablename)
        {
            base.AfterCreateTempTable(tablename);
        }

        /// <summary>
        /// 设置报表合计列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        //public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        //{
            //var result = base.GetSummaryColumnInfo(filter);
            //result.Add(new SummaryField("FAliPay", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //result.Add(new SummaryField("FForTodayBal", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //return result;
        //}
        protected override string GetSummaryColumsSQL(List<SummaryField> summaryFields)
        {
            var result = base.GetSummaryColumsSQL(summaryFields);
            return result;
        }
        protected override System.Data.DataTable GetListData(string sSQL)
        {
            var result = base.GetListData(sSQL);
            return result;
        }
        protected override System.Data.DataTable GetReportData(IRptParams filter)
        {
            var result = base.GetReportData(filter);
            return result;
        }
        protected override System.Data.DataTable GetReportData(string tablename, IRptParams filter)
        {
            var result = base.GetReportData(tablename, filter);
            return result;
        }
        public override int GetRowsCount(IRptParams filter)
        {
            var result = base.GetRowsCount(filter);
            return result;
        }
        protected override string BuilderFromWhereSQL(IRptParams filter)
        {
            string result = base.BuilderFromWhereSQL(filter);
            return result;
        }
        protected override string BuilderSelectFieldSQL(IRptParams filter)
        {
            string result = base.BuilderSelectFieldSQL(filter);
            return result;
        }
        protected override string BuilderTempTableOrderBySQL(IRptParams filter)
        {
            string result = base.BuilderTempTableOrderBySQL(filter);
            return result;
        }
        public override void CloseReport()
        {
            base.CloseReport();
        }
        protected override string CreateGroupSummaryData(IRptParams filter, string tablename)
        {
            string result = base.CreateGroupSummaryData(filter, tablename);
            return result;
        }
        protected override void CreateTempTable(string sSQL)
        {
            base.CreateTempTable(sSQL);
        }
        public override void DropTempTable()
        {
            base.DropTempTable();
        }
        public override System.Data.DataTable GetList(IRptParams filter)
        {
            var result = base.GetList(filter);
            return result;
        }
        public override List<long> GetOrgIdList(IRptParams filter)
        {
            var result = base.GetOrgIdList(filter);
            return result;
        }
        public override List<Kingdee.BOS.Core.Metadata.TreeNode> GetTreeNodes(IRptParams filter)
        {
            var result = base.GetTreeNodes(filter);
            return result;
        }
    }
}

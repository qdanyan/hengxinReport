using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.GL.App.Core;
using Kingdee.K3.FIN.GL.Common.Core;

namespace ANY.HX.K3.Report
{
    public class ReportCommonFunction
    {
        private static GlRptConst glRptConst = new GlRptConst();

        public static RptSystemParamInfor GetSysParameter(Context ctx, long acctBookID, bool isMultiColumnParam = false)
        {
            RptSystemParamInfor rptSystemParamInfor = new RptSystemParamInfor();
            CommonService commonService = new CommonService();
            object paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "SameDirectory");
            if (paramter != null)
            {
                rptSystemParamInfor.ShowAcctDC = Convert.ToBoolean(paramter);
            }
            if (!isMultiColumnParam)
            {
                paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "ShowAcctFullName");
                if (paramter != null)
                {
                    rptSystemParamInfor.AcctShowFullName = Convert.ToBoolean(paramter);
                }
                paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "ShowItemSize");
                if (paramter != null)
                {
                    rptSystemParamInfor.GradingDetailName = Convert.ToBoolean(paramter);
                }
                paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "InheritingPreExplanation");
                if (paramter != null)
                {
                    rptSystemParamInfor.AutoInheritExplanation = Convert.ToBoolean(paramter);
                }
            }
            else
            {
                paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "CostAcctFromBalance");
                if (paramter != null)
                {
                    rptSystemParamInfor.CostAcctFromBalance = Convert.ToBoolean(paramter);
                }
                paramter = commonService.GetParamter(ctx, acctBookID, "GL_SystemParameter", "ProfitAcctFromBalance");
                if (paramter != null)
                {
                    rptSystemParamInfor.ProfitAcctFromBalance = Convert.ToBoolean(paramter);
                }
            }
            return rptSystemParamInfor;
        }

        public static string ReturnPeriodString(int iStartYear, int iEndYear, int iStartPeriod, int iEndPeriod, string alias = "")
        {
            return FilterString.GetFilterStringByPeriod(iStartYear, iStartPeriod, iEndYear, iEndPeriod, alias);
        }

        public static string ReturnYearPeriodString(int iStartYear, int iEndYear, int iStartPeriod, int iEndPeriod, string alias = "")
        {
            string text = "";
            if (iStartYear * 100 + iStartPeriod < iEndYear * 100 + iEndPeriod)
            {
                return string.Format(" ({0}FYearPeriod >=" + Convert.ToInt32(iStartYear * 100 + iStartPeriod) + " and {0}FYearPeriod <=" + Convert.ToInt32(iEndYear * 100 + iEndPeriod) + ")", alias);
            }
            return $" ({alias}FYearPeriod ={Convert.ToInt32(iStartYear * 100 + iStartPeriod)}" + ")";
        }

        public static string ReturnDateString(DateTime iStartdate, DateTime iEnddate, bool includeEqual = true, string alias = "")
        {
            string text = "";
            if (includeEqual)
            {
                if (iStartdate != DateTime.MinValue && iEnddate == DateTime.MinValue)
                {
                    return string.Format(" {0}FDATE >=TO_DATE('" + iStartdate.Date.ToString("yyyy-MM-dd HH:mm:ss") + "')", alias);
                }
                if (iEnddate != DateTime.MinValue && iStartdate == DateTime.MinValue)
                {
                    return string.Format(" {0}FDATE <=TO_DATE('" + iEnddate.AddDays(1.0).Date.AddSeconds(-1.0).ToString("yyyy-MM-dd HH:mm:ss") + "')", alias);
                }
                return string.Format(" ({0}FDATE >=TO_DATE('" + iStartdate.Date.ToString("yyyy-MM-dd HH:mm:ss") + "') and {0}FDATE <=TO_DATE('" + iEnddate.AddDays(1.0).Date.AddSeconds(-1.0).ToString("yyyy-MM-dd HH:mm:ss") + "'))", alias);
            }
            if (iStartdate != DateTime.MinValue && iEnddate == DateTime.MinValue)
            {
                return string.Format(" {0}FDATE >TO_DATE('" + iStartdate.Date.ToString("yyyy-MM-dd HH:mm:ss") + "')", alias);
            }
            if (iEnddate != DateTime.MinValue && iStartdate == DateTime.MinValue)
            {
                return string.Format(" {0}FDATE <TO_DATE('" + iEnddate.AddDays(1.0).Date.AddSeconds(-1.0).ToString("yyyy-MM-dd HH:mm:ss") + "')", alias);
            }
            return string.Format(" ({0}FDATE >TO_DATE('" + iStartdate.Date.ToString("yyyy-MM-dd HH:mm:ss") + "') and {0}FDATE <TO_DATE('" + iEnddate.AddDays(1.0).Date.AddSeconds(-1.0).ToString("yyyy-MM-dd HH:mm:ss") + "'))", alias);
        }

        public static int StartPeriod(Context ctx, long acctBook, int year)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.Append("select min(p.FPERIOD) as FPERIOD from T_BD_ACCOUNTPERIOD p");
            stringBuilder.Append(" inner join T_BD_ACCOUNTCALENDAR c on c.FID=p.FID");
            stringBuilder.Append(" inner join T_BD_ACCOUNTBOOK b on b.FPERIODID=c.FID");
            stringBuilder.AppendFormat(" where b.FBOOKID={0} and p.FYEAR={1}", acctBook, year);
            return DBUtils.ExecuteScalar(ctx, stringBuilder.ToString(), 1, (SqlParam[])null);
        }

        public static string ReturnAccountLevelString(int iStartLevel, int iEndLevel, string alias = "")
        {
            string text = "";
            if (iEndLevel > iStartLevel)
            {
                return string.Format(" and ({0}FLEVEL>=" + iStartLevel + " and {0}FLEVEL<=" + iEndLevel + ")", alias);
            }
            if (iStartLevel == iEndLevel)
            {
                return string.Format(" and {0}FLEVEL=" + iEndLevel, alias);
            }
            return "";
        }

        public static string FilterAccountNumber(string strStartAccount, string strEndAccount, string alias = "")
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(strStartAccount) && strEndAccount == "")
            {
                result = $" AND {alias}FNUMBER >= '{strStartAccount}'";
            }
            else if (strStartAccount == "" && !string.IsNullOrWhiteSpace(strEndAccount))
            {
                result = string.Format(" AND ({0}FNUMBER <= '{1}' OR {0}FNUMBER like '{1}.%')", alias, strEndAccount);
            }
            else if (string.IsNullOrWhiteSpace(strStartAccount) && string.IsNullOrWhiteSpace(strEndAccount))
            {
                result = " ";
            }
            else if (!string.IsNullOrWhiteSpace(strStartAccount) && !string.IsNullOrWhiteSpace(strEndAccount))
            {
                result = ((!(strStartAccount == strEndAccount)) ? string.Format(" AND ({0}FNUMBER>='{1}' AND ({0}FNUMBER <= '{2}' OR {0}FNUMBER like '{2}.%'))", alias, strStartAccount, strEndAccount) : string.Format(" AND ({0}FNUMBER = '{1}' OR {0}FNUMBER like '{1}.%')", alias, strStartAccount));
            }
            return result;
        }

        public static int GetAccountBookAmountdigits(Context ctx, long acctbookId)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.AppendLine(" select FAMOUNTDIGITS from T_BD_CURRENCY c");
            stringBuilder.AppendLine(" inner join T_BD_ACCOUNTBOOK b on b.FCURRENCYID=c.FCURRENCYID");
            stringBuilder.AppendFormat(" where b.FBOOKID=@FBOOKID ");
            return DBUtils.ExecuteScalar(ctx, stringBuilder.ToString(), 1, new SqlParam("@FBOOKID", KDDbType.Int64, acctbookId));
        }

        public static int GetAccountBookPricedigits(Context ctx, long acctbookId)
        {
            string strSql = string.Format("SELECT FPRICEDIGITS FROM T_BD_CURRENCY C\r\n                                            INNER JOIN T_BD_ACCOUNTBOOK B ON B.FCURRENCYID=C.FCURRENCYID\r\n                                            WHERE B.FBOOKID=@FBOOKID", acctbookId);
            SqlParam sqlParam = new SqlParam("@FBOOKID", KDDbType.Int64, acctbookId);
            return DBUtils.ExecuteScalar(ctx, strSql, 6, sqlParam);
        }

        public static int GetRatetypeAmountdigits(Context ctx, long acctbookId)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.AppendLine(" select r.FDIGITS from T_BD_RATETYPE r");
            stringBuilder.AppendLine(" inner join T_BD_ACCOUNTBOOK b on r.FRATETYPEID=b.FRATETYPEID");
            stringBuilder.AppendFormat(" where b.FBOOKID=@FBOOKID ");
            return DBUtils.ExecuteScalar(ctx, stringBuilder.ToString(), 2, new SqlParam("@FBOOKID", KDDbType.Int64, acctbookId));
        }

        public static void SetAmtFieldDigit(ReportProperty rptProperty, string amtFieldName, string amountdigitsFieldName = "FAMOUNTDIGITS")
        {
            if (rptProperty.DecimalControlFieldList == null)
            {
                rptProperty.DecimalControlFieldList = new List<DecimalControlField>();
            }
            for (int num = rptProperty.DecimalControlFieldList.Count - 1; num >= 0; num--)
            {
                DecimalControlField decimalControlField = rptProperty.DecimalControlFieldList[num];
                if (decimalControlField.ByDecimalControlFieldName.EqualsIgnoreCase(amtFieldName))
                {
                    rptProperty.DecimalControlFieldList.RemoveAt(num);
                }
            }
            DecimalControlField decimalControlField2 = new DecimalControlField();
            decimalControlField2.ByDecimalControlFieldName = amtFieldName;
            decimalControlField2.DecimalControlFieldName = amountdigitsFieldName;
            rptProperty.DecimalControlFieldList.Add(decimalControlField2);
        }

        public static AccountBookInfor GetAccountBookInfor(Context ctx, long acctBookID)
        {
            AccountBookInfor accountBookInfor = new AccountBookInfor();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Select FCurrentPeriod,FCurrentYear,");
            stringBuilder.AppendLine("FStartPeriod,FStartYear,FInitialStatus,FCURRENCYID,FACCTTABLEID ");
            stringBuilder.AppendLine("From T_BD_ACCOUNTBOOK Where FBookID=@FBookID");
            using (IDataReader dataReader = DBUtils.ExecuteReader(ctx, stringBuilder.ToString(), new SqlParam("@FBookID", KDDbType.Int64, acctBookID)))
            {
                if (dataReader != null && dataReader.Read())
                {
                    accountBookInfor.CurrentPeriod = dataReader.GetValue<int>(0);
                    accountBookInfor.CurrentYear = dataReader.GetValue<int>(1);
                    accountBookInfor.StartPeriod = dataReader.GetValue<int>(2);
                    accountBookInfor.StartYear = dataReader.GetValue<int>(3);
                    accountBookInfor.InitialStatus = dataReader.GetValue<string>(4) == "1";
                    accountBookInfor.CurrencyId = dataReader.GetValue<long>(5);
                    accountBookInfor.AccountTableID = dataReader.GetValue<long>(6);
                }
            }
            if (accountBookInfor.CurrentYear == 0)
            {
                accountBookInfor.CurrentYear = accountBookInfor.StartYear;
                accountBookInfor.CurrentPeriod = accountBookInfor.StartPeriod;
            }
            stringBuilder.Clear();
            stringBuilder.AppendLine("Select Min(p.FPERIODSTARTDATE) FPERIODSTARTDATE, ");
            stringBuilder.AppendLine("Max(p.FPERIODENDDATE) FPERIODENDDATE ");
            stringBuilder.AppendLine("from T_BD_ACCOUNTPERIOD p");
            stringBuilder.AppendLine("inner join T_BD_ACCOUNTCALENDAR c on c.FID=p.FID");
            stringBuilder.AppendLine("inner join T_BD_ACCOUNTBOOK b on b.FPERIODID=c.FID");
            stringBuilder.AppendFormat(" where (p.FYear>@FYEAR1 Or (p.FYear=@FYEAR2 And p.FPeriod>=@FPERIOD) )");
            stringBuilder.AppendFormat(" And b.FBOOKID=@FBOOKID ");
            List<SqlParam> list = new List<SqlParam>();
            list.Add(new SqlParam("@FYEAR1", KDDbType.Int32, accountBookInfor.StartYear));
            list.Add(new SqlParam("@FYEAR2", KDDbType.Int32, accountBookInfor.StartYear));
            list.Add(new SqlParam("@FPERIOD", KDDbType.Int32, accountBookInfor.StartPeriod));
            list.Add(new SqlParam("@FBOOKID", KDDbType.Int64, acctBookID));
            using IDataReader dataReader2 = DBUtils.ExecuteReader(ctx, stringBuilder.ToString(), list);
            if (dataReader2 != null)
            {
                if (dataReader2.Read())
                {
                    if (!(dataReader2["FPERIODSTARTDATE"] is DBNull))
                    {
                        accountBookInfor.MinPeriodDate = dataReader2.GetDateTime(0);
                    }
                    if (!(dataReader2["FPERIODENDDATE"] is DBNull))
                    {
                        accountBookInfor.MaxPeriodDate = dataReader2.GetDateTime(1);
                        return accountBookInfor;
                    }
                    return accountBookInfor;
                }
                return accountBookInfor;
            }
            return accountBookInfor;
        }

        public static string DelVirtualPostTempTable(Context ctx, string tempTable, bool isNotPost = true, bool immediate = false)
        {
            if (isNotPost && !string.IsNullOrWhiteSpace(tempTable) && !tempTable.EqualsIgnoreCase("T_GL_BALANCE") && !tempTable.EqualsIgnoreCase("T_GL_BALANCEPROFIT"))
            {
                CommonFunction.DropTempTable(ctx, tempTable, immediate);
                tempTable = "";
            }
            return tempTable;
        }

        public static void CreateTempTblIndex(Context ctx, string tmpTable, string indexColumn)
        {
            string tempTableIndexName = CommonFunction.GetTempTableIndexName(tmpTable);
            string strSQL = $" CREATE CLUSTERED INDEX {tempTableIndexName} ON {tmpTable} ({indexColumn}) ";
            DBUtils.Execute(ctx, strSQL);
        }

        public static string GetDateRange(DateTime dtStart, DateTime dtEnd)
        {
            return string.Format(ResManager.LoadKDString("{0} 至 {1}", "003235000007225", SubSystemType.FIN), ConvertDate(dtStart), ConvertDate(dtEnd));
        }

        private static string ConvertDate(DateTime dt)
        {
            return string.Format(ResManager.LoadKDString("{0:yyyy年MM月dd日}", "003235000007228", SubSystemType.FIN), dt);
        }

        public static string GetYearAndPeriodRange(string startYear, string startPeriod, string endYear = "", string endPeriod = "")
        {
            if (endYear.IsNullOrEmptyOrWhiteSpace() || endPeriod.IsNullOrEmptyOrWhiteSpace())
            {
                return $"{startYear}.{startPeriod}";
            }
            return $"{startYear}.{startPeriod} -- {endYear}.{endPeriod}";
        }

        public static bool IsFromOtherReport(DynamicObject dyobj, string isNoEmpty = "ACCTBOOKID")
        {
            if (dyobj[isNoEmpty] != null)
            {
                return true;
            }
            return false;
        }

        public static ReportSearchType GetReportSearchType(IRptParams filter, string isNoEmpty = "ACCTBOOKID")
        {
            Dictionary<string, object> customParams = filter.CustomParams;
            if (customParams == null)
            {
                return ReportSearchType.Other;
            }
            if (!customParams.ContainsKey("IsOneselfFilter"))
            {
                customParams["IsOneselfFilter"] = "False";
            }
            if (Convert.ToBoolean(customParams["IsOneselfFilter"]))
            {
                return ReportSearchType.OneselfFilter;
            }
            if (filter.IsRefresh && !customParams.ContainsKey(KeyConst.PARENTREPORTCURRENTROW_KEY))
            {
                if (customParams.ContainsKey("ISREFRESH") && customParams.ContainsKey("OpenParameter"))
                {
                    Dictionary<string, object> dictionary = customParams["OpenParameter"] as Dictionary<string, object>;
                    if (dictionary != null)
                    {
                        if (dictionary.ContainsKey("ParentFormId"))
                        {
                            string text;
                            if ((text = dictionary["ParentFormId"].ToString().ToUpperInvariant()) != null && text == "GL_RPT_FLEXACCOUNT")
                            {
                                return ReportSearchType.OtherReport;
                            }
                        }
                        else if (dictionary.ContainsKey("VoucherId"))
                        {
                            return ReportSearchType.Voucher;
                        }
                    }
                }
                if (!customParams.ContainsKey("ISREFRESH"))
                {
                    customParams["IsOneselfFilter"] = "True";
                    return ReportSearchType.OneselfFilter;
                }
            }
            if (customParams.ContainsKey(KeyConst.PARENTREPORTFILTER_KEY) && customParams.ContainsKey(KeyConst.PARENTREPORTCURRENTROW_KEY))
            {
                return ReportSearchType.OtherReport;
            }
            if (customParams.ContainsKey("OpenParameter"))
            {
                Dictionary<string, object> dictionary2 = customParams["OpenParameter"] as Dictionary<string, object>;
                if (!customParams.ContainsKey("ISREFRESH") && IsFromOtherReport(filter.FilterParameter.CustomFilter, isNoEmpty))
                {
                    customParams["IsOneselfFilter"] = "True";
                    return ReportSearchType.OneselfFilter;
                }
                if (dictionary2.ContainsKey("VoucherId"))
                {
                    return ReportSearchType.Voucher;
                }
                if (dictionary2.ContainsKey("ParentFormId"))
                {
                    if (dictionary2["ParentFormId"].ToString().ToUpperInvariant() == "GL_CHECKINGACCOUNT")
                    {
                        return ReportSearchType.CheckAccount;
                    }
                    if (dictionary2["ParentFormId"].ToString().EqualsIgnoreCase("GL_MUTILACCOUNTBOOK"))
                    {
                        return ReportSearchType.MulitAccountBalance;
                    }
                    if (dictionary2["ParentFormId"].ToString().EqualsIgnoreCase("GL_VOUCHERSUMMARY"))
                    {
                        return ReportSearchType.VoucherSummary;
                    }
                    if (dictionary2["ParentFormId"].ToString().ToUpperInvariant() == "GL_RPT_FLEXACCOUNT")
                    {
                        return ReportSearchType.OtherReport;
                    }
                }
            }
            return ReportSearchType.Other;
        }

        public static string GetOrderByString(IRptParams filter)
        {
            if (string.IsNullOrWhiteSpace(filter.FilterParameter.SortString))
            {
                return $" ORDER BY FYEAR ASC,FPERIOD ASC,FDATE ASC,FDATATYPE ASC,FVCHGROUPID DESC,FVCHGROUPNO ASC";
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
            List<SortRow> sortRows = filter.FilterParameter.SortRows;
            dictionary.Add("FYEAR", "ASC");
            dictionary.Add("FPERIOD", "ASC");
            dictionary.Add("FDATATYPE", "ASC");
            foreach (SortRow item in sortRows)
            {
                if (!dictionary.ContainsKey(item.SortField.FieldName.ToUpper()))
                {
                    dictionary.Add(item.SortField.FieldName.ToUpper(), Enum.GetName(typeof(Enu_SortType), item.SortType));
                }
            }
            dictionary2.Add("FDATE", "ASC");
            dictionary2.Add("FVCHGROUPID", "ASC");
            dictionary2.Add("FVCHGROUPNO", "ASC");
            foreach (KeyValuePair<string, string> item2 in dictionary2)
            {
                if (!dictionary.ContainsKey(item2.Key))
                {
                    dictionary.Add(item2.Key, item2.Value);
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(" ORDER BY ");
            foreach (KeyValuePair<string, string> item3 in dictionary)
            {
                stringBuilder.AppendFormat("{0} {1},", item3.Key, item3.Value);
            }
            return stringBuilder.ToString().TrimEnd(',');
        }

        public static StringBuilder SignDetailPagingSqlInludeChild(Context ctx, DetailFilterParams pram, bool GetDetailByAcctID = true, bool includeChildAcct = false)
        {
            int num = 0;
            DynamicObjectCollection dynamicObjectCollection = null;
            DynamicObjectCollection dynamicObjectCollection2 = null;
            DynamicObjectCollection dynamicObjectCollection3 = null;
            List<long> list = new List<long>();
            if (GetDetailByAcctID)
            {
                dynamicObjectCollection3 = GetAccountDetailIncludeChild(ctx, pram.AccountNumber);
                if (dynamicObjectCollection3.Count == 1)
                {
                    list.Add(Convert.ToInt64(dynamicObjectCollection3[0]["FID"]));
                    dynamicObjectCollection = DetailBaseInfor(ctx, useDetailGrp: false, list, "", null);
                }
                else
                {
                    list = AccountDetailGroup(dynamicObjectCollection3);
                    dynamicObjectCollection = DetailBaseInfor(ctx, useDetailGrp: false, list, "", null);
                }
            }
            else
            {
                list.Add(0L);
                dynamicObjectCollection = DetailBaseInfor(ctx, useDetailGrp: false, list, pram.ValueSource, null);
            }
            StringBuilder stringBuilder = new StringBuilder();
            if (dynamicObjectCollection.Count > 0)
            {
                foreach (DynamicObject item in dynamicObjectCollection)
                {
                    bool flag;
                    if (pram.ValueSource == item.GetValue<string>("FValueSource") && pram.StartDetail == glRptConst.UnknownItemNumber)
                    {
                        flag = true;
                    }
                    else
                    {
                        dynamicObjectCollection2 = EmptyItems.ValidateSingleItem(ctx, pram.DistinguishAcct, pram.ValueSource, pram.Filter);
                        flag = dynamicObjectCollection2.Count > 0;
                    }
                    if (item.GetValue<string>("FVALUETYPE") == "0")
                    {
                        if (BillPlugInBaseFun.ValueIsNotNullOrWhiteSpace(item["FTABLENAME"]))
                        {
                            num++;
                            stringBuilder.AppendLine(BaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                        }
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "1")
                    {
                        num++;
                        stringBuilder.AppendLine(AuxiliaryBaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "2")
                    {
                        num++;
                        stringBuilder.AppendLine(CustomBaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                    }
                    if (num < dynamicObjectCollection.Count)
                    {
                        stringBuilder.AppendLine(" union all ");
                    }
                }
                return stringBuilder;
            }
            return stringBuilder;
        }

        public static StringBuilder SignDetailPagingSqlInludeChildForMulti(Context ctx, DetailFilterParams pram, Dictionary<long, List<long>> acctSytemIdDic, bool GetDetailByAcctID = true, bool includeChildAcct = false)
        {
            int num = 0;
            DynamicObjectCollection dynamicObjectCollection = null;
            DynamicObjectCollection dynamicObjectCollection2 = null;
            List<long> list = new List<long>();
            list.Add(0L);
            dynamicObjectCollection = DetailBaseInfor(ctx, useDetailGrp: false, list, pram.ValueSource, null);
            StringBuilder stringBuilder = new StringBuilder();
            if (dynamicObjectCollection.Count > 0)
            {
                foreach (DynamicObject item in dynamicObjectCollection)
                {
                    bool flag;
                    if (pram.ValueSource == item.GetValue<string>("FValueSource") && pram.StartDetail == glRptConst.UnknownItemNumber)
                    {
                        flag = true;
                    }
                    else
                    {
                        dynamicObjectCollection2 = EmptyItems.ValidateSingleItem(ctx, pram.DistinguishAcct, pram.ValueSource, pram.Filter);
                        flag = dynamicObjectCollection2.Count > 0;
                    }
                    if (item.GetValue<string>("FVALUETYPE") == "0")
                    {
                        if (BillPlugInBaseFun.ValueIsNotNullOrWhiteSpace(item["FTABLENAME"]))
                        {
                            num++;
                            stringBuilder.AppendLine(BaseResourcePagingSqlStringForMulti(ctx, pram, item, flag, acctSytemIdDic).ToString());
                        }
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "1")
                    {
                        num++;
                        stringBuilder.AppendLine(AuxiliaryBaseResourcePagingSqlStringForMulti(ctx, pram, item, flag, acctSytemIdDic).ToString());
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "2")
                    {
                        num++;
                        stringBuilder.AppendLine(CustomBaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                    }
                    if (num < dynamicObjectCollection.Count)
                    {
                        stringBuilder.AppendLine(" union all ");
                    }
                }
                return stringBuilder;
            }
            return stringBuilder;
        }

        public static StringBuilder IgnoreDetailGroupPagingSql(Context ctx, DetailFilterParams pram, bool isAcctDetail = true)
        {
            int num = 0;
            List<long> list = new List<long>();
            list.Add(pram.FlexItem);
            DynamicObjectCollection dynamicObjectCollection = DetailBaseInfor(ctx, pram.UseDetailGrp, list, pram.ValueSource, pram.DetailGroupData);
            StringBuilder stringBuilder = new StringBuilder();
            if (dynamicObjectCollection.Count > 0)
            {
                foreach (DynamicObject item in dynamicObjectCollection)
                {
                    bool flag = EmptyItems.ValidateExistEmpty(ctx, pram.ValueSource, pram.Filter, pram.BalanceFilter, pram.AccountFilter);
                    if (item.GetValue<string>("FVALUETYPE") == "0")
                    {
                        if (BillPlugInBaseFun.ValueIsNotNullOrWhiteSpace(item["FTABLENAME"]))
                        {
                            num++;
                            stringBuilder.AppendLine(BaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                        }
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "1")
                    {
                        num++;
                        stringBuilder.AppendLine(AuxiliaryBaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                    }
                    else if (item.GetValue<string>("FVALUETYPE") == "2")
                    {
                        num++;
                        stringBuilder.AppendLine(CustomBaseResourcePagingSqlString(ctx, pram, item, flag).ToString());
                    }
                    if (num < dynamicObjectCollection.Count)
                    {
                        stringBuilder.AppendLine(" union all ");
                    }
                }
                return stringBuilder;
            }
            return stringBuilder;
        }

        private static StringBuilder BaseResourcePagingSqlString(Context ctx, DetailFilterParams pram, DynamicObject obj, bool ShowUnknownItem)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string empty = string.Empty;
            string value = obj.GetValue<string>("FPKFIELDNAME");
            string value2 = obj.GetValue<string>("FMASTERIDFIELDNAME");
            string value3 = obj.GetValue<string>("FTABLENAME");
            string value4 = obj.GetValue<string>("FNAMEFIELDNAME");
            string value5 = obj.GetValue<string>("FNUMBERFIELDNAME");
            string value6 = obj.GetValue<string>("FFLEXNUMBER");
            int value7 = obj.GetValue<int>("FNAMEISLOCALE");
            int value8 = obj.GetValue<int>("Fstrategytype");
            empty = ((value8 == 2) ? value2 : value);
            string text = string.Empty;
            if (!string.IsNullOrWhiteSpace(value4))
            {
                text = "rtrim({0})";
                text = ((value7 != 1) ? string.Format(text, "t." + value4) : string.Format(text, "t1." + value4));
            }
            BaseDataTypeGroupInfo groupInfo = pram.GroupInfo;
            bool flag = groupInfo != null && groupInfo.HasGroupField && pram.FlexLevel > 0;
            bool flag2 = groupInfo != null && groupInfo.HasGroupLevelTable && pram.FlexLevel > 0;
            stringBuilder.Append("select FFLEXNUMBER,FID,FNUMBER,FNAME,FFULLPARENTID from (");
            stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER,TO_CHAR(min(t.{1})) as FID,TO_CHAR(t.{2}) as FNUMBER,{3} as FNAME", value6, empty, value5, text);
            if (flag && flag2)
            {
                stringBuilder.AppendFormat(",(isnull(gpl.FFULLPARENTID,'.')||'.'||to_char(t.{0})) as FFULLPARENTID", empty);
                stringBuilder.AppendFormat(",(isnull(gpl.FLEVEL,0)+1) AS FFLEXLEVEL");
            }
            else
            {
                stringBuilder.Append(",'.' as FFULLPARENTID,1 as FFLEXLEVEL");
            }
            stringBuilder.AppendFormat(" from {0} t", value3);
            if (!string.IsNullOrWhiteSpace(value4) && value7 == 1)
            {
                stringBuilder.AppendFormat(" left join {0}_L t1", value3);
                stringBuilder.AppendFormat(" on t.{0}=t1.{0} and t1.FLOCALEID={1} ", value, ctx.UserLocale.LCID);
            }
            if (flag && flag2)
            {
                stringBuilder.AppendFormat(" left join {0} gpl on t.{1}=gpl.fid", groupInfo.GroupLevelTable, groupInfo.GroupFieldName);
            }
            List<string> list = new List<string>();
            if (InitBalanceService.IsTableFieldExists(ctx, value3, "FDocumentStatus"))
            {
                list.Add("(t.FDocumentStatus!='A' and t.FDocumentStatus!='Z')");
            }
            if (!pram.IncludeForbidItem && InitBalanceService.IsTableFieldExists(ctx, value3, "FFORBIDSTATUS"))
            {
                list.Add("t.FFORBIDSTATUS='A'");
            }
            if (list.Count > 0)
            {
                stringBuilder.AppendFormat(" where {0} ", string.Join(" AND ", list));
            }
            else
            {
                stringBuilder.AppendLine(" where 1=1 ");
            }
            string value9 = obj.GetValue<string>("FORGFIELDNAME");
            if (value8 == 2 || value8 == 3)
            {
                stringBuilder.AppendFormat(" AND T.{0}=T.{1}", value, value2);
                stringBuilder.AppendFormat(" AND EXISTS(SELECT {0} FROM {1} B1 WHERE T.{0}=B1.{0} ", value2, value3);
                stringBuilder.Append(BaseDataOrgSeprString(ctx, pram, value8, "B1." + value9));
            }
            string detailNumberFilterSql = GetDetailNumberFilterSql(value5, pram.StartDetail, pram.EndDetail);
            stringBuilder.Append(detailNumberFilterSql);
            if (value8 == 2 || value8 == 3)
            {
                stringBuilder.Append(")");
            }
            stringBuilder.AppendFormat(" GROUP By t.{1},t.{0}", value5, empty);
            if (!string.IsNullOrWhiteSpace(value4))
            {
                stringBuilder.AppendFormat(",{0}", value4);
            }
            if (flag2)
            {
                stringBuilder.Append(",gpl.FFULLPARENTID,gpl.FLEVEL");
            }
            if (ShowUnknownItem && pram.IncludeZeroItem)
            {
                stringBuilder.Append(" union ");
                stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER, TO_CHAR('0') as FID,'{1}' as FNUMBER,N'{2}' as FNAME ,'.' as FFULLPARENTID,1 as FFLEXLEVEL", value6, glRptConst.UnknownItemNumber, glRptConst.UnknownItemName);
            }
            string groupSql = GetGroupSql(ctx, groupInfo, obj, pram);
            if (!string.IsNullOrWhiteSpace(groupSql))
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.Append(groupSql);
            }
            stringBuilder.Append(") tmp ");
            if (pram.FlexLevel > 0)
            {
                stringBuilder.AppendFormat(" where fflexlevel<={0}", pram.FlexLevel);
            }
            return stringBuilder;
        }

        public static string GetDetailNumberFilterSql(string numberFieldName, string startDetail, string endDetail, string tableAlias = "t.")
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(startDetail) && !string.IsNullOrWhiteSpace(endDetail))
            {
                stringBuilder.AppendFormat(" and {0}{1} between '{2}' and '{3}' ", tableAlias, numberFieldName, startDetail, endDetail);
            }
            else if (!string.IsNullOrWhiteSpace(endDetail))
            {
                stringBuilder.AppendFormat(" and {0}{1} <='{2}' ", tableAlias, numberFieldName, endDetail);
            }
            else if (!string.IsNullOrWhiteSpace(startDetail))
            {
                stringBuilder.AppendFormat(" and {0}{1} >='{2}' ", tableAlias, numberFieldName, startDetail);
            }
            return stringBuilder.ToString();
        }

        public static string GetDetailNumberFilterSql(string numberFieldName, List<string> listAcctDems, string tableAlias = "t.")
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<string> list = new List<string>();
            if (listAcctDems != null && listAcctDems.Count > 0)
            {
                foreach (string listAcctDem in listAcctDems)
                {
                    list.Add($" '{listAcctDem}'");
                }
                stringBuilder.AppendFormat(" and {0}{1} in ({2}) ", tableAlias, numberFieldName, string.Join(" , ", list));
            }
            return stringBuilder.ToString();
        }

        public static string GetGroupSql(Context ctx, BaseDataTypeGroupInfo groupInfo, DynamicObject flexObj, DetailFilterParams parm)
        {
            bool flag = groupInfo != null && groupInfo.HasGroupField && parm.FlexLevel > 0;
            bool flag2 = groupInfo != null && groupInfo.HasGroupLevelTable && parm.FlexLevel > 0;
            if (!flag || !flag2)
            {
                return string.Empty;
            }
            string value = flexObj.GetValue<string>("FFLEXNUMBER");
            string value2 = flexObj.GetValue<string>("FPKFIELDNAME");
            string value3 = flexObj.GetValue<string>("FMASTERIDFIELDNAME");
            string value4 = flexObj.GetValue<string>("FNUMBERFIELDNAME");
            string value5 = flexObj.GetValue<string>("FTABLENAME");
            value3 = (string.IsNullOrWhiteSpace(value3) ? value2 : value3);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("\r\n                    select '{0}' FFLEXNUMBER,g.FID,g.FNUMBER,gl.FNAME,g_l.FFULLPARENTID,g_l.FLEVEL AS FFLEXLEVEL\r\n                    from {1} g\r\n                    left join {2} g_l on g.fid=g_l.fid\r\n                    left join {1}_L  gl on g.fid=gl.fid and gl.flocaleid={3}\r\n                ", value, groupInfo.GroupTable, groupInfo.GroupLevelTable, ctx.UserLocale.LCID);
            string detailNumberFilterSql = GetDetailNumberFilterSql(value4, parm.StartDetail, parm.EndDetail, "c.");
            if (!string.IsNullOrWhiteSpace(detailNumberFilterSql))
            {
                stringBuilder.AppendFormat("\r\n                    where exists(\r\n                        select 1\r\n                        from {0} c \r\n                        inner join {1} b on c.{2}=b.fid\r\n                        where {3}={4} {5} and (b.ffullparentid=g_L.ffullparentid or b.ffullparentid like  concat(g_L.FFULLPARENTID,'.%'))\r\n                    )", value5, groupInfo.GroupLevelTable, groupInfo.GroupFieldName, value2, value3, detailNumberFilterSql);
            }
            return stringBuilder.ToString();
        }

        private static StringBuilder BaseResourcePagingSqlStringForMulti(Context ctx, DetailFilterParams pram, DynamicObject obj, bool ShowUnknownItem, Dictionary<long, List<long>> acctSytemIdDic)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string empty = string.Empty;
            string value = obj.GetValue<string>("FPKFIELDNAME");
            string value2 = obj.GetValue<string>("FMASTERIDFIELDNAME");
            string value3 = obj.GetValue<string>("FTABLENAME");
            string value4 = obj.GetValue<string>("FNAMEFIELDNAME");
            string value5 = obj.GetValue<string>("FNUMBERFIELDNAME");
            string value6 = obj.GetValue<string>("FFLEXNUMBER");
            int value7 = obj.GetValue<int>("FNAMEISLOCALE");
            int value8 = obj.GetValue<int>("Fstrategytype");
            empty = (string.IsNullOrWhiteSpace(value2) ? value : value2);
            string text = "''";
            if (!string.IsNullOrWhiteSpace(value4))
            {
                text = "rtrim({0})";
                text = ((value7 != 1) ? string.Format(text, "t." + value4) : string.Format(text, "t1." + value4));
            }
            stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER,TO_CHAR(min(t.{1})) as FID,TO_CHAR(t.{2}) as FNUMBER,{3} as FNAME", value6, empty, value5, text);
            stringBuilder.AppendFormat(" from {0} t", value3);
            if (!string.IsNullOrWhiteSpace(value4) && value7 == 1)
            {
                stringBuilder.AppendFormat(" left join {0}_L t1", value3);
                stringBuilder.AppendFormat(" on t.{0}=t1.{0} and t1.FLOCALEID={1} ", value, ctx.UserLocale.LCID);
            }
            List<string> list = new List<string>();
            if (InitBalanceService.IsTableFieldExists(ctx, value3, "FDocumentStatus"))
            {
                list.Add("(t.FDocumentStatus!='A' and t.FDocumentStatus!='Z')");
            }
            if (!pram.IncludeForbidItem && InitBalanceService.IsTableFieldExists(ctx, value3, "FFORBIDSTATUS"))
            {
                list.Add("t.FFORBIDSTATUS='A'");
            }
            if (list.Count > 0)
            {
                stringBuilder.AppendFormat(" where {0} ", string.Join(" AND ", list));
            }
            else
            {
                stringBuilder.AppendLine(" where 1=1 ");
            }
            string value9 = obj.GetValue<string>("FORGFIELDNAME");
            string text2 = "";
            if (!string.IsNullOrWhiteSpace(value9))
            {
                text2 = CommonFunction.BaseDataOrgSeprString(ctx, acctSytemIdDic, value8, value9);
            }
            if (value8 == 2)
            {
                stringBuilder.AppendFormat(" AND T.{0}=T.{1} AND EXISTS(SELECT 1 FROM {2} B1 WHERE T.{0}=B1.{0} {3})", value2, value, value3, text2);
            }
            else
            {
                stringBuilder.AppendLine(text2);
            }
            if (string.IsNullOrWhiteSpace(pram.StartDetail) && pram.EndDetail != "")
            {
                stringBuilder.AppendFormat(" and t.{0} <='{1}' ", value5, pram.EndDetail);
            }
            else if (string.IsNullOrWhiteSpace(pram.EndDetail) && pram.StartDetail != "")
            {
                stringBuilder.AppendFormat(" and t.{0} >='{1}'", value5, pram.StartDetail);
            }
            else if (!string.IsNullOrWhiteSpace(pram.StartDetail) && !string.IsNullOrWhiteSpace(pram.EndDetail))
            {
                stringBuilder.AppendFormat(" and t.{0} between '{1}' and '{2}'", value5, pram.StartDetail, pram.EndDetail);
            }
            stringBuilder.AppendFormat(" GROUP By t.{1},t.{0}", value5, empty);
            if (!string.IsNullOrWhiteSpace(value4))
            {
                stringBuilder.AppendFormat(",{0}", value4);
            }
            if (ShowUnknownItem && pram.IncludeZeroItem)
            {
                stringBuilder.Append(" union ");
                stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER, TO_CHAR('0') as FID,'{1}' as FNUMBER,N'{2}' as FNAME ", obj.GetValue<string>("FFLEXNUMBER"), glRptConst.UnknownItemNumber, glRptConst.UnknownItemName);
            }
            return stringBuilder;
        }

        private static StringBuilder AuxiliaryBaseResourcePagingSqlString(Context ctx, DetailFilterParams pram, DynamicObject obj, bool bl)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER,", obj["FFLEXNUMBER"].ToString());
            stringBuilder.AppendLine(" TO_CHAR(m.FENTRYID) as FID,TO_CHAR(m.FNUMBER) as FNUMBER,ISNULL(l.FDATAVALUE, ' ') as FNAME ");
            stringBuilder.AppendFormat(" from {0} t ", "T_BD_FLEXITEMPROPERTY");
            stringBuilder.AppendLine(" inner join (select FID,TO_CHAR(FENTRYID) FENTRYID,TO_CHAR(FNUMBER) FNUMBER from T_BAS_ASSISTANTDATAENTRY ");
            stringBuilder.AppendLine(" where (FDocumentStatus!='A' and FDocumentStatus!='Z') ");
            if (!pram.IncludeForbidItem)
            {
                stringBuilder.AppendLine(" and FFORBIDSTATUS='A' ");
            }
            stringBuilder.AppendLine(BaseDataOrgSeprString(ctx, pram, obj["FSTRATEGYTYPE"], obj["FORGFIELDNAME"]));
            if (string.IsNullOrWhiteSpace(pram.StartDetail) && pram.EndDetail != "")
            {
                stringBuilder.AppendFormat(" and FNUMBER <='{0}' ", pram.EndDetail);
            }
            else if (string.IsNullOrWhiteSpace(pram.EndDetail) && pram.StartDetail != "")
            {
                stringBuilder.AppendFormat(" and FNUMBER >='{0}'", pram.StartDetail);
            }
            else if (!string.IsNullOrWhiteSpace(pram.StartDetail) && !string.IsNullOrWhiteSpace(pram.EndDetail))
            {
                stringBuilder.AppendFormat(" and FNUMBER between '{0}' and '{1}'", pram.StartDetail, pram.EndDetail);
            }
            if (bl && pram.IncludeZeroItem)
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.AppendFormat(" select distinct '{3}' as FID, TO_CHAR('{1}') as FENTRYID,TO_CHAR('{2}') as FNUMBER from {0}", glRptConst.UnknownItemQueryTable, glRptConst.UnknownItemId, glRptConst.UnknownItemNumber, obj["FVALUESOURCE"].ToString());
            }
            stringBuilder.AppendLine(")m on m.FID=t.FVALUESOURCE ");
            stringBuilder.AppendFormat(" and t.FVALUESOURCE='{0}'", obj["FVALUESOURCE"].ToString());
            stringBuilder.AppendLine(" left join (");
            stringBuilder.AppendLine(" select FENTRYID,ISNULL(FDATAVALUE,' ') FDATAVALUE,FLOCALEID from T_BAS_ASSISTANTDATAENTRY_L");
            if (bl && pram.IncludeZeroItem)
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.AppendFormat(" select distinct TO_CHAR('{3}') as FENTRYID,N'{0}' as FDATAVALUE,TO_NUMBER({1}) as FLOCALEID from {2}_L", glRptConst.UnknownItemName, ctx.UserLocale.LCID, glRptConst.UnknownItemQueryTable, glRptConst.UnknownItemId);
            }
            stringBuilder.AppendLine(") l");
            stringBuilder.AppendFormat(" on m.FENTRYID=l.FENTRYID and l.FLOCALEID={0}", ctx.UserLocale.LCID);
            return stringBuilder;
        }

        private static StringBuilder AuxiliaryBaseResourcePagingSqlStringForMulti(Context ctx, DetailFilterParams pram, DynamicObject obj, bool bl, Dictionary<long, List<long>> acctSytemIdDic)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER,", obj["FFLEXNUMBER"].ToString());
            stringBuilder.AppendLine(" TO_CHAR(m.FENTRYID) as FID,TO_CHAR(m.FNUMBER) as FNUMBER,ISNULL(l.FDATAVALUE,' ') as FNAME ");
            stringBuilder.AppendFormat(" from {0} t ", "T_BD_FLEXITEMPROPERTY");
            stringBuilder.AppendLine(" inner join (select FID,TO_CHAR(FENTRYID) FENTRYID,TO_CHAR(FNUMBER) FNUMBER from T_BAS_ASSISTANTDATAENTRY ");
            stringBuilder.AppendLine(" where (FDocumentStatus!='A' and FDocumentStatus!='Z') ");
            if (!pram.IncludeForbidItem)
            {
                stringBuilder.AppendLine(" and FFORBIDSTATUS='A' ");
            }
            stringBuilder.AppendLine(CommonFunction.BaseDataOrgSeprString(ctx, acctSytemIdDic, obj["FSTRATEGYTYPE"], obj["FORGFIELDNAME"]));
            if (string.IsNullOrWhiteSpace(pram.StartDetail) && pram.EndDetail != "")
            {
                stringBuilder.AppendFormat(" and FNUMBER <='{0}' ", pram.EndDetail);
            }
            else if (string.IsNullOrWhiteSpace(pram.EndDetail) && pram.StartDetail != "")
            {
                stringBuilder.AppendFormat(" and FNUMBER >='{0}'", pram.StartDetail);
            }
            else if (!string.IsNullOrWhiteSpace(pram.StartDetail) && !string.IsNullOrWhiteSpace(pram.EndDetail))
            {
                stringBuilder.AppendFormat(" and FNUMBER between '{0}' and '{1}'", pram.StartDetail, pram.EndDetail);
            }
            if (bl && pram.IncludeZeroItem)
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.AppendFormat(" select distinct '{3}' as FID, TO_CHAR('{1}') as FENTRYID,TO_CHAR('{2}') as FNUMBER from {0}", glRptConst.UnknownItemQueryTable, glRptConst.UnknownItemId, glRptConst.UnknownItemNumber, obj["FVALUESOURCE"].ToString());
            }
            stringBuilder.AppendLine(")m on m.FID=t.FVALUESOURCE ");
            stringBuilder.AppendFormat(" and t.FVALUESOURCE='{0}'", obj["FVALUESOURCE"].ToString());
            stringBuilder.AppendLine(" left join (");
            stringBuilder.AppendLine(" select FENTRYID,ISNULL(FDATAVALUE,' ') FDATAVALUE,FLOCALEID from T_BAS_ASSISTANTDATAENTRY_L");
            if (bl && pram.IncludeZeroItem)
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.AppendFormat(" select distinct TO_CHAR('{3}') as FENTRYID,N'{0}' as FDATAVALUE,TO_NUMBER({1}) as FLOCALEID from {2}_L", glRptConst.UnknownItemName, ctx.UserLocale.LCID, glRptConst.UnknownItemQueryTable, glRptConst.UnknownItemId);
            }
            stringBuilder.AppendLine(") l");
            stringBuilder.AppendFormat(" on m.FENTRYID=l.FENTRYID and l.FLOCALEID={0}", ctx.UserLocale.LCID);
            return stringBuilder;
        }

        private static StringBuilder CustomBaseResourcePagingSqlString(Context ctx, DetailFilterParams pram, DynamicObject obj, bool bl)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(" select '{0}' as FFLEXNUMBER,", obj["FFLEXNUMBER"].ToString());
            stringBuilder.AppendLine("TO_CHAR(FID) as FID,FNUMBER,FNAME from (");
            stringBuilder.AppendFormat(" select distinct TO_CHAR({0}) as FID,{0} as FNUMBER,{0} as FNAME from T_BD_FLEXITEMDETAILV ", obj["FFLEXNUMBER"].ToString());
            stringBuilder.AppendLine(" where " + string.Format(glRptConst.ItemNotNullOrWhiteSpace, obj["FFLEXNUMBER"].ToString(), ""));
            if (string.IsNullOrWhiteSpace(pram.StartDetail) && pram.EndDetail != "")
            {
                stringBuilder.AppendFormat(" and {0} <='{1}' ", obj["FFLEXNUMBER"].ToString(), pram.EndDetail);
            }
            else if (string.IsNullOrWhiteSpace(pram.EndDetail) && pram.StartDetail != "")
            {
                stringBuilder.AppendFormat(" and {0} >='{1}'", obj["FFLEXNUMBER"].ToString(), pram.StartDetail);
            }
            else if (!string.IsNullOrWhiteSpace(pram.StartDetail) && !string.IsNullOrWhiteSpace(pram.EndDetail))
            {
                stringBuilder.AppendFormat(" and {0} between '{1}' and '{2}'", obj["FFLEXNUMBER"].ToString(), pram.StartDetail, pram.EndDetail);
            }
            if (bl && pram.IncludeZeroItem)
            {
                stringBuilder.AppendLine(" union ");
                stringBuilder.AppendFormat(" select distinct TO_CHAR({1}) as FID, TO_CHAR('{2}') as FNUMBER,N'{3}' as FNAME from {0}", glRptConst.UnknownItemQueryTable, glRptConst.UnknownItemId, glRptConst.UnknownItemNumber, glRptConst.UnknownItemName);
            }
            stringBuilder.AppendLine(")m ");
            return stringBuilder;
        }

        public static DynamicObjectCollection DetailBaseInfor(Context ctx, bool useDetailGrp, List<long> flexItem, string valueSource, List<DetailGroup> detailGrp)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(" select distinct c.FNAMEISLOCALE, c.FTABLENAME,c.FPKFIELDNAME,c.FMASTERIDFIELDNAME,c.FNUMBERFIELDNAME,c.FNAMEFIELDNAME,b.FVALUETYPE,");
            stringBuilder.AppendLine(" b.FVALUESOURCE,b.FFLEXNUMBER,c.FORGFIELDNAME,ISNULL(t.FSTRATEGYTYPE,0) as FSTRATEGYTYPE ");
            stringBuilder.AppendFormat(" from {0} b", "T_BD_FLEXITEMPROPERTY");
            stringBuilder.AppendLine(" left join T_META_LOOKUPCLASS c on c.FFORMID=b.FVALUESOURCE");
            stringBuilder.AppendLine(" left join T_META_BASEDATATYPE t on b.FVALUESOURCE=t.FBASEDATATYPEID ");
            if (!useDetailGrp)
            {
                if (!valueSource.IsNullOrEmptyOrWhiteSpace())
                {
                    stringBuilder.AppendFormat(" where b.FVALUESOURCE='{0}'", valueSource);
                }
                else if (flexItem.Count > 0)
                {
                    stringBuilder.AppendFormat(" where b.FID in({0})", string.Join(",", flexItem));
                }
            }
            else if (detailGrp != null && detailGrp.Count != 0)
            {
                StringBuilder stringBuilder2 = new StringBuilder();
                foreach (DetailGroup item in detailGrp)
                {
                    stringBuilder2.AppendLine("'" + item.DetailType + "',");
                }
                stringBuilder.AppendFormat(" where FVALUESOURCE in ({0})", stringBuilder2.ToString().Remove(stringBuilder2.ToString().LastIndexOf(","), 1));
            }
            return DBUtils.ExecuteDynamicObject(ctx, stringBuilder.ToString(), null, null, CommandType.Text);
        }

        private static DynamicObjectCollection GetAccountDetail(Context ctx, long accountId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("select distinct t.FID,t.FVALUESOURCE from T_BD_ACCOUNT a ");
            stringBuilder.AppendLine(" inner join T_BD_FLEXITEMGROUP f on f.FID=a.FITEMDETAILID");
            stringBuilder.AppendLine(" inner join T_BD_FLEXITEMGRPENTRY e on f.FID=e.FID");
            stringBuilder.AppendFormat(" inner join {0} t on e.FDATAFIELDNAME=t.FFLEXNUMBER", "T_BD_FLEXITEMPROPERTY");
            stringBuilder.AppendLine(" where a.FACCTID=@facctid");
            SqlParam sqlParam = new SqlParam("@facctid", KDDbType.Int64, accountId);
            return DBUtils.ExecuteDynamicObject(ctx, stringBuilder.ToString(), null, null, CommandType.Text, sqlParam);
        }

        private static DynamicObjectCollection GetAccountDetailIncludeChild(Context ctx, string acctNumber)
        {
            string strSQL = string.Format("\r\n                    SELECT distinct d.FID,d.FVALUESOURCE FROM t_bd_account a\r\n                    inner JOIN T_BD_FLEXITEMGROUP g ON a.FITEMDETAILID=g.FID\r\n                    inner JOIN T_BD_FLEXITEMGRPENTRY e ON g.fid=e.FID\r\n                    LEFT JOIN T_BD_FLEXITEMGROUP_L gl ON g.fid=gl.FID AND gl.FLOCALEID={0}\r\n                    inner JOIN T_BD_FLEXITEMPROPERTY d ON e.FFLEXITEMPROPERTYID=d.FID\r\n                    where {1}", ctx.UserLocale.LCID, string.Format(" (a.FNUMBER='{0}' or a.FNUMBER LIKE '{0}.%') ", acctNumber));
            return DBUtils.ExecuteDynamicObject(ctx, strSQL, null, null, CommandType.Text);
        }

        private static List<long> AccountDetailGroup(DynamicObjectCollection dycol)
        {
            List<long> list = new List<long>();
            foreach (DynamicObject item in dycol)
            {
                list.Add(long.Parse(item["FID"].ToString()));
            }
            return list;
        }

        public static string BaseDataOrgSeprString(Context ctx, DetailFilterParams pram, object strategType, object orgField, string tableAlias = null)
        {
            return CommonFunction.BaseDataOrgSeprString(ctx, pram.AcctSystemID, pram.AcctBookOrg, strategType, orgField, tableAlias);
        }

        public static string BaseDataOrgSeprString(Context ctx, MulAcctBookDetailFilterParams pram, object strategType, object orgField)
        {
            return CommonFunction.BaseDataOrgSeprString(ctx, pram.AcctSystemIdDic, strategType, orgField);
        }

        public static string DealColumnName(string colName, string signColumn)
        {
            if (colName.Contains("'"))
            {
                colName = colName.Replace("'", "''");
            }
            return signColumn + "'" + colName + "',";
        }

        public static decimal GetSumDC(EnumerableRowCollection<DataRow> dataRowCol, string acctBook, string detailId, string columnName, string Name)
        {
            return (from t in dataRowCol
                    where t.Field<string>("AcctBook") == acctBook
                    select t into W
                    where W.Field<string>(Name) == detailId
                    select W into f
                    select f.Field<decimal>(columnName)).Sum();
        }

        public static string GetDetailNameById(Context ctx, long detailId)
        {
            if (detailId <= 0)
            {
                return string.Empty;
            }
            Dictionary<string, Tuple<string, Field>> dictionary = new Dictionary<string, Tuple<string, Field>>();
            StringBuilder stringBuilder = new StringBuilder();
            EnumPkFieldType enumPkFieldType = EnumPkFieldType.INT;
            IMetaDataService service = ServiceHelper.GetService<IMetaDataService>();
            FormMetadata formMetadata = (FormMetadata)service.Load(ctx, "BD_FLEXITEMDETAILV");
            IEnumerable<Field> enumerable = from f in formMetadata.BusinessInfo.GetFieldList()
                                            where f is AssistantField || f is BaseDataField
                                            select f;
            SqlParam sqlParam = new SqlParam("@FID", KDDbType.Int64, detailId);
            DynamicObject dynamicObject = DBUtils.ExecuteDynamicObject(ctx, string.Format("SELECT * FROM {0} WHERE FID=@FID", "T_BD_FLEXITEMDETAILV", detailId), null, null, CommandType.Text, sqlParam)[0];
            foreach (Field item in enumerable)
            {
                string fieldName = item.FieldName;
                string text = Convert.ToString(dynamicObject[item.Key]);
                if (!string.IsNullOrWhiteSpace(text) && text != "0")
                {
                    dictionary.Add(fieldName, new Tuple<string, Field>(text, item));
                }
            }
            if (dictionary.Count == 0)
            {
                return string.Empty;
            }
            foreach (string key in dictionary.Keys)
            {
                Tuple<string, Field> tuple = dictionary[key];
                string text2;
                string text3;
                string text4;
                if (tuple.Item2 is AssistantField)
                {
                    AssistantField assistantField = tuple.Item2 as AssistantField;
                    _ = (string)assistantField.Name;
                    text2 = "T_BAS_ASSISTANTDATAENTRY";
                    text3 = "FENTRYID";
                    text4 = "A.FNUMBER,FDATAVALUE AS FNAME";
                    enumPkFieldType = assistantField.LookUpObject.PkFieldType;
                }
                else
                {
                    BaseDataField baseDataField = tuple.Item2 as BaseDataField;
                    _ = baseDataField.LookUpObject.Name;
                    text2 = baseDataField.LookUpObject.TableName;
                    text3 = baseDataField.LookUpObject.PkFieldName;
                    text4 = "A.FNUMBER,FNAME";
                    enumPkFieldType = baseDataField.LookUpObject.PkFieldType;
                }
                stringBuilder.AppendLine(" UNION ALL ");
                stringBuilder.AppendFormat("SELECT TOP 1\t{0}\r\n                                        FROM {1} A\r\n                                        LEFT JOIN {1}_L B\r\n\t                                        ON A.{2} = B.{2} AND B.FLOCALEID = {4}\r\n                                        WHERE A.{2} = {3}", text4, text2, text3, (enumPkFieldType != EnumPkFieldType.STRING) ? tuple.Item1 : $"'{tuple.Item1}'", ctx.UserLocale.LCID);
            }
            DynamicObjectCollection dynamicObjectCollection = DBUtils.ExecuteDynamicObject(ctx, stringBuilder.ToString().ReplaceFirst(" UNION ALL ", ""), null, null, CommandType.Text);
            stringBuilder.Clear();
            foreach (DynamicObject item2 in dynamicObjectCollection)
            {
                stringBuilder.Append("/");
                stringBuilder.AppendFormat("[{0}]{1}", item2["FNUMBER"], item2["FNAME"]);
            }
            return stringBuilder.ToString().ReplaceFirst("/", "");
        }

        public static string GetAcctFlexsName(Context ctx, int acctId)
        {
            string strSQL = string.Format("select  fpl.FNAME\r\n                            from    T_BD_ACCOUNTFLEXENTRY ae\r\n                                    inner join t_bd_flexitemproperty fp on ae.FDATAFIELDNAME = fp.FFLEXNUMBER\r\n                                    LEFT join T_BD_FLEXITEMPROPERTY_L fpl on fp.FID = fpl.FID and fpl.flocaleid={1} \r\n                            where   ae.FACCTID = {0}", acctId, ctx.UserLocale.LCID);
            List<string> list = new List<string>();
            using (IDataReader dataReader = DBUtils.ExecuteReader(ctx, strSQL))
            {
                while (dataReader.Read())
                {
                    list.Add(string.Format(ResManager.LoadKDString("[未录入]{0}", "003235000018024", SubSystemType.FIN), dataReader["FNAME"]));
                }
            }
            return string.Join("/", list);
        }

        public static string InheritPreviousExplantion(Context ctx, bool inheritExplanationPrev, string voucherEntryTemp, QueryVoucherEntity queryVoucherEntity)
        {
            string text = DBUtils.CreateSessionTemplateTable(ctx, "TM_GL_MULTIEXPVOUCHERENTRY", DefineTempTableMultiExpVoucherEntry());
            DBUtils.ExecuteWithTime(ctx, GetVoucherEntry(ctx, voucherEntryTemp, IsMultiCollect: true, queryVoucherEntity).ToString(), null, 12000);
            StringBuilder stringBuilder = new StringBuilder("");
            if (inheritExplanationPrev)
            {
                stringBuilder.Clear();
                stringBuilder.AppendFormat("insert into {0}(FENTRYSEQ, fvoucherid, fexplanation) select FENTRYSEQ, fvoucherid, fexplanation from {1}", text, voucherEntryTemp);
                stringBuilder.Append(" where isnull(fexplanation, ' ') <> ' ' order by FENTRYSEQ desc");
                DBUtils.ExecuteWithTime(ctx, stringBuilder.ToString(), null, 12000);
                stringBuilder.Clear();
                stringBuilder.AppendFormat("update {0} as t0", voucherEntryTemp);
                stringBuilder.AppendFormat(" set fexplanation = (select top 1 t1.fexplanation from {0} t1 ", text);
                stringBuilder.Append(" where t1.fvoucherid = t0.fvoucherid and t1.FENTRYSEQ < t0.FENTRYSEQ) where isnull(t0.fexplanation, ' ') = ' '");
            }
            else
            {
                stringBuilder.Clear();
                stringBuilder.AppendFormat("insert into {0}(FVoucherID,FENTRYID,FEXPLANATION,FENTRYSEQ) select FVoucherID,FENTRYID,FEXPLANATION,FENTRYSEQ from {1} where TRIM(FEXPLANATION) is not null and FENTRYSEQ=1 ", text, voucherEntryTemp);
                DBUtils.ExecuteWithTime(ctx, stringBuilder.ToString(), null, 12000);
                stringBuilder.Clear();
                if (ctx.DatabaseType == DatabaseType.MS_SQL_Server)
                {
                    stringBuilder.AppendFormat(" update {0} as v set(FEXPLANATION)=", voucherEntryTemp);
                    stringBuilder.AppendLine("(select (case when v.FEXPLANATION is null then b.FEXPLANATION else (case when v.FEXPLANATION='' then b.FEXPLANATION else (case when v.FEXPLANATION=' ' then b.FEXPLANATION else v.FEXPLANATION end) end) end) as FEXPLANATION");
                    stringBuilder.AppendFormat(" from  {0}  b where v.FVoucherID=b.FVoucherID)", text);
                }
                else
                {
                    string arg = $"Select FEXPLANATION ,FVoucherID from {text} ";
                    string arg2 = $"Select FEXPLANATION, FVoucherID From {voucherEntryTemp} Where  nvl(FEXPLANATION,' ') = ' '";
                    stringBuilder.AppendFormat("/*dialect*/  Merge into ({0}) T Using ({1}) K ", arg2, arg);
                    stringBuilder.AppendLine(" On ( T.FVoucherID = K.FVoucherID ) ");
                    stringBuilder.AppendLine(" WHEN MATCHED THEN UPDATE SET  T.FEXPLANATION = K.FEXPLANATION  ");
                }
            }
            DBUtils.ExecuteWithTime(ctx, stringBuilder.ToString(), null, 12000);
            DBUtils.DropSessionTemplateTable(ctx, text);
            return voucherEntryTemp;
        }

        public static string DefineTempTableMultiVoucherEntry()
        {
            return "\r\n                                    (\r\n                                       FVoucherID           int                  null default 0,\r\n                                       FENTRYID             int                  null default 0,\r\n                                       FEXPLANATION         nvarchar(1000)        null,\r\n                                       FACCOUNTID           int                  null default 0,\r\n                                       FDETAILID            int                  null default 0,\r\n                                       FAMOUNTFOR           decimal(28,10)       null default 0,\r\n                                       FAMOUNT              decimal(28,10)       null default 0,\r\n                                       fdebit               decimal(28,10)       null default 0,\r\n                                       fcredit              decimal(28,10)       null default 0,\r\n                                       FCURRENCYID          int                  null default 0,\r\n                                       FEXCHANGERATETYPE    int                  null default 0,\r\n                                       FEXCHANGERATE        decimal(23,10)       null default 0,\r\n                                       FDC                  smallint             null default -1,\r\n                                       FMEASUREUNITID       int                  null default 0,\r\n                                       FUNITPRICE           decimal(23,10)       null default 0,\r\n                                       FQUANTITY            decimal(23,10)       null default 0,\r\n                                       FSettleTypeID        int                  null default 0,\r\n                                       FSettleNo            nvarchar(510)        null,\r\n                                       fentryseq            int                  null default 0,\r\n                                       FSideEntrySeq        int                  null default 0,\r\n                                       FISMULTICOLLECT      char(1)              null default '1',\r\n                                       FACCTUNITQTY         decimal(23, 10)      NULL default 0,\r\n                                       FBASEUNITQTY         decimal(23,10)       NULL default 0\r\n                                    )";
        }

        public static string DefineTempTableMultiExpVoucherEntry()
        {
            return "\r\n                                    (\r\n                                       FVoucherID           int                  null default 0,\r\n                                       FENTRYID             int                  null default 0,\r\n                                       FEXPLANATION         nvarchar(510)        null,\r\n                                       fentryseq            int                  null default 0\r\n                                    )";
        }

        private static StringBuilder GetVoucherEntry(Context ctx, string tempTableName, bool IsMultiCollect, QueryVoucherEntity queryVoucherEntity)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.AppendFormat("insert into {0}(", tempTableName);
            stringBuilder.Append("FVoucherID,FENTRYID,FEXPLANATION,FACCOUNTID,FDETAILID,");
            stringBuilder.Append("FAMOUNTFOR,FAMOUNT,fdebit,fcredit,");
            stringBuilder.Append("FCURRENCYID,FEXCHANGERATETYPE,FEXCHANGERATE,FDC,");
            stringBuilder.Append("FMEASUREUNITID,FUNITPRICE,FQUANTITY,FSettleTypeID,FSettleNo,");
            stringBuilder.Append("fentryseq,FSideEntrySeq,FISMULTICOLLECT,FACCTUNITQTY,FBASEUNITQTY)");
            stringBuilder.Append(" select v.FVoucherID,e.FENTRYID,e.FEXPLANATION,e.FACCOUNTID,e.FDETAILID,");
            stringBuilder.Append("e.FAMOUNTFOR,e.FAMOUNT,e.fdebit,e.fcredit,");
            stringBuilder.Append("e.FCURRENCYID,e.FEXCHANGERATETYPE,e.FEXCHANGERATE,e.FDC,");
            stringBuilder.Append("e.FMEASUREUNITID,e.FUNITPRICE,e.FQUANTITY,e.FSettleTypeID,e.FSettleNo,");
            stringBuilder.Append("e.fentryseq,e.FSideEntrySeq,e.FISMULTICOLLECT,");
            stringBuilder.Append("e.FACCTUNITQTY,e.FBASEUNITQTY ");
            stringBuilder.Append(" From T_GL_Voucher v inner join T_GL_VoucherEntry e on v.FVoucherID=e.FVoucherID");
            stringBuilder.AppendFormat(" where v.FACCOUNTBOOKID={0} ", queryVoucherEntity.AcctBook);
            if (queryVoucherEntity.UsePeriod && (queryVoucherEntity.StartYear != 0 || queryVoucherEntity.EndYear != 0 || queryVoucherEntity.StartPeriod != 0 || queryVoucherEntity.EndPeriod != 0))
            {
                stringBuilder.Append(" and " + ReturnPeriodString(queryVoucherEntity.StartYear, queryVoucherEntity.EndYear, queryVoucherEntity.StartPeriod, queryVoucherEntity.EndPeriod));
            }
            if (queryVoucherEntity.UseDate && (queryVoucherEntity.StartDate != DateTime.MinValue || queryVoucherEntity.EndDate != DateTime.MinValue))
            {
                stringBuilder.Append(" and " + ReturnDateString(queryVoucherEntity.StartDate, queryVoucherEntity.EndDate));
            }
            if (!queryVoucherEntity.IncludeUnPost)
            {
                stringBuilder.AppendLine(" and v.FPosted='1' ");
            }
            return stringBuilder;
        }

        public static List<long> GetAcctIdFromFlexEntry(Context ctx, List<string> lstFlexNumbers)
        {
            string strSQL = "SELECT DISTINCT FACCTID FROM T_BD_ACCOUNTFLEXENTRY WHERE FDATAFIELDNAME IN (@FDATAFIELDNAME)\r\n                                        GROUP BY FACCTID HAVING COUNT(1)=@FLEXNUMBERSCOUNT ";
            List<SqlParam> list = new List<SqlParam>();
            list.Add(new SqlParam("@FDATAFIELDNAME", KDDbType.String, string.Join("','", lstFlexNumbers)));
            list.Add(new SqlParam("@FLEXNUMBERSCOUNT", KDDbType.Int32, lstFlexNumbers.Count));
            List<long> list2 = new List<long>();
            using IDataReader dataReader = DBUtils.ExecuteReader(ctx, strSQL, list);
            while (dataReader.Read())
            {
                list2.Add(Convert.ToInt32(dataReader["FACCTID"]));
            }
            return list2;
        }

        public static string ReturnAccountSqlString(string balance, string alias = "a")
        {
            List<string> list = new List<string>();
            string[] array = (from n in balance.Split(',')
                              where !string.IsNullOrWhiteSpace(n)
                              select n).ToArray();
            string[] array2 = array;
            foreach (string arg in array2)
            {
                list.Add(string.Format(" {0}.FNUMBER = '{1}' or {0}.fnumber like '{1}.%' ", alias, arg));
            }
            return string.Format("({0})", string.Join(" or ", list));
        }

        public static string ReturnAccountCombinationSqlString(string balance, string alias = "b")
        {
            List<string> list = new List<string>();
            string[] array = (from n in balance.Split(',')
                              where !string.IsNullOrWhiteSpace(n)
                              select n).ToArray();
            string[] array2 = array;
            foreach (string arg in array2)
            {
                list.Add(string.Format(" {0}.FNUMBER = '{1}' or {0}.fnumber like '{1}.%' ", alias, arg));
            }
            return string.Format("({0})", string.Join(" or ", list));
        }

        public static void UpdateIdentityid(Context ctx, string strTempTable)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("SELECT (FROWCOUNT-FMAXIDENTITYID) AS FCOUNT FROM(SELECT COUNT(1) FROWCOUNT,ISNULL(MAX(FIDENTITYID),0) FMAXIDENTITYID FROM {0}) A", strTempTable);
            if (DBUtils.ExecuteScalar(ctx, stringBuilder.ToString(), 0) != 0)
            {
                stringBuilder.Clear();
                string tempTableName = CommonFunction.GetTempTableName(ctx);
                stringBuilder.AppendFormat("SELECT FIDENTITYID,ROW_NUMBER() OVER(ORDER BY FIDENTITYID) FROWID INTO {0} FROM {1}", tempTableName, strTempTable);
                DBUtils.Execute(ctx, stringBuilder.ToString());
                CreateFidentityIdIndex(ctx, tempTableName);
                stringBuilder.Clear();
                stringBuilder.AppendFormat("UPDATE {0} SET FIDENTITYID=(SELECT FROWID FROM {1} B WHERE {0}.FIDENTITYID=B.FIDENTITYID)", strTempTable, tempTableName);
                DBUtils.Execute(ctx, stringBuilder.ToString());
                CommonFunction.DropTempTable(ctx, tempTableName, immediate: true);
            }
        }

        private static void CreateFidentityIdIndex(Context ctx, string strTableName)
        {
            string strSQL = $"CREATE INDEX IDX_{strTableName.Substring(4)} ON {strTableName}(FIDENTITYID)";
            DBUtils.Execute(ctx, strSQL);
        }
    }
}

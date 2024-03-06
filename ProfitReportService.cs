using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.GroupElement;
using Kingdee.BOS.Core.Permission.Objects;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.Report.PivotReport;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.Core.Parameters;
using Kingdee.K3.FIN.GL.App.Core;
using Kingdee.K3.FIN.GL.App.Core.VoucherAdjust;
using Kingdee.K3.FIN.GL.Common.BusinessEntity;
using Kingdee.K3.FIN.GL.Common.Core;
using Kingdee.K3.FIN.GL.App.Report;


namespace ANY.HX.K3.Report
{
    [Description("ANY | 停-恒信销售利润报表")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ProfitReportService : SysReportBaseService
    {

        public const string Separator_FlexNoName = "  ";

        private int QtyPrecision;

        private DynamicObject accountBook;

        private int currencyId;

        private int intAcctSystemId;

        private int intAccountOrgId;

        private DateTime startDate;

        private DateTime endDate;

        private string acctNumber;

        private int acctLevel;

        private List<BaseDataTempTable> lstBaseDataTempTable;

        private int reportStyle;

        private bool useDetailGroup;

        private List<DetailGroup> detailGrp;

        private List<long> lstAcctIds;

        private List<long> lstBookAcctIds;

        private int flexType;

        private string strDetail = string.Empty;

        private bool noShowFlexNo;

        private string startFlexNo;

        private string endFlexNo;

        public Dictionary<string, DynamicObject> dicDetailSourceInfo = new Dictionary<string, DynamicObject>();

        private bool showDetailOnly;

        private bool showZero;

        private bool notPostVercher;

        private bool excludeAdjustVch;

        private bool noBalance;

        private bool bNotShowAcctNumber;

        private bool bShowNeverUseAcct;

        private bool bShowNeverUseFlex;

        private bool forbidAcct;

        private bool showAllAct;

        private int flexLevel;

        private bool reCalTotalRows;

        private string strFlexTmpTableName;

        private string flexFields;

        private long AcctTableID;

        private static GlRptConst GlRptConst = new GlRptConst();

        private bool bShowBalDC;

        private bool bShowColSB;

        private bool bShowColEB;

        private bool bShowColSQ;

        private bool bShowColEQ;

        private bool blEndBalDCByAcct;

        private bool blIsStartQty;

        private string strAcctFilterSql;

        private Dictionary<int, dynamic> dctAcctName;

        private bool IsByFlexScope = true;

        private string FlexNumber = string.Empty;

        private Dictionary<string, FlexItemPropertySubObject> dctFlexItemProperty;

        private bool bShowSubTotal = true;

        private bool isShowAnyFlex;

        private string[] curPeriodFields = new string[20]
        {
            "F_DR", "F_CR", "F_D", "F_C", "F_DQ", "F_CQ", "F_DYR", "F_CYR", "F_DY", "F_CY",
            "F_DYQ", "F_CYQ", "F_SDR", "F_SCR", "F_EDR", "F_ECR", "F_SQD", "F_SQC", "F_EQD", "F_EQC"
        };

        private Dictionary<int, string> dctCurrencyFormat = new Dictionary<int, string>();

        private HashSet<string> hsStdCurFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "F_SR", "F_DR", "F_CR", "F_ER", "F_DYR", "F_CYR", "F_SDR", "F_SCR", "F_EDR", "F_ECR" };

        private readonly string subTotal = ResManager.LoadKDString("小计", "003235000007174", SubSystemType.FIN);

        private readonly string total = ResManager.LoadKDString("总计", "003235000021049", SubSystemType.FIN);

        private Dictionary<string, DynamicObject> valueSources;

        private RptSystemParamInfor sysParm
        {
            get
            {
                if (bookId > 0)
                {
                    return ReportCommonFunction.GetSysParameter(base.Context, Convert.ToInt32(bookId));
                }
                return null;
            }
        }

        private int bookId
        {
            get
            {
                if (accountBook != null)
                {
                    return Convert.ToInt32(accountBook["Id"]);
                }
                return 0;
            }
        }

        private int bookCurrencyId
        {
            get
            {
                if (accountBook != null)
                {
                    return Convert.ToInt32(accountBook["Currency_Id"]);
                }
                return -1;
            }
        }

        private string[] flexFieldsArr => flexFields.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        public override void Initialize()
        {
            base.ReportProperty.IsGroupSummary = true;
            base.ReportProperty.BillKeyFieldName = "FdetailId";
            base.ReportProperty.ReportName = new LocaleValue(ResManager.LoadKDString("核算维度与科目组合表", "003235000018002", SubSystemType.FIN), base.Context.UserLocale.LCID);
            base.Initialize();
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            InitFlexItemPropertyInfo();
            DataTable currentReportData = GetCurrentReportData(base.Context, filter);
            CreateUpdateTempTable(base.Context, tableName, currentReportData);
            currentReportData.TableName = tableName;
            DBUtils.BulkInserts(base.Context, currentReportData);
            GetCurrencyFormat();
            Dictionary<string, string> dictionary = FlexAndName();
            string value = string.Empty;
            base.SettingInfo = new PivotReportSettingInfo();
            if (reCalTotalRows)
            {
                base.SettingInfo.IsShowGrandTotal = false;
            }
            if (reportStyle == 1)
            {
                TextField textField = new TextField();
                DecimalField fieldData = new DecimalField();
                foreach (DataColumn column in currentReportData.Columns)
                {
                    if (column.ColumnName.StartsWith("T_"))
                    {
                        textField = new TextField();
                        textField.Key = column.ColumnName;
                        textField.FieldName = column.ColumnName;
                        dictionary.TryGetValue(column.ColumnName.Substring(2), out value);
                        textField.Name = new LocaleValue(value, base.Context.UserLocale.LCID);
                        SettingField settingField = PivotReportSettingInfo.CreateColumnSettingField(textField, 0);
                        SetFieldTotalDragProperty(settingField, bShowSubTotal, reCalTotalRows);
                        base.SettingInfo.RowTitleFields.Add(settingField);
                        base.SettingInfo.SelectedFields.Add(settingField);
                    }
                    else if (column.ColumnName.StartsWith("F_") && column.ColumnName.EndsWith("Q"))
                    {
                        CreateFieldData("N" + QtyPrecision, fieldData, column);
                    }
                    else if (column.ColumnName.StartsWith("F_"))
                    {
                        CreateFieldData(fieldData, column);
                    }
                }
                textField = new TextField();
                textField.Key = "ACCTNAME";
                textField.FieldName = "ACCTNAME";
                textField.Name = new LocaleValue(ResManager.LoadKDString("科目名称", "003235000007099", SubSystemType.FIN), base.Context.UserLocale.LCID);
                SettingField settingField2 = PivotReportSettingInfo.CreateColumnSettingField(textField, 0);
                SetFieldTotalDragProperty(settingField2, bShowSubTotal, reCalTotalRows);
                base.SettingInfo.ColTitleFields.Add(settingField2);
                base.SettingInfo.SelectedFields.Add(settingField2);
            }
            else if (reportStyle == 2)
            {
                TextField textField2 = new TextField();
                textField2.Key = "ACCTNAME";
                textField2.FieldName = "ACCTNAME";
                textField2.Name = new LocaleValue(ResManager.LoadKDString("科目名称", "003235000007099", SubSystemType.FIN), base.Context.UserLocale.LCID);
                SettingField settingField3 = PivotReportSettingInfo.CreateColumnSettingField(textField2, 0);
                SetFieldTotalDragProperty(settingField3, bShowSubTotal, reCalTotalRows);
                base.SettingInfo.RowTitleFields.Add(settingField3);
                base.SettingInfo.SelectedFields.Add(settingField3);
                DecimalField fieldData2 = new DecimalField();
                foreach (DataColumn column2 in currentReportData.Columns)
                {
                    if (column2.ColumnName.StartsWith("T_"))
                    {
                        textField2 = new TextField();
                        textField2.Key = column2.ColumnName;
                        textField2.FieldName = column2.ColumnName;
                        dictionary.TryGetValue(column2.ColumnName.Substring(2), out value);
                        textField2.Name = new LocaleValue(value, base.Context.UserLocale.LCID);
                        SettingField settingField4 = PivotReportSettingInfo.CreateColumnSettingField(textField2, 0);
                        SetFieldTotalDragProperty(settingField4, bShowSubTotal, reCalTotalRows);
                        base.SettingInfo.ColTitleFields.Add(settingField4);
                        base.SettingInfo.SelectedFields.Add(settingField4);
                    }
                    else if (column2.ColumnName.StartsWith("F_") && column2.ColumnName.EndsWith("Q"))
                    {
                        CreateFieldData("N" + QtyPrecision, fieldData2, column2);
                    }
                    else if (column2.ColumnName.StartsWith("F_"))
                    {
                        CreateFieldData(fieldData2, column2);
                    }
                }
            }
            else if (reportStyle == 3)
            {
                TextField textField3 = new TextField();
                DecimalField fieldData3 = new DecimalField();
                string text = string.Empty;
                foreach (DataColumn column3 in currentReportData.Columns)
                {
                    if (column3.ColumnName.StartsWith("T_") && string.IsNullOrWhiteSpace(text))
                    {
                        text = column3.ColumnName;
                    }
                    if (column3.ColumnName.StartsWith("T_") && text != column3.ColumnName)
                    {
                        textField3 = new TextField();
                        textField3.Key = column3.ColumnName;
                        textField3.FieldName = column3.ColumnName;
                        dictionary.TryGetValue(column3.ColumnName.Substring(2), out value);
                        textField3.Name = new LocaleValue(value, base.Context.UserLocale.LCID);
                        SettingField settingField5 = PivotReportSettingInfo.CreateColumnSettingField(textField3, 0);
                        SetFieldTotalDragProperty(settingField5, bShowSubTotal, reCalTotalRows);
                        base.SettingInfo.RowTitleFields.Add(settingField5);
                        base.SettingInfo.SelectedFields.Add(settingField5);
                    }
                    else if (column3.ColumnName.StartsWith("F_") && column3.ColumnName.EndsWith("Q"))
                    {
                        CreateFieldData("N" + QtyPrecision, fieldData3, column3);
                    }
                    else if (column3.ColumnName.StartsWith("F_"))
                    {
                        CreateFieldData(fieldData3, column3);
                    }
                }
                textField3 = new TextField();
                textField3.Key = "ACCTNAME";
                textField3.FieldName = "ACCTNAME";
                textField3.Name = new LocaleValue(ResManager.LoadKDString("科目名称", "003235000007099", SubSystemType.FIN), base.Context.UserLocale.LCID);
                SettingField settingField6 = PivotReportSettingInfo.CreateColumnSettingField(textField3, 0);
                SetFieldTotalDragProperty(settingField6, bShowSubTotal, reCalTotalRows);
                base.SettingInfo.ColTitleFields.Add(settingField6);
                base.SettingInfo.SelectedFields.Add(settingField6);
                foreach (DataColumn column4 in currentReportData.Columns)
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        text = column4.ColumnName;
                    }
                    if (column4.ColumnName.StartsWith("T_") && text == column4.ColumnName)
                    {
                        textField3 = new TextField();
                        textField3.Key = column4.ColumnName;
                        textField3.FieldName = column4.ColumnName;
                        dictionary.TryGetValue(column4.ColumnName.Substring(2), out value);
                        textField3.Name = new LocaleValue(value, base.Context.UserLocale.LCID);
                        SettingField item = PivotReportSettingInfo.CreateColumnSettingField(textField3, 1);
                        SetFieldTotalDragProperty(settingField6, bShowSubTotal, reCalTotalRows);
                        base.SettingInfo.ColTitleFields.Add(item);
                        base.SettingInfo.SelectedFields.Add(item);
                    }
                }
            }
            currentReportData.Clear();
            currentReportData.Dispose();
            currentReportData = null;
            dctAcctName.Clear();
            dctAcctName = null;
        }

        private void CreateFieldData(string currencyFormat, DecimalField fieldData, DataColumn col)
        {
            fieldData = new DecimalField();
            fieldData.Key = col.ColumnName;
            fieldData.FieldName = col.ColumnName;
            fieldData.Name = new LocaleValue(GetTitleName(col.ColumnName));
            SettingField settingField = PivotReportSettingInfo.CreateDataSettingField(fieldData, 0, GroupSumType.Sum, currencyFormat);
            if (reCalTotalRows)
            {
                settingField.IsAllowDrag = false;
            }
            base.SettingInfo.AggregateFields.Add(settingField);
            base.SettingInfo.SelectedFields.Add(settingField);
        }

        private void CreateFieldData(DecimalField fieldData, DataColumn col)
        {
            string value = string.Empty;
            if (currencyId == 0 || currencyId == bookCurrencyId || !hsStdCurFields.Contains(col.ColumnName))
            {
                dctCurrencyFormat.TryGetValue(bookCurrencyId, out value);
            }
            else
            {
                dctCurrencyFormat.TryGetValue(currencyId, out value);
            }
            if (!value.IsNullOrEmptyOrWhiteSpace())
            {
                CreateFieldData(value, fieldData, col);
            }
        }

        private string GetTitleName(string title)
        {
            return title.Split(new char[1] { '_' }, StringSplitOptions.RemoveEmptyEntries)[1].ToUpper() switch
            {
                "S" => ResManager.LoadKDString("起始日期余额", "003235000018003", SubSystemType.FIN),
                "D" => ResManager.LoadKDString("借方发生", "003235000018004", SubSystemType.FIN),
                "C" => ResManager.LoadKDString("贷方发生", "003235000018005", SubSystemType.FIN),
                "E" => ResManager.LoadKDString("截止日期余额", "003235000018006", SubSystemType.FIN),
                "DY" => ResManager.LoadKDString("本年累计借方发生", "003235000018007", SubSystemType.FIN),
                "CY" => ResManager.LoadKDString("本年累计贷方发生", "003235000018008", SubSystemType.FIN),
                "SR" => ResManager.LoadKDString("起始日期余额(原币)", "003235000018009", SubSystemType.FIN),
                "DR" => ResManager.LoadKDString("借方发生(原币)", "003235000018010", SubSystemType.FIN),
                "CR" => ResManager.LoadKDString("贷方发生(原币)", "003235000018011", SubSystemType.FIN),
                "ER" => ResManager.LoadKDString("截止日期余额(原币)", "003235000018012", SubSystemType.FIN),
                "DYR" => ResManager.LoadKDString("本年累计借方发生(原币)", "003235000018013", SubSystemType.FIN),
                "CYR" => ResManager.LoadKDString("本年累计贷方发生(原币)", "003235000018014", SubSystemType.FIN),
                "SQ" => ResManager.LoadKDString("起始日期数量", "003235000018015", SubSystemType.FIN),
                "DQ" => ResManager.LoadKDString("借方数量", "003235000018016", SubSystemType.FIN),
                "CQ" => ResManager.LoadKDString("贷方数量", "003235000018017", SubSystemType.FIN),
                "EQ" => ResManager.LoadKDString("截止日期数量", "003235000018018", SubSystemType.FIN),
                "DYQ" => ResManager.LoadKDString("本年累计借方数量", "003235000018019", SubSystemType.FIN),
                "CYQ" => ResManager.LoadKDString("本年累计贷方数量", "003235000018020", SubSystemType.FIN),
                "SDR" => ResManager.LoadKDString("起始日期余额（原币-借方）", "003235000032177", SubSystemType.FIN),
                "SCR" => ResManager.LoadKDString("起始日期余额（原币-贷方）", "003235000032178", SubSystemType.FIN),
                "EDR" => ResManager.LoadKDString("截止日期余额（原币-借方）", "003235000032179", SubSystemType.FIN),
                "ECR" => ResManager.LoadKDString("截止日期余额（原币-贷方）", "003235000032180", SubSystemType.FIN),
                "SD" => ResManager.LoadKDString("起始日期余额（借方）", "003235000032181", SubSystemType.FIN),
                "SC" => ResManager.LoadKDString("起始日期余额（贷方）", "003235000032182", SubSystemType.FIN),
                "ED" => ResManager.LoadKDString("截止日期余额（借方）", "003235000032183", SubSystemType.FIN),
                "EC" => ResManager.LoadKDString("截止日期余额（贷方）", "003235000032184", SubSystemType.FIN),
                "SQD" => ResManager.LoadKDString("起始日期数量（借方）", "003235000032185", SubSystemType.FIN),
                "SQC" => ResManager.LoadKDString("起始日期数量（贷方）", "003235000032186", SubSystemType.FIN),
                "EQD" => ResManager.LoadKDString("截止日期数量（借方）", "003235000032187", SubSystemType.FIN),
                "EQC" => ResManager.LoadKDString("截止日期数量（贷方）", "003235000032188", SubSystemType.FIN),
                _ => "",
            };
        }

        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles reportTitles = new ReportTitles();
            reportTitles.AddTitle("FACCTBOOKID", accountBook["Name"].ToString());
            string sTitleValue = startDate.ToString("yyyy-MM-dd") + " " + ResManager.LoadKDString("至", "003235000018021", SubSystemType.FIN) + " " + endDate.ToString("yyyy-MM-dd");
            reportTitles.AddTitle("FTTITLEDATE", sTitleValue);
            reportTitles.AddTitle("FCURRENCYID", GetCurrencyName(currencyId));
            return reportTitles;
        }

        public override void CloseReport()
        {
            CommonFunction.DropTempTable(base.Context, strFlexTmpTableName, immediate: true);
            base.CloseReport();
        }

        private Dictionary<string, string> FlexAndName()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string strSQL = "SELECT Y.FFLEXNUMBER,L.FNAME FROM T_BD_FLEXITEMPROPERTY Y LEFT JOIN T_BD_FLEXITEMPROPERTY_L L ON Y.FID=L.FID AND L.FLOCALEID=@FLOCALEID\r\n                                            WHERE ISNULL(Y.FFLEXNUMBER,' ')<>' ' ";
            using IDataReader dataReader = DBUtils.ExecuteReader(base.Context, strSQL, new SqlParam("@FLOCALEID", KDDbType.Int32, base.Context.UserLocale.LCID));
            while (dataReader.Read())
            {
                dictionary.Add(dataReader["FFLEXNUMBER"].ToString(), dataReader["FNAME"].ToString());
            }
            dataReader.Close();
            return dictionary;
        }

        private void CreateUpdateTempTable(Context ctx, string tableNameTemp, DataTable dTable)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("create table {0} (", tableNameTemp);
            stringBuilder.Append("FlexName nvarchar(300),");
            stringBuilder.Append("detailId varchar(100),");
            stringBuilder.Append("AcctName nvarchar(500),");
            for (int i = 0; i < dTable.Columns.Count; i++)
            {
                if (dTable.Columns[i].ColumnName.StartsWith("F_"))
                {
                    stringBuilder.AppendFormat("{0} decimal(28,10),", dTable.Columns[i].ColumnName);
                }
                else if (dTable.Columns[i].ColumnName.StartsWith("T_"))
                {
                    stringBuilder.AppendFormat("{0} nvarchar(300),", dTable.Columns[i].ColumnName);
                    stringBuilder.AppendFormat("{0} int,", dTable.Columns[i].ColumnName.Remove(0, 2) + "_FFLEXLEVEL");
                    stringBuilder.AppendFormat("{0} nvarchar(300),", dTable.Columns[i].ColumnName.Remove(0, 2) + "_FFULLPARENTID");
                }
            }
            stringBuilder.Append("FIDENTITYID int)");
            DBUtils.Execute(ctx, stringBuilder.ToString());
        }

        private void InitFilter(DynamicObject param)
        {
            accountBook = param["ACCTBOOKID"] as DynamicObject;
            currencyId = Convert.ToInt32(param["CURRENCYID"]);
            intAccountOrgId = Convert.ToInt32(accountBook["AccountOrgID_Id"]);
            intAcctSystemId = Convert.ToInt32(accountBook["AcctSystemID_Id"]);
            startDate = Convert.ToDateTime(param["STARTDATE"]);
            endDate = Convert.ToDateTime(param["ENDDATE"]);
            acctNumber = param["BALANCE"] as string;
            acctLevel = Convert.ToInt32(param["BALANCELEVEL"]);
            reportStyle = Convert.ToInt32(param["REPORTSTYLE"]);
            flexType = Convert.ToInt32(param["DETAIL_Id"]);
            noShowFlexNo = Convert.ToBoolean(param["NOSHOWFLEXNO"]);
            showDetailOnly = Convert.ToBoolean(param["SHOWDETAILONLY"]);
            showZero = Convert.ToBoolean(param["SHOWZERO"]);
            bShowNeverUseAcct = Convert.ToBoolean(param["SHOWNEVERUSEACCT"]);
            bShowNeverUseFlex = Convert.ToBoolean(param["SHOWNEVERUSEFLEX"]);
            notPostVercher = Convert.ToBoolean(param["NOTPOSTVOUCHER"]);
            excludeAdjustVch = Convert.ToBoolean(param["EXCLUDEADJUSTVCH"]);
            noBalance = Convert.ToBoolean(param["NOBALANCE"]);
            bNotShowAcctNumber = Convert.ToBoolean(param["SHOWBALANCE"]);
            forbidAcct = Convert.ToBoolean(param["FORBIDBALANCE"]);
            showAllAct = Convert.ToBoolean(param["SHOWALLACT"]);
            flexLevel = param.GetValue("FLEXLEVEL", 0);
            reCalTotalRows = param.GetValue("RECALTOTALROWS", defValue: false);
            bShowSubTotal = !param.GetValue("NoShowSubTotal", defValue: false);
            isShowAnyFlex = Convert.ToBoolean(param["ISSHOWANYFLEXACCT"]);
            bShowBalDC = Convert.ToBoolean(param["SHOWBALDEBITANDCREDIT"]);
            AcctTableID = ReportCommonFunction.GetAccountBookInfor(base.Context, Convert.ToInt64(accountBook["Id"])).AccountTableID;
            if (Convert.ToString(param["AcctDemRadioGrp"]).Equals("0"))
            {
                IsByFlexScope = true;
                FlexNumber = string.Empty;
                startFlexNo = Convert.ToString(param["STARTFLEXNO"]);
                endFlexNo = Convert.ToString(param["ENDFLEXNO"]);
            }
            else
            {
                IsByFlexScope = false;
                startFlexNo = string.Empty;
                endFlexNo = string.Empty;
                FlexNumber = Convert.ToString(param["ACCTDEM"]);
            }
            useDetailGroup = Convert.ToBoolean(param["USEDETAILGROUP"]);
            if (useDetailGroup)
            {
                detailGrp = SetMutilFlexGroupInfo(ObjectSerialize.Desrialize(Convert.ToString(param["DETAILGRP"]), typeof(List<DetailGroup>)) as List<DetailGroup>);
            }
            else
            {
                DynamicObject dynamicObject = param["DETAIL"] as DynamicObject;
                strDetail = Convert.ToString(dynamicObject["ValueSource_Id"]);
                detailGrp = SetSingleFlexGroupInfo(null, strDetail);
            }
            BuildFlexItemTableObject();
            SetAcctId();
            strFlexTmpTableName = (isShowAnyFlex ? GetAnyFlexJoinSql(out flexFields) : GetNeverUsedFlexJoinSql(out flexFields));
            CommonService commonService = new CommonService();
            object paramter = commonService.GetParamter(base.Context, Convert.ToInt64(accountBook["Id"]), "GL_SystemParameter", "SameDirectory");
            if (paramter != null)
            {
                bool.TryParse(paramter.ToString(), out blEndBalDCByAcct);
            }
            lstBaseDataTempTable = CommonFunction.GetDataRuleFilter(base.Context, "GL_CRPT_FlexAccount", "BD_Account", Convert.ToInt64(accountBook["Id"]));
            SetIsStartAcctQty();
        }

        private IEnumerable<dynamic> FilterRows(IEnumerable<DataRow> rows, DateTime begin, DateTime end)
        {
            IEnumerable<DataRow> enumerable = rows.Where(delegate (DataRow r)
            {
                DateTime field = r.GetField<DateTime>("FDATE");
                return field >= begin && field < end;
            });
            if (enumerable == null || enumerable.Count() == 0)
            {
                return new List<object>();
            }
            return from r in enumerable
                   group r by GetDataTableKey(r) into g
                   select new
                   {
                       Key = g.Key,
                       Debit = g.Sum((DataRow r) => r.GetField<decimal>("FDEBIT")),
                       DebitFor = g.Sum((DataRow r) => r.GetField<decimal>("FDEBITFOR")),
                       Credit = g.Sum((DataRow r) => r.GetField<decimal>("FCREDIT")),
                       CreditFor = g.Sum((DataRow r) => r.GetField<decimal>("FCREDITFOR")),
                       DebitQty = g.Sum((DataRow r) => r.GetField<decimal>("FDEBITQTY")),
                       CreditQty = g.Sum((DataRow r) => r.GetField<decimal>("FCREDITQTY"))
                   };
        }

        private string GetDataTableKey(DataRow r)
        {
            List<string> list = new List<string>();
            list.Add(r["FACCOUNTID"].ToString());
            list.Add(r["FLEVEL"].ToString());
            list.Add(r["FPARENTID"].ToString());
            list.Add(r["FDETAILID"].ToString());
            string[] array = flexFieldsArr;
            foreach (string columnName in array)
            {
                list.Add(r[columnName].ToString());
            }
            return string.Join("_", list);
        }

        private string GetFlexGrpKey(DataRow r)
        {
            List<string> list = new List<string>();
            string[] array = flexFieldsArr;
            foreach (string columnName in array)
            {
                list.Add(r[columnName].ToString());
            }
            return string.Join(GlRptConst.StrDetailSplitTag, list);
        }

        private string GetFlexGrpKeyVal(DataRow r)
        {
            List<string> list = new List<string>();
            string[] array = flexFieldsArr;
            foreach (string text in array)
            {
                list.Add($"{text};{r[text].ToString()}");
            }
            return string.Join(GlRptConst.StrDetailSplitTag, list);
        }

        private string GetFlexGrpKeyVal(DataRow r, string ignoreFlexNumber)
        {
            List<string> list = new List<string>();
            string[] array = flexFieldsArr;
            foreach (string text in array)
            {
                if (!text.StartsWith(ignoreFlexNumber))
                {
                    list.Add($"{text};{r[text].ToString()}");
                }
            }
            return string.Join(GlRptConst.StrDetailSplitTag, list);
        }

        private string GetFlexGrpKeyVal(DataRow r, int maxIndex)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < maxIndex && i < detailGrp.Count; i++)
            {
                DetailGroup g = detailGrp[i];
                IEnumerable<string> enumerable = flexFieldsArr.Where((string field) => field.StartsWith(g.FlexNumber));
                foreach (string item in enumerable)
                {
                    list.Add($"{item};{r[item].ToString()}");
                }
            }
            return string.Join(GlRptConst.StrDetailSplitTag, list);
        }

        private string GetFlexGrpKeyValForNewStyle(DataRow r)
        {
            List<string> list = new List<string>();
            for (int i = 1; i < detailGrp.Count; i++)
            {
                DetailGroup g = detailGrp[i];
                IEnumerable<string> enumerable = flexFieldsArr.Where((string field) => field.StartsWith(g.FlexNumber));
                foreach (string item in enumerable)
                {
                    list.Add($"{item};{r[item].ToString()}");
                }
            }
            return string.Join(GlRptConst.StrDetailSplitTag, list);
        }

        private dynamic GetBalRowDynamic(DataRow balRow)
        {
            return new
            {
                Year = balRow["FYEAR"],
                BeginBalance = balRow["FBEGINBALANCE"],
                BeginBalanceFor = balRow["FBEGINBALANCEFOR"],
                YtdDebit = balRow["FYTDDEBIT"],
                YtdDebitFor = balRow["FYTDDEBITFOR"],
                YtdCredit = balRow["FYTDCREDIT"],
                YtdCreditFor = balRow["FYTDCREDITFOR"],
                BeginQty = balRow["FBeginQty"],
                DebitQty = balRow["FDebitQty"],
                CreditQty = balRow["FCreditQty"],
                YtdDebitQty = balRow["FYtdDebitQty"],
                YtdCreditQty = balRow["FYtdCreditQty"]
            };
        }

        private DataTable GetCurrentReportData(Context ctx, IRptParams filter)
        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            InitFilter(customFilter);
            InitFilterShowColumnInfo(filter);
            DateTime startMonthDate;
            DataTable balance = GetBalance(out startMonthDate);
            DataTable voucherData = GetVoucherData(startMonthDate);
            DateTime dateTime = startDate;
            int iDetailid = 0;
            DataRow dataRow = balance.NewRow();
            if (balance.Rows.Count > 0)
            {
                iDetailid = balance.AsEnumerable().Max((DataRow f) => f.GetField<int>("FDETAILID"));
                dataRow = balance.AsEnumerable().Where(delegate (DataRow r)
                {
                    int field = r.GetField<int>("FDETAILID");
                    return field == iDetailid;
                }).FirstOrDefault();
            }
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (DataRow row in balance.Rows)
            {
                if (bShowNeverUseAcct && bShowNeverUseFlex && Convert.ToInt32(row["FDETAILID"]) == 2)
                {
                    if (string.IsNullOrWhiteSpace(Convert.ToString(row["FACCOUNTID"])))
                    {
                        row["FDETAILID"] = iDetailid + 1;
                        row["FACCOUNTID"] = dataRow["FACCOUNTID"];
                        row["FLEVEL"] = dataRow["FLEVEL"];
                        row["FPARENTID"] = dataRow["FPARENTID"];
                        row["FCURRENCYID"] = dataRow["FCURRENCYID"];
                        row["FNUMBER"] = dataRow["FNUMBER"];
                    }
                    string columnName = flexFieldsArr.Where((string c) => c.EndsWith("_FNAME")).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(Convert.ToString(row[columnName])))
                    {
                        string[] array = flexFieldsArr;
                        foreach (string columnName2 in array)
                        {
                            row[columnName2] = dataRow[columnName2];
                            row["FDETAILID"] = iDetailid;
                        }
                    }
                }
                else if (bShowNeverUseAcct && Convert.ToInt32(row["FDETAILID"]) == 2)
                {
                    row["FDETAILID"] = iDetailid;
                    string[] array2 = flexFieldsArr;
                    foreach (string columnName3 in array2)
                    {
                        row[columnName3] = dataRow[columnName3];
                    }
                }
                else if (bShowNeverUseFlex && Convert.ToInt32(row["FDETAILID"]) == 2)
                {
                    row["FDETAILID"] = iDetailid + 1;
                    row["FACCOUNTID"] = dataRow["FACCOUNTID"];
                    row["FLEVEL"] = dataRow["FLEVEL"];
                    row["FPARENTID"] = dataRow["FPARENTID"];
                    row["FCURRENCYID"] = dataRow["FCURRENCYID"];
                    row["FNUMBER"] = dataRow["FNUMBER"];
                }
                string dataTableKey = GetDataTableKey(row);
                dictionary[dataTableKey] = 0;
            }
            foreach (DataRow row2 in voucherData.Rows)
            {
                string dataTableKey2 = GetDataTableKey(row2);
                if (dictionary.ContainsKey(dataTableKey2))
                {
                    continue;
                }
                dictionary[dataTableKey2] = 0;
                DataRow dataRow4 = balance.NewRow();
                foreach (DataColumn column in balance.Columns)
                {
                    if (voucherData.Columns.Contains(column.ColumnName))
                    {
                        dataRow4[column.ColumnName] = row2[column.ColumnName];
                    }
                }
                dataRow4["FYEAR"] = 9999;
                dataRow4["FPERIOD"] = 99;
                dataRow4["FCURRENCYID"] = 0;
                dataRow4["FBEGINBALANCE"] = 0;
                dataRow4["FBEGINBALANCEFOR"] = 0;
                dataRow4["FYTDDEBIT"] = 0;
                dataRow4["FYTDDEBITFOR"] = 0;
                dataRow4["FYTDCREDIT"] = 0;
                dataRow4["FYTDCREDITFOR"] = 0;
                dataRow4["FBeginQty"] = 0;
                dataRow4["FDebitQty"] = 0;
                dataRow4["FCreditQty"] = 0;
                dataRow4["FYtdDebitQty"] = 0;
                dataRow4["FYtdCreditQty"] = 0;
                balance.Rows.Add(dataRow4);
            }
            EnumerableRowCollection<DataRow> rows = voucherData.AsEnumerable();
            IEnumerable<object> inner = FilterRows(rows, startMonthDate, dateTime);
            IEnumerable<object> inner2 = FilterRows(rows, dateTime, endDate.AddDays(1.0));
            Tuple<int, int> yearPeriodByDate = CommonFunction.GetYearPeriodByDate(base.Context, bookId, startMonthDate);
            Tuple<int, int> yearPeriodByDate2 = CommonFunction.GetYearPeriodByDate(base.Context, bookId, endDate);
            DateTime begin = startMonthDate;
            DateTime dateTime2 = endDate;
            if (yearPeriodByDate.Item1 != yearPeriodByDate2.Item1)
            {
                CommonFunction.GetDateByYearPeriod(ctx, bookId, yearPeriodByDate2.Item1, 1, out begin, out dateTime2);
            }
            IEnumerable<object> inner3 = FilterRows(rows, begin, endDate.AddDays(1.0));
            IEnumerable<object> inner4 = FilterRows(rows, startMonthDate, endDate.AddDays(1.0));
            var enumerable = new List<object>();
            Hashtable hashtable = new Hashtable();
            if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    hashtable.Add(item.ToString(), item);
                }
            }
            Dictionary<int, object> allAcctDct = GetAllAcctDct();
            DataTable dataTable = GetTableData(balance, hashtable, allAcctDct);
            voucherData.Clear();
            voucherData.Dispose();
            voucherData = null;
            hashtable.Clear();
            hashtable = null;
            enumerable = null;
            if (flexLevel > 0)
            {
                SumParentGroup(dataTable);
                RemoveRowIfMatchCondition(dataTable, delegate (DataRow r)
                {
                    int num2 = Convert.ToInt32(r["detailId"]);
                    bool flag2 = false;
                    foreach (DetailGroup item2 in detailGrp)
                    {
                        int num3 = 0;
                        if (r[$"{item2.FlexNumber}_FFLEXLEVEL"] != DBNull.Value)
                        {
                            num3 = Convert.ToInt16(r[$"{item2.FlexNumber}_FFLEXLEVEL"]);
                        }
                        flag2 = false;
                        string columnName4 = $"{item2.FlexNumber}_FGROUPID";
                        if (!(r[columnName4] is DBNull) && Convert.ToInt32(r[columnName4]) != 0)
                        {
                            if (num3 > flexLevel)
                            {
                                flag2 = true;
                            }
                            else if (num3 < flexLevel && num2 == 0)
                            {
                                flag2 = true;
                            }
                            if (flag2)
                            {
                                return flag2;
                            }
                        }
                    }
                    return flag2;
                });
            }
            foreach (DataRow row3 in dataTable.Rows)
            {
                row3["RowType"] = 1;
            }
            if (bShowBalDC && !blEndBalDCByAcct)
            {
                dataTable = SumDataByFlexField(dataTable);
            }
            SumParentAcct(dataTable);
            DelZerosByFlexAndAcct(dataTable, (DataRow r) => GetDelSumGroupKey(r));
            List<string> value = (from p in dataTable.AsEnumerable()
                                  select p.Field<string>("AcctName").Split(' ')[0]).Distinct().ToList();
            filter.FilterParameter.CustomOption.Remove("acctnumber");
            filter.FilterParameter.CustomOption.Add("acctnumber", value);
            RemoveRowIfMatchCondition(dataTable, delegate (DataRow r)
            {
                int num = Convert.ToInt16(r["FLEVEL"]);
                return (num > acctLevel) ? true : false;
            });
            if (reCalTotalRows && dataTable != null && dataTable.Rows.Count > 0)
            {
                if (bShowSubTotal)
                {
                    SumSubTotal(dataTable);
                }
                int idc = (from s in dataTable.AsEnumerable()
                           orderby s.Field<string>("AcctName")
                           select s into f
                           select f.Field<int>("DC")).FirstOrDefault();
                int iMaxAcctLevel = dataTable.AsEnumerable().Max((DataRow f) => f.Field<int>("FLEVEL"));
                if (reportStyle == 1 || reportStyle == 3)
                {
                    SumTotal(dataTable, idc, iMaxAcctLevel);
                    SumTotalV(dataTable, idc, iMaxAcctLevel);
                }
                else if (reportStyle == 2)
                {
                    SumTotalV(dataTable, idc, iMaxAcctLevel);
                    SumTotal(dataTable, idc, iMaxAcctLevel);
                }
            }
            foreach (DetailGroup item3 in detailGrp)
            {
                dataTable.Columns.Add(new DataColumn($"T_{item3.FlexNumber}", typeof(string)));
            }
            for (int l = 0; l < dataTable.Rows.Count; l++)
            {
                DataRow dataRow6 = dataTable.Rows[l];
                dataRow6["FIDENTITYID"] = l + 1;
                foreach (DetailGroup item4 in detailGrp)
                {
                    string text = (noShowFlexNo ? "" : Convert.ToString(dataRow6[$"{item4.FlexNumber}_FNUMBER"]));
                    string arg = Convert.ToString(dataRow6[$"{item4.FlexNumber}_FNAME"]);
                    bool flag = !noShowFlexNo && !string.IsNullOrWhiteSpace(text);
                    dataRow6[$"T_{item4.FlexNumber}"] = string.Format("{0}{1}{2}", text, flag ? "  " : "", arg);
                }
            }
            RemoveNoNeededColumn(dataTable);
            DealData(dataTable, filter);
            DealShowDCBalCol(dataTable, filter);
            return dataTable;
        }

        private DataTable GetTableData(DataTable balTable, Hashtable hash, Dictionary<int, dynamic> acctNames)
        {
            DataTable tableScheme = GetTableScheme();
            int num = 0;
            int firstAcctId = GetFirstAcctId(balTable);
            tableScheme.BeginLoadData();
            foreach (DataRow row in balTable.Rows)
            {
                num = Convert.ToInt32(row["FACCOUNTID"]);
                if (num == 0)
                {
                    num = firstAcctId;
                }
                string dataTableKey = GetDataTableKey(row);
                if (!hash.ContainsKey(dataTableKey))
                {
                    continue;
                }
                dynamic val = hash[dataTableKey];
                dynamic val2 = null;
                int num2 = 0;
                if (acctNames.ContainsKey(num))
                {
                    val2 = acctNames[num];
                    num2 = val2.DC;
                    if (QtyPrecision < val2.Precision)
                    {
                        QtyPrecision = val2.Precision;
                    }
                }
                dynamic balRowDynamic = GetBalRowDynamic(row);
                DataRow dataRow2 = tableScheme.NewRow();
                string[] array = flexFieldsArr;
                foreach (string columnName in array)
                {
                    dataRow2[columnName] = row[columnName];
                }
                dataRow2["detailId"] = row["FDETAILID"];
                dataRow2["AcctName"] = (object)((val2 == null) ? "" : val2.Name);
                dataRow2["DC"] = num2;
                dataRow2["FLEVEL"] = row["FLEVEL"];
                dataRow2["FPARENTID"] = row["FPARENTID"];
                dataRow2["F_SR"] = (object)(num2 * (balRowDynamic.BeginBalanceFor + val.BeginDebitFor - val.BeginCreditFor));
                dataRow2["F_ER"] = (object)(num2 * (balRowDynamic.BeginBalanceFor + val.TotDebitFor - val.TotCreditFor));
                dataRow2["F_S"] = (object)(num2 * (balRowDynamic.BeginBalance + val.BeginDebit - val.BeginCredit));
                dataRow2["F_E"] = (object)(num2 * (balRowDynamic.BeginBalance + val.TotDebit - val.TotCredit));
                dataRow2["F_SQ"] = (object)(num2 * (balRowDynamic.BeginQty + val.BeginDebitQty - val.BeginCreditQty));
                dataRow2["F_EQ"] = (object)(num2 * (balRowDynamic.BeginQty + val.TotDebitQty - val.TotCreditQty));
                AssembleNewRowDataByDC(val, num2, balRowDynamic, dataRow2);
                dataRow2["F_DR"] = (object)val.CurDebitFor;
                dataRow2["F_CR"] = (object)val.CurCreditFor;
                Tuple<int, int> yearPeriodByDate = CommonFunction.GetYearPeriodByDate(base.Context, bookId, endDate);
                if (Convert.ToInt16(balRowDynamic.Year) != yearPeriodByDate.Item1)
                {
                    dataRow2["F_DYR"] = (object)val.YtdDebitFor;
                    dataRow2["F_CYR"] = (object)val.YtdCreditFor;
                    dataRow2["F_DY"] = (object)val.YtdDebit;
                    dataRow2["F_CY"] = (object)val.YtdCredit;
                    dataRow2["F_DYQ"] = (object)val.YtdDebitQty;
                    dataRow2["F_CYQ"] = (object)val.YtdCreditQty;
                }
                else
                {
                    dataRow2["F_DYR"] = (object)(balRowDynamic.YtdDebitFor + val.YtdDebitFor);
                    dataRow2["F_CYR"] = (object)(balRowDynamic.YtdCreditFor + val.YtdCreditFor);
                    dataRow2["F_DY"] = (object)(balRowDynamic.YtdDebit + val.YtdDebit);
                    dataRow2["F_CY"] = (object)(balRowDynamic.YtdCredit + val.YtdCredit);
                    dataRow2["F_DYQ"] = (object)(balRowDynamic.YtdDebitQty + val.YtdDebitQty);
                    dataRow2["F_CYQ"] = (object)(balRowDynamic.YtdCreditQty + val.YtdCreditQty);
                }
                dataRow2["F_D"] = (object)val.CurDebit;
                dataRow2["F_C"] = (object)val.CurCredit;
                dataRow2["F_DQ"] = (object)val.CurDebitQty;
                dataRow2["F_CQ"] = (object)val.CurCreditQty;
                tableScheme.Rows.Add(dataRow2);
            }
            tableScheme.EndLoadData();
            return tableScheme;
        }

        private int GetFirstAcctId(DataTable balTable)
        {
            int num = 0;
            foreach (DataRow row in balTable.Rows)
            {
                if (Convert.ToInt32(row["FACCOUNTID"]) != 0)
                {
                    num = Convert.ToInt32(row["FACCOUNTID"]);
                    break;
                }
            }
            if (num == 0 && lstAcctIds != null && lstAcctIds.Count > 0)
            {
                num = Convert.ToInt32(lstAcctIds[0]);
            }
            return num;
        }

        private void AssembleNewRowDataByDC(dynamic row, int dc, dynamic value, DataRow newR)
        {
            if (blEndBalDCByAcct)
            {
                if (dc > 0)
                {
                    newR["F_SDR"] = (object)(dc * (value.BeginBalanceFor + row.BeginDebitFor - row.BeginCreditFor));
                    newR["F_EDR"] = (object)(dc * (value.BeginBalanceFor + row.TotDebitFor - row.TotCreditFor));
                    newR["F_SD"] = (object)(dc * (value.BeginBalance + row.BeginDebit - row.BeginCredit));
                    newR["F_ED"] = (object)(dc * (value.BeginBalance + row.TotDebit - row.TotCredit));
                    newR["F_SQD"] = (object)(dc * (value.BeginQty + row.BeginDebitQty - row.BeginCreditQty));
                    newR["F_EQD"] = (object)(dc * (value.BeginQty + row.TotDebitQty - row.TotCreditQty));
                    newR["F_SCR"] = 0;
                    newR["F_ECR"] = 0;
                    newR["F_SC"] = 0;
                    newR["F_EC"] = 0;
                    newR["F_SQC"] = 0;
                    newR["F_EQC"] = 0;
                }
                else
                {
                    newR["F_SCR"] = (object)(dc * (value.BeginBalanceFor + row.BeginDebitFor - row.BeginCreditFor));
                    newR["F_ECR"] = (object)(dc * (value.BeginBalanceFor + row.TotDebitFor - row.TotCreditFor));
                    newR["F_SC"] = (object)(dc * (value.BeginBalance + row.BeginDebit - row.BeginCredit));
                    newR["F_EC"] = (object)(dc * (value.BeginBalance + row.TotDebit - row.TotCredit));
                    newR["F_SQC"] = (object)(dc * (value.BeginQty + row.BeginDebitQty - row.BeginCreditQty));
                    newR["F_EQC"] = (object)(dc * (value.BeginQty + row.TotDebitQty - row.TotCreditQty));
                    newR["F_SDR"] = 0;
                    newR["F_EDR"] = 0;
                    newR["F_SD"] = 0;
                    newR["F_ED"] = 0;
                    newR["F_SQD"] = 0;
                    newR["F_EQD"] = 0;
                }
                return;
            }
            if (value.BeginBalanceFor + row.BeginDebitFor - row.BeginCreditFor > 0)
            {
                newR["F_SDR"] = (object)(value.BeginBalanceFor + row.BeginDebitFor - row.BeginCreditFor);
                newR["F_SCR"] = 0;
            }
            else
            {
                newR["F_SDR"] = 0;
                newR["F_SCR"] = (object)(-1 * (value.BeginBalanceFor + row.BeginDebitFor - row.BeginCreditFor));
            }
            if (value.BeginBalanceFor + row.TotDebitFor - row.TotCreditFor > 0)
            {
                newR["F_EDR"] = (object)(value.BeginBalanceFor + row.TotDebitFor - row.TotCreditFor);
                newR["F_ECR"] = 0;
            }
            else
            {
                newR["F_EDR"] = 0;
                newR["F_ECR"] = (object)(-1 * (value.BeginBalanceFor + row.TotDebitFor - row.TotCreditFor));
            }
            if (value.BeginBalance + row.BeginDebit - row.BeginCredit > 0)
            {
                newR["F_SD"] = (object)(value.BeginBalance + row.BeginDebit - row.BeginCredit);
                newR["F_SC"] = 0;
            }
            else
            {
                newR["F_SD"] = 0;
                newR["F_SC"] = (object)(-1 * (value.BeginBalance + row.BeginDebit - row.BeginCredit));
            }
            if (value.BeginBalance + row.TotDebit - row.TotCredit > 0)
            {
                newR["F_ED"] = (object)(value.BeginBalance + row.TotDebit - row.TotCredit);
                newR["F_EC"] = 0;
            }
            else
            {
                newR["F_ED"] = 0;
                newR["F_EC"] = (object)(-1 * (value.BeginBalance + row.TotDebit - row.TotCredit));
            }
            if (value.BeginQty + row.BeginDebitQty - row.BeginCreditQty > 0)
            {
                newR["F_SQD"] = (object)(value.BeginQty + row.BeginDebitQty - row.BeginCreditQty);
                newR["F_SQC"] = 0;
            }
            else
            {
                newR["F_SQD"] = 0;
                newR["F_SQC"] = (object)(-1 * (value.BeginQty + row.BeginDebitQty - row.BeginCreditQty));
            }
            if (value.BeginQty + row.TotDebitQty - row.TotCreditQty > 0)
            {
                newR["F_EQD"] = (object)(value.BeginQty + row.TotDebitQty - row.TotCreditQty);
                newR["F_EQC"] = 0;
            }
            else
            {
                newR["F_EQD"] = 0;
                newR["F_EQC"] = (object)(-1 * (value.BeginQty + row.TotDebitQty - row.TotCreditQty));
            }
        }

        private void InitFilterShowColumnInfo(IRptParams filter)
        {
            if (bShowBalDC)
            {
                List<ColumnField> columnInfo = filter.FilterParameter.ColumnInfo;
                bShowColSB = columnInfo.FirstOrDefault((ColumnField o) => o.Key == "FSTARTDATEBALANCE")?.Visible ?? false;
                bShowColEB = columnInfo.FirstOrDefault((ColumnField o) => o.Key == "FENDDATEBALANCE")?.Visible ?? false;
                bShowColSQ = columnInfo.FirstOrDefault((ColumnField o) => o.Key == "FSTARTQTY")?.Visible ?? false;
                bShowColEQ = columnInfo.FirstOrDefault((ColumnField o) => o.Key == "FENDQTY")?.Visible ?? false;
            }
            else
            {
                bShowColSB = (bShowColEB = (bShowColSQ = (bShowColEQ = false)));
            }
        }

        private void RemoveRowIfMatchCondition(DataTable dt, Func<DataRow, bool> condition)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return;
            }
            List<int> list = new List<int>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (condition(dt.Rows[i]))
                {
                    list.Add(i);
                }
            }
            for (int num = list.Count - 1; num >= 0; num--)
            {
                dt.Rows.RemoveAt(list[num]);
            }
        }

        private DataTable GetTableScheme()
        {
            Type typeFromHandle = typeof(string);
            Type typeFromHandle2 = typeof(int);
            Type typeFromHandle3 = typeof(decimal);
            DataTable dataTable = new DataTable();
            foreach (DetailGroup item in detailGrp)
            {
                if (item.ValueType == "1")
                {
                    dataTable.Columns.Add(item.FlexNumber, typeFromHandle);
                }
                else
                {
                    dataTable.Columns.Add(item.FlexNumber, typeFromHandle2);
                }
                dataTable.Columns.Add($"{item.FlexNumber}_FFULLPARENTID", typeFromHandle);
                dataTable.Columns.Add($"{item.FlexNumber}_FNUMBER", typeFromHandle);
                dataTable.Columns.Add($"{item.FlexNumber}_FNAME", typeFromHandle);
                dataTable.Columns.Add($"{item.FlexNumber}_FGROUPID", typeFromHandle2);
                dataTable.Columns.Add($"{item.FlexNumber}_FPARENTGROUPID", typeFromHandle2);
                dataTable.Columns.Add($"{item.FlexNumber}_FFLEXLEVEL", typeFromHandle2);
            }
            dataTable.Columns.AddRange(new DataColumn[37]
            {
                new DataColumn("FIDENTITYID", typeFromHandle2),
                new DataColumn("detailId", typeFromHandle),
                new DataColumn("AcctName", typeFromHandle),
                new DataColumn("RowType", typeFromHandle2),
                new DataColumn("DC", typeFromHandle2),
                new DataColumn("FLEVEL", typeFromHandle2),
                new DataColumn("FPARENTID", typeFromHandle2),
                new DataColumn("F_SR", typeFromHandle3),
                new DataColumn("F_DR", typeFromHandle3),
                new DataColumn("F_CR", typeFromHandle3),
                new DataColumn("F_ER", typeFromHandle3),
                new DataColumn("F_DYR", typeFromHandle3),
                new DataColumn("F_CYR", typeFromHandle3),
                new DataColumn("F_S", typeFromHandle3),
                new DataColumn("F_D", typeFromHandle3),
                new DataColumn("F_C", typeFromHandle3),
                new DataColumn("F_E", typeFromHandle3),
                new DataColumn("F_DY", typeFromHandle3),
                new DataColumn("F_CY", typeFromHandle3),
                new DataColumn("F_SQ", typeFromHandle3),
                new DataColumn("F_DQ", typeFromHandle3),
                new DataColumn("F_CQ", typeFromHandle3),
                new DataColumn("F_EQ", typeFromHandle3),
                new DataColumn("F_DYQ", typeFromHandle3),
                new DataColumn("F_CYQ", typeFromHandle3),
                new DataColumn("F_SDR", typeFromHandle3),
                new DataColumn("F_SCR", typeFromHandle3),
                new DataColumn("F_EDR", typeFromHandle3),
                new DataColumn("F_ECR", typeFromHandle3),
                new DataColumn("F_SD", typeFromHandle3),
                new DataColumn("F_SC", typeFromHandle3),
                new DataColumn("F_ED", typeFromHandle3),
                new DataColumn("F_EC", typeFromHandle3),
                new DataColumn("F_SQD", typeFromHandle3),
                new DataColumn("F_SQC", typeFromHandle3),
                new DataColumn("F_EQD", typeFromHandle3),
                new DataColumn("F_EQC", typeFromHandle3)
            });
            return dataTable;
        }

        private Dictionary<int, Tuple<int, string, string, string>> GetFlexGroupDic(BaseDataTypeGroupInfo groupInfo)
        {
            string strSQL = string.Format("select g.fid,g.fparentid,glevel.FFULLPARENTID,g_l.FNAME,g.FNUMBER\r\n                from {0} g\r\n                left join {0}_l g_l on g.fid=g_l.fid and g_l.flocaleid={2}\r\n                left join {1} glevel on g.fid=glevel.fid", groupInfo.GroupTable, groupInfo.GroupLevelTable, base.Context.UserLocale.LCID);
            return DBUtils.ExecuteDynamicObject(base.Context, strSQL, null, null, CommandType.Text).ToDictionary((DynamicObject r) => Convert.ToInt32(r["fid"]), (DynamicObject r) => new Tuple<int, string, string, string>(Convert.ToInt32(r["fparentid"]), Convert.ToString(r["FFULLPARENTID"]), Convert.ToString(r["FNUMBER"]), Convert.ToString(r["FNAME"])));
        }

        private void SumParentGroup(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return;
            }
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (DetailGroup g in detailGrp)
            {
                bool flag = g.GroupInfo != null && g.GroupInfo.HasGroupField;
                bool flag2 = g.GroupInfo != null && g.GroupInfo.HasGroupLevelTable;
                if (!flag || !flag2)
                {
                    continue;
                }
                if (!dictionary.ContainsKey(g.FlexNumber))
                {
                    EnumerableRowCollection<DataRow> enumerableRowCollection = from p in dt.AsEnumerable()
                                                                               where p[$"{g.FlexNumber}_FFLEXLEVEL"] != DBNull.Value
                                                                               select p;
                    if (enumerableRowCollection.IsEmpty())
                    {
                        continue;
                    }
                    dictionary[g.FlexNumber] = enumerableRowCollection.Max((DataRow r) => Convert.ToInt32(r[$"{g.FlexNumber}_FFLEXLEVEL"]));
                }
                int num = dictionary[g.FlexNumber];
                if (num <= this.flexLevel)
                {
                    continue;
                }
                string groupId = $"{g.FlexNumber}_FGROUPID";
                string parentGroupId = $"{g.FlexNumber}_FPARENTGROUPID";
                string flexLevel = $"{g.FlexNumber}_FFLEXLEVEL";
                string fullParentId = $"{g.FlexNumber}_FFULLPARENTID";
                string number = $"{g.FlexNumber}_FNUMBER";
                string name = $"{g.FlexNumber}_FNAME";
                Dictionary<int, Tuple<int, string, string, string>> groupDic = GetFlexGroupDic(g.GroupInfo);
                int i;
                for (i = num; i > this.flexLevel; i--)
                {
                    IEnumerable<DataRow> enumerable = (from r in dt.AsEnumerable()
                                                       where r.GetField<int>(flexLevel) == i && r.GetField<int>(parentGroupId) > 0
                                                       group r by new
                                                       {
                                                           acctName = r.GetField<string>("AcctName"),
                                                           dc = r.GetField<int>("DC"),
                                                           level = r.GetField<int>("FLEVEL"),
                                                           parentId = r.GetField<int>("FPARENTID"),
                                                           flexKey = GetFlexGrpKeyVal(r, g.FlexNumber),
                                                           parentGroupId = r.GetField<int>(parentGroupId)
                                                       }).Select(gr =>
                                                       {
                                                           DataRow dataRow = dt.NewRow();
                                                           dataRow["AcctName"] = gr.Key.acctName;
                                                           dataRow["DC"] = gr.Key.dc;
                                                           dataRow["FIDENTITYID"] = 0;
                                                           dataRow["detailId"] = 0;
                                                           dataRow["FLEVEL"] = gr.Key.level;
                                                           dataRow["FPARENTID"] = gr.Key.parentId;
                                                           string[] array = gr.Key.flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                                                           string[] array2 = array;
                                                           foreach (string text in array2)
                                                           {
                                                               string[] array3 = text.Split(';');
                                                               if (array3.Length >= 2 && !string.IsNullOrWhiteSpace(array3[1]))
                                                               {
                                                                   dataRow[array3[0]] = array3[1];
                                                               }
                                                           }
                                                           Tuple<int, string, string, string> tuple = groupDic[gr.Key.parentGroupId];
                                                           dataRow[groupId] = gr.Key.parentGroupId;
                                                           dataRow[parentGroupId] = tuple.Item1;
                                                           dataRow[fullParentId] = tuple.Item2;
                                                           dataRow[number] = tuple.Item3;
                                                           dataRow[name] = tuple.Item4;
                                                           dataRow[flexLevel] = i - 1;
                                                           dataRow["F_SR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SR"));
                                                           dataRow["F_DR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                                                           dataRow["F_CR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                                                           dataRow["F_ER"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ER"));
                                                           dataRow["F_DYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                                                           dataRow["F_CYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                                                           dataRow["F_S"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_S"));
                                                           dataRow["F_D"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                                                           dataRow["F_C"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                                                           dataRow["F_E"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_E"));
                                                           dataRow["F_DY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                                                           dataRow["F_CY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                                                           dataRow["F_SQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQ"));
                                                           dataRow["F_DQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                                                           dataRow["F_CQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                                                           dataRow["F_EQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQ"));
                                                           dataRow["F_DYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                                                           dataRow["F_CYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                                                           if (blEndBalDCByAcct)
                                                           {
                                                               if (gr.Key.dc > 0)
                                                               {
                                                                   dataRow["F_SDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                                   dataRow["F_EDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                                   dataRow["F_SD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                                   dataRow["F_ED"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                                   dataRow["F_SQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                                   dataRow["F_EQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                                   dataRow["F_SCR"] = 0;
                                                                   dataRow["F_ECR"] = 0;
                                                                   dataRow["F_SC"] = 0;
                                                                   dataRow["F_EC"] = 0;
                                                                   dataRow["F_SQC"] = 0;
                                                                   dataRow["F_EQC"] = 0;
                                                               }
                                                               else
                                                               {
                                                                   dataRow["F_SCR"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                                   dataRow["F_ECR"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                                   dataRow["F_SC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                                   dataRow["F_EC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                                   dataRow["F_SQC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                                   dataRow["F_EQC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                                   dataRow["F_SDR"] = 0;
                                                                   dataRow["F_EDR"] = 0;
                                                                   dataRow["F_SD"] = 0;
                                                                   dataRow["F_ED"] = 0;
                                                                   dataRow["F_SQD"] = 0;
                                                                   dataRow["F_EQD"] = 0;
                                                               }
                                                           }
                                                           else
                                                           {
                                                               decimal num2 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                               dataRow["F_SDR"] = ((num2 > 0m) ? num2 : 0m);
                                                               dataRow["F_SCR"] = ((num2 > 0m) ? 0m : (-num2));
                                                               decimal num3 = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                               dataRow["F_EDR"] = ((num3 > 0m) ? num3 : 0m);
                                                               dataRow["F_ECR"] = ((num3 > 0m) ? 0m : (-num3));
                                                               decimal num4 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                               dataRow["F_SD"] = ((num4 > 0m) ? num4 : 0m);
                                                               dataRow["F_SC"] = ((num4 > 0m) ? 0m : (-num4));
                                                               decimal num5 = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                               dataRow["F_ED"] = ((num5 > 0m) ? num5 : 0m);
                                                               dataRow["F_EC"] = ((num5 > 0m) ? 0m : (-num5));
                                                               decimal num6 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                               dataRow["F_SQD"] = ((num6 > 0m) ? num6 : 0m);
                                                               dataRow["F_SQC"] = ((num6 > 0m) ? 0m : (-num6));
                                                               decimal num7 = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                               dataRow["F_EQD"] = ((num7 > 0m) ? num7 : 0m);
                                                               dataRow["F_EQC"] = ((num7 > 0m) ? 0m : (-num7));
                                                           }
                                                           return dataRow;
                                                       });
                    foreach (DataRow item in enumerable)
                    {
                        dt.Rows.Add(item);
                    }
                }
            }
        }

        private void SumParentAcct(DataTable dt)
        {
            if (showDetailOnly || dt.Rows.Count == 0)
            {
                return;
            }
            int num = dt.AsEnumerable().Max((DataRow r) => Convert.ToInt32(r["FLEVEL"]));
            Dictionary<int, object> acctDic = GetAllAcctDct();
            int i;
            for (i = num; i > 1; i--)
            {
                IEnumerable<DataRow> enumerable = (from r in dt.AsEnumerable()
                                                   where r.GetField<int>("FLEVEL") == i && r.GetField<int>("FPARENTID") > 0
                                                   group r by new
                                                   {
                                                       parentId = r.GetField<int>("FPARENTID"),
                                                       flexKey = GetFlexGrpKeyVal(r)
                                                   }).Select(gr =>
                                                   {
                                                       DataRow dataRow = dt.NewRow();
                                                       object obj = acctDic[gr.Key.parentId];
                                                       dataRow["AcctName"] = (object)((dynamic)obj).Name;
                                                       dataRow["DC"] = (object)((dynamic)obj).DC;
                                                       dataRow["FLEVEL"] = i - 1;
                                                       dataRow["FPARENTID"] = (object)((dynamic)obj).ParentId;
                                                       dataRow["FIDENTITYID"] = 0;
                                                       dataRow["detailId"] = 0;
                                                       dataRow["RowType"] = 0;
                                                       string[] array = gr.Key.flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                                                       string[] array2 = array;
                                                       foreach (string text in array2)
                                                       {
                                                           string[] array3 = text.Split(';');
                                                           if (array3.Length >= 2 && !string.IsNullOrWhiteSpace(array3[1]))
                                                           {
                                                               dataRow[array3[0]] = array3[1];
                                                           }
                                                       }
                                                       dataRow["F_SR"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_SR") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_DR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                                                       dataRow["F_CR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                                                       dataRow["F_ER"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_ER") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_DYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                                                       dataRow["F_CYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                                                       dataRow["F_S"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_S") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_D"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                                                       dataRow["F_C"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                                                       dataRow["F_E"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_E") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_DY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                                                       dataRow["F_CY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                                                       dataRow["F_SQ"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_SQ") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_DQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                                                       dataRow["F_CQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                                                       dataRow["F_EQ"] = (object)(((dynamic)obj).DC * gr.Sum((DataRow r) => r.GetField<decimal>("F_EQ") * (decimal)r.Field<int>("DC")));
                                                       dataRow["F_DYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                                                       dataRow["F_CYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                                                       if (blEndBalDCByAcct)
                                                       {
                                                           if (((dynamic)obj).DC > 0)
                                                           {
                                                               dataRow["F_SDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                               dataRow["F_EDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                               dataRow["F_SD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                               dataRow["F_ED"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                               dataRow["F_SQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                               dataRow["F_EQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                               dataRow["F_SCR"] = 0;
                                                               dataRow["F_ECR"] = 0;
                                                               dataRow["F_SC"] = 0;
                                                               dataRow["F_EC"] = 0;
                                                               dataRow["F_SQC"] = 0;
                                                               dataRow["F_EQC"] = 0;
                                                           }
                                                           else
                                                           {
                                                               dataRow["F_SCR"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                               dataRow["F_ECR"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                               dataRow["F_SC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                               dataRow["F_EC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                               dataRow["F_SQC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                               dataRow["F_EQC"] = -gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) + gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                               dataRow["F_SDR"] = 0;
                                                               dataRow["F_EDR"] = 0;
                                                               dataRow["F_SD"] = 0;
                                                               dataRow["F_ED"] = 0;
                                                               dataRow["F_SQD"] = 0;
                                                               dataRow["F_EQD"] = 0;
                                                           }
                                                       }
                                                       else
                                                       {
                                                           decimal num2 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                           dataRow["F_SDR"] = ((num2 > 0m) ? num2 : 0m);
                                                           dataRow["F_SCR"] = ((num2 > 0m) ? 0m : (-num2));
                                                           decimal num3 = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                           dataRow["F_EDR"] = ((num3 > 0m) ? num3 : 0m);
                                                           dataRow["F_ECR"] = ((num3 > 0m) ? 0m : (-num3));
                                                           decimal num4 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                           dataRow["F_SD"] = ((num4 > 0m) ? num4 : 0m);
                                                           dataRow["F_SC"] = ((num4 > 0m) ? 0m : (-num4));
                                                           decimal num5 = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                           dataRow["F_ED"] = ((num5 > 0m) ? num5 : 0m);
                                                           dataRow["F_EC"] = ((num5 > 0m) ? 0m : (-num5));
                                                           decimal num6 = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                           dataRow["F_SQD"] = ((num6 > 0m) ? num6 : 0m);
                                                           dataRow["F_SQC"] = ((num6 > 0m) ? 0m : (-num6));
                                                           decimal num7 = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                           dataRow["F_EQD"] = ((num7 > 0m) ? num7 : 0m);
                                                           dataRow["F_EQC"] = ((num7 > 0m) ? 0m : (-num7));
                                                       }
                                                       return dataRow;
                                                   });
                foreach (DataRow item in enumerable)
                {
                    dt.Rows.Add(item);
                }
            }
        }

        private void SumSubTotal(DataTable dt)
        {
            if (reportStyle == 1 || reportStyle == 2)
            {
                int j;
                for (j = detailGrp.Count - 1; j > 0; j--)
                {
                    DetailGroup detailGroup = detailGrp[j - 1];
                    string name2 = $"{detailGroup.FlexNumber}_FNAME";
                    IEnumerable<DataRow> enumerable = (from r in dt.AsEnumerable()
                                                       where r.GetField<int>("RowType") < 2
                                                       group r by new
                                                       {
                                                           rowType = r.GetField<int>("RowType"),
                                                           acctName = r.GetField<string>("AcctName"),
                                                           acctLevel = r.GetField<int>("FLEVEL"),
                                                           dc = r.GetField<int>("DC"),
                                                           flexKey = GetFlexGrpKeyVal(r, j)
                                                       }).Select(gr =>
                                                       {
                                                           DataRow dataRow3 = dt.NewRow();
                                                           string[] array7 = gr.Key.flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                                                           string[] array8 = array7;
                                                           foreach (string text3 in array8)
                                                           {
                                                               string[] array9 = text3.Split(';');
                                                               if (array9.Length >= 2 && !string.IsNullOrWhiteSpace(array9[1]))
                                                               {
                                                                   dataRow3[array9[0]] = array9[1];
                                                               }
                                                           }
                                                           for (int num = j; num < detailGrp.Count; num++)
                                                           {
                                                               DetailGroup g3 = detailGrp[num];
                                                               IEnumerable<string> enumerable6 = flexFieldsArr.Where((string field) => field.StartsWith(g3.FlexNumber));
                                                               foreach (string item in enumerable6)
                                                               {
                                                                   dataRow3[item] = DBNull.Value;
                                                               }
                                                           }
                                                           dataRow3[name2] = $"{dataRow3[name2]} {subTotal}";
                                                           dataRow3["AcctName"] = gr.Key.acctName;
                                                           dataRow3["DC"] = gr.Key.dc;
                                                           dataRow3["FIDENTITYID"] = 0;
                                                           dataRow3["detailId"] = 0;
                                                           dataRow3["FLEVEL"] = gr.Key.acctLevel;
                                                           dataRow3["RowType"] = 20 + gr.Key.rowType;
                                                           dataRow3["F_SR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SR"));
                                                           dataRow3["F_DR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                                                           dataRow3["F_CR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                                                           dataRow3["F_ER"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ER"));
                                                           dataRow3["F_DYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                                                           dataRow3["F_CYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                                                           dataRow3["F_S"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_S"));
                                                           dataRow3["F_D"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                                                           dataRow3["F_C"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                                                           dataRow3["F_E"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_E"));
                                                           dataRow3["F_DY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                                                           dataRow3["F_CY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                                                           dataRow3["F_SQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQ"));
                                                           dataRow3["F_DQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                                                           dataRow3["F_CQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                                                           dataRow3["F_EQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQ"));
                                                           dataRow3["F_DYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                                                           dataRow3["F_CYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                                                           dataRow3["F_SDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR"));
                                                           dataRow3["F_SCR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                           dataRow3["F_EDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR"));
                                                           dataRow3["F_ECR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                           dataRow3["F_SD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD"));
                                                           dataRow3["F_SC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                           dataRow3["F_ED"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED"));
                                                           dataRow3["F_EC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                           dataRow3["F_SQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD"));
                                                           dataRow3["F_SQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                           dataRow3["F_EQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD"));
                                                           dataRow3["F_EQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                           return dataRow3;
                                                       });
                    foreach (DataRow item2 in enumerable)
                    {
                        dt.Rows.Add(item2);
                    }
                }
            }
            else
            {
                if (reportStyle != 3)
                {
                    return;
                }
                int i;
                for (i = detailGrp.Count - 1; i > 1; i--)
                {
                    DetailGroup detailGroup2 = detailGrp[i - 1];
                    string name = $"{detailGroup2.FlexNumber}_FNAME";
                    IEnumerable<DataRow> enumerable2 = (from r in dt.AsEnumerable()
                                                        where r.GetField<int>("RowType") < 2
                                                        group r by new
                                                        {
                                                            rowType = r.GetField<int>("RowType"),
                                                            acctName = r.GetField<string>("AcctName"),
                                                            acctLevel = r.GetField<int>("FLEVEL"),
                                                            dc = r.GetField<int>("DC"),
                                                            flexKey = GetFlexGrpKeyVal(r, i)
                                                        }).Select(gr =>
                                                        {
                                                            DataRow dataRow2 = dt.NewRow();
                                                            string[] array4 = gr.Key.flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                                                            string[] array5 = array4;
                                                            foreach (string text2 in array5)
                                                            {
                                                                string[] array6 = text2.Split(';');
                                                                if (array6.Length >= 2 && !string.IsNullOrWhiteSpace(array6[1]))
                                                                {
                                                                    dataRow2[array6[0]] = array6[1];
                                                                }
                                                            }
                                                            for (int m = i; m < detailGrp.Count; m++)
                                                            {
                                                                DetailGroup g2 = detailGrp[m];
                                                                IEnumerable<string> enumerable5 = flexFieldsArr.Where((string field) => field.StartsWith(g2.FlexNumber));
                                                                foreach (string item3 in enumerable5)
                                                                {
                                                                    dataRow2[item3] = DBNull.Value;
                                                                }
                                                            }
                                                            dataRow2[name] = $"{dataRow2[name]} {subTotal}";
                                                            dataRow2["AcctName"] = gr.Key.acctName;
                                                            dataRow2["DC"] = gr.Key.dc;
                                                            dataRow2["FIDENTITYID"] = 0;
                                                            dataRow2["detailId"] = 0;
                                                            dataRow2["FLEVEL"] = gr.Key.acctLevel;
                                                            dataRow2["RowType"] = 20 + gr.Key.rowType;
                                                            dataRow2["F_SR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SR"));
                                                            dataRow2["F_DR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                                                            dataRow2["F_CR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                                                            dataRow2["F_ER"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ER"));
                                                            dataRow2["F_DYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                                                            dataRow2["F_CYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                                                            dataRow2["F_S"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_S"));
                                                            dataRow2["F_D"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                                                            dataRow2["F_C"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                                                            dataRow2["F_E"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_E"));
                                                            dataRow2["F_DY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                                                            dataRow2["F_CY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                                                            dataRow2["F_SQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQ"));
                                                            dataRow2["F_DQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                                                            dataRow2["F_CQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                                                            dataRow2["F_EQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQ"));
                                                            dataRow2["F_DYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                                                            dataRow2["F_CYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                                                            dataRow2["F_SDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR"));
                                                            dataRow2["F_SCR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                            dataRow2["F_EDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR"));
                                                            dataRow2["F_ECR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                            dataRow2["F_SD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD"));
                                                            dataRow2["F_SC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                            dataRow2["F_ED"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED"));
                                                            dataRow2["F_EC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                            dataRow2["F_SQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD"));
                                                            dataRow2["F_SQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                            dataRow2["F_EQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD"));
                                                            dataRow2["F_EQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                            return dataRow2;
                                                        });
                    foreach (DataRow item4 in enumerable2)
                    {
                        dt.Rows.Add(item4);
                    }
                }
                IEnumerable<DataRow> enumerable3 = (from r in dt.AsEnumerable()
                                                    where r.GetField<int>("RowType") <= 21
                                                    group r by new
                                                    {
                                                        rowType = r.GetField<int>("RowType"),
                                                        acctName = r.GetField<string>("AcctName"),
                                                        acctLevel = r.GetField<int>("FLEVEL"),
                                                        dc = r.GetField<int>("DC"),
                                                        flexKey = GetFlexGrpKeyValForNewStyle(r)
                                                    }).Select(gr =>
                                                    {
                                                        DataRow dataRow = dt.NewRow();
                                                        string[] array = gr.Key.flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                                                        string[] array2 = array;
                                                        foreach (string text in array2)
                                                        {
                                                            string[] array3 = text.Split(';');
                                                            if (array3.Length >= 2 && !string.IsNullOrWhiteSpace(array3[1]))
                                                            {
                                                                dataRow[array3[0]] = array3[1];
                                                            }
                                                        }
                                                        DetailGroup g = detailGrp[0];
                                                        IEnumerable<string> enumerable4 = flexFieldsArr.Where((string field) => field.StartsWith(g.FlexNumber));
                                                        foreach (string item5 in enumerable4)
                                                        {
                                                            dataRow[item5] = DBNull.Value;
                                                        }
                                                        dataRow["AcctName"] = $"{gr.Key.acctName} {subTotal}";
                                                        dataRow["DC"] = gr.Key.dc;
                                                        dataRow["FIDENTITYID"] = 0;
                                                        dataRow["detailId"] = 0;
                                                        dataRow["FLEVEL"] = gr.Key.acctLevel;
                                                        dataRow["RowType"] = 50 + gr.Key.rowType;
                                                        dataRow["F_SR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SR"));
                                                        dataRow["F_DR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                                                        dataRow["F_CR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                                                        dataRow["F_ER"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ER"));
                                                        dataRow["F_DYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                                                        dataRow["F_CYR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                                                        dataRow["F_S"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_S"));
                                                        dataRow["F_D"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                                                        dataRow["F_C"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                                                        dataRow["F_E"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_E"));
                                                        dataRow["F_DY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                                                        dataRow["F_CY"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                                                        dataRow["F_SQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQ"));
                                                        dataRow["F_DQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                                                        dataRow["F_CQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                                                        dataRow["F_EQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQ"));
                                                        dataRow["F_DYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                                                        dataRow["F_CYQ"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                                                        dataRow["F_SDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SDR"));
                                                        dataRow["F_SCR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                                                        dataRow["F_EDR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EDR"));
                                                        dataRow["F_ECR"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                                                        dataRow["F_SD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SD"));
                                                        dataRow["F_SC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                                                        dataRow["F_ED"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_ED"));
                                                        dataRow["F_EC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                                                        dataRow["F_SQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQD"));
                                                        dataRow["F_SQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                                                        dataRow["F_EQD"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQD"));
                                                        dataRow["F_EQC"] = gr.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                                                        return dataRow;
                                                    });
                foreach (DataRow item6 in enumerable3)
                {
                    dt.Rows.Add(item6);
                }
            }
        }

        private void SumTotal(DataTable dt, int idc, int iMaxAcctLevel)
        {
            IEnumerable<IGrouping<object, DataRow>> enumerable = ((reportStyle == 1) ? (from r in dt.AsEnumerable()
                                                                                        where r.Field<int>("RowType") == 1 || r.Field<int>("RowType") == 0
                                                                                        group r by new
                                                                                        {
                                                                                            rowType = r.GetField<int>("RowType"),
                                                                                            acctName = r.Field<string>("AcctName"),
                                                                                            acctLevel = r.Field<int>("FLEVEL"),
                                                                                            dc = r.Field<int>("DC")
                                                                                        }) : ((reportStyle != 2) ? ((IEnumerable<IGrouping<object, DataRow>>)(from r in dt.AsEnumerable()
                                                                                                                                                              where r.Field<int>("RowType") == 1 || r.Field<int>("RowType") == 0 || r.Field<int>("RowType") == 51 || r.Field<int>("RowType") == 50
                                                                                                                                                              group r by new
                                                                                                                                                              {
                                                                                                                                                                  rowType = r.GetField<int>("RowType"),
                                                                                                                                                                  acctName = r.Field<string>("AcctName"),
                                                                                                                                                                  acctLevel = r.Field<int>("FLEVEL"),
                                                                                                                                                                  dc = r.Field<int>("DC"),
                                                                                                                                                                  flexKey = GetFlexGrpKeyVal(r, 1)
                                                                                                                                                              })) : ((IEnumerable<IGrouping<object, DataRow>>)(from r in dt.AsEnumerable()
                                                                                                                                                                                                               where r.Field<int>("RowType") == 1 || (r.Field<int>("RowType") == 0 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 21 || (r.Field<int>("RowType") == 20 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 31 || (r.Field<int>("RowType") == 30 && r.Field<int>("FLEVEL") == iMaxAcctLevel)
                                                                                                                                                                                                               group r by new
                                                                                                                                                                                                               {
                                                                                                                                                                                                                   flexKey = GetFlexGrpKeyVal(r)
                                                                                                                                                                                                               }))));
            foreach (IGrouping<object, DataRow> item in enumerable)
            {
                DataRow dataRow = dt.NewRow();
                idc = ((reportStyle == 1 || reportStyle == 3) ? ((dynamic)item.Key).dc : ((object)idc));
                if (reportStyle == 1)
                {
                    DetailGroup detailGroup = detailGrp[0];
                    dataRow[$"{detailGroup.FlexNumber}_FNAME"] = total;
                    dataRow["AcctName"] = (object)((dynamic)item.Key).acctName;
                    dataRow["FLEVEL"] = (object)((dynamic)item.Key).acctLevel;
                }
                else if (reportStyle == 2)
                {
                    dynamic val = ((dynamic)item.Key).flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (dynamic item2 in val)
                    {
                        dynamic val2 = item2.Split(';');
                        if (!((val2.Length < 2 || string.IsNullOrWhiteSpace(val2[1])) ? true : false))
                        {
                            dataRow[val2[0]] = val2[1];
                        }
                    }
                    dataRow["AcctName"] = total;
                }
                else
                {
                    DetailGroup detailGroup2 = detailGrp[1];
                    dataRow[$"{detailGroup2.FlexNumber}_FNAME"] = total;
                    dataRow["AcctName"] = (object)((dynamic)item.Key).acctName;
                    dataRow["FLEVEL"] = (object)((dynamic)item.Key).acctLevel;
                    dynamic val3 = ((dynamic)item.Key).flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (dynamic item3 in val3)
                    {
                        dynamic val4 = item3.Split(';');
                        if (!((val4.Length < 2 || string.IsNullOrWhiteSpace(val4[1])) ? true : false))
                        {
                            dataRow[val4[0]] = val4[1];
                        }
                    }
                }
                dataRow["FIDENTITYID"] = 0;
                dataRow["detailId"] = 0;
                if (reportStyle == 1 || reportStyle == 3)
                {
                    dataRow["RowType"] = (object)(30 + ((dynamic)item.Key).rowType);
                }
                else if (reportStyle == 2)
                {
                    dataRow["RowType"] = 3;
                }
                dataRow["DC"] = idc;
                dataRow["F_SR"] = GetSum(item, "F_SR", idc);
                dataRow["F_DR"] = GetSum(item, "F_DR", 1);
                dataRow["F_CR"] = GetSum(item, "F_CR", 1);
                dataRow["F_ER"] = GetSum(item, "F_ER", idc);
                dataRow["F_DYR"] = GetSum(item, "F_DYR", 1);
                dataRow["F_CYR"] = GetSum(item, "F_CYR", 1);
                dataRow["F_S"] = GetSum(item, "F_S", idc);
                dataRow["F_D"] = GetSum(item, "F_D", 1);
                dataRow["F_C"] = GetSum(item, "F_C", 1);
                dataRow["F_E"] = GetSum(item, "F_E", idc);
                dataRow["F_DY"] = GetSum(item, "F_DY", 1);
                dataRow["F_CY"] = GetSum(item, "F_CY", 1);
                dataRow["F_SQ"] = GetSum(item, "F_SQ", idc);
                dataRow["F_DQ"] = GetSum(item, "F_DQ", 1);
                dataRow["F_CQ"] = GetSum(item, "F_CQ", 1);
                dataRow["F_EQ"] = GetSum(item, "F_EQ", idc);
                dataRow["F_DYQ"] = GetSum(item, "F_DYQ", 1);
                dataRow["F_CYQ"] = GetSum(item, "F_CYQ", 1);
                dataRow["F_SDR"] = GetSum(item, "F_SDR", 1);
                dataRow["F_SCR"] = GetSum(item, "F_SCR", 1);
                dataRow["F_EDR"] = GetSum(item, "F_EDR", 1);
                dataRow["F_ECR"] = GetSum(item, "F_ECR", 1);
                dataRow["F_SD"] = GetSum(item, "F_SD", 1);
                dataRow["F_SC"] = GetSum(item, "F_SC", 1);
                dataRow["F_ED"] = GetSum(item, "F_ED", 1);
                dataRow["F_EC"] = GetSum(item, "F_EC", 1);
                dataRow["F_SQD"] = GetSum(item, "F_SQD", 1);
                dataRow["F_SQC"] = GetSum(item, "F_SQC", 1);
                dataRow["F_EQD"] = GetSum(item, "F_EQD", 1);
                dataRow["F_EQC"] = GetSum(item, "F_EQC", 1);
                dt.Rows.Add(dataRow);
            }
        }

        private void SumTotalV(DataTable dt, int idc, int iMaxAcctLevel)
        {
            IEnumerable<IGrouping<object, DataRow>> enumerable = ((reportStyle == 1) ? (from r in dt.AsEnumerable()
                                                                                        where r.Field<int>("RowType") == 1 || (r.Field<int>("RowType") == 0 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 21 || (r.Field<int>("RowType") == 20 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 31 || (r.Field<int>("RowType") == 30 && r.Field<int>("FLEVEL") == iMaxAcctLevel)
                                                                                        group r by GetFlexGrpKeyVal(r)) : ((reportStyle != 2) ? ((IEnumerable<IGrouping<object, DataRow>>)(from r in dt.AsEnumerable()
                                                                                                                                                                                           where r.Field<int>("RowType") == 1 || (r.Field<int>("RowType") == 0 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 21 || (r.Field<int>("RowType") == 20 && r.Field<int>("FLEVEL") == iMaxAcctLevel) || r.Field<int>("RowType") == 31 || (r.Field<int>("RowType") == 30 && r.Field<int>("FLEVEL") == iMaxAcctLevel)
                                                                                                                                                                                           group r by GetFlexGrpKeyVal(r, detailGrp[0].FlexNumber))) : ((IEnumerable<IGrouping<object, DataRow>>)(from r in dt.AsEnumerable()
                                                                                                                                                                                                                                                                                                  where r.Field<int>("RowType") == 1 || r.Field<int>("RowType") == 0
                                                                                                                                                                                                                                                                                                  group r by new
                                                                                                                                                                                                                                                                                                  {
                                                                                                                                                                                                                                                                                                      rowType = r.GetField<int>("RowType"),
                                                                                                                                                                                                                                                                                                      acctName = r.Field<string>("AcctName"),
                                                                                                                                                                                                                                                                                                      acctLevel = r.GetField<int>("FLEVEL"),
                                                                                                                                                                                                                                                                                                      dc = r.Field<int>("DC")
                                                                                                                                                                                                                                                                                                  }))));
            foreach (IGrouping<object, DataRow> item in enumerable)
            {
                DataRow dataRow = dt.NewRow();
                idc = ((reportStyle == 1 || reportStyle == 3) ? ((object)idc) : ((dynamic)item.Key).dc);
                if (reportStyle == 1 || reportStyle == 3)
                {
                    dynamic val = ((dynamic)item.Key).Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (dynamic item2 in val)
                    {
                        dynamic val2 = item2.Split(';');
                        if (!((val2.Length < 2 || string.IsNullOrWhiteSpace(val2[1])) ? true : false))
                        {
                            dataRow[val2[0]] = item2.Substring(item2.IndexOf(';') + 1);
                        }
                    }
                    dataRow["AcctName"] = total;
                }
                else
                {
                    string[] array = flexFieldsArr;
                    foreach (string columnName in array)
                    {
                        dataRow[columnName] = DBNull.Value;
                    }
                    DetailGroup detailGroup = detailGrp[0];
                    dataRow[$"{detailGroup.FlexNumber}_FNAME"] = total;
                    dataRow["AcctName"] = (object)((dynamic)item.Key).acctName;
                    dataRow["FLEVEL"] = (object)((dynamic)item.Key).acctLevel;
                }
                dataRow["FIDENTITYID"] = 0;
                dataRow["detailId"] = 0;
                if (reportStyle == 1 || reportStyle == 3)
                {
                    dataRow["RowType"] = 3;
                }
                else if (reportStyle == 2)
                {
                    dataRow["RowType"] = (object)(30 + ((dynamic)item.Key).rowType);
                }
                dataRow["DC"] = idc;
                dataRow["F_SR"] = GetSum(item, "F_SR", idc);
                dataRow["F_DR"] = GetSum(item, "F_DR", 1);
                dataRow["F_CR"] = GetSum(item, "F_CR", 1);
                dataRow["F_ER"] = GetSum(item, "F_ER", idc);
                dataRow["F_DYR"] = GetSum(item, "F_DYR", 1);
                dataRow["F_CYR"] = GetSum(item, "F_CYR", 1);
                dataRow["F_S"] = GetSum(item, "F_S", idc);
                dataRow["F_D"] = GetSum(item, "F_D", 1);
                dataRow["F_C"] = GetSum(item, "F_C", 1);
                dataRow["F_E"] = GetSum(item, "F_E", idc);
                dataRow["F_DY"] = GetSum(item, "F_DY", 1);
                dataRow["F_CY"] = GetSum(item, "F_CY", 1);
                dataRow["F_SQ"] = GetSum(item, "F_SQ", idc);
                dataRow["F_DQ"] = GetSum(item, "F_DQ", 1);
                dataRow["F_CQ"] = GetSum(item, "F_CQ", 1);
                dataRow["F_EQ"] = GetSum(item, "F_EQ", idc);
                dataRow["F_DYQ"] = GetSum(item, "F_DYQ", 1);
                dataRow["F_CYQ"] = GetSum(item, "F_CYQ", 1);
                dataRow["F_SDR"] = GetSum(item, "F_SDR", 1);
                dataRow["F_SCR"] = GetSum(item, "F_SCR", 1);
                dataRow["F_EDR"] = GetSum(item, "F_EDR", 1);
                dataRow["F_ECR"] = GetSum(item, "F_ECR", 1);
                dataRow["F_SD"] = GetSum(item, "F_SD", 1);
                dataRow["F_SC"] = GetSum(item, "F_SC", 1);
                dataRow["F_ED"] = GetSum(item, "F_ED", 1);
                dataRow["F_EC"] = GetSum(item, "F_EC", 1);
                dataRow["F_SQD"] = GetSum(item, "F_SQD", 1);
                dataRow["F_SQC"] = GetSum(item, "F_SQC", 1);
                dataRow["F_EQD"] = GetSum(item, "F_EQD", 1);
                dataRow["F_EQC"] = GetSum(item, "F_EQC", 1);
                dt.Rows.Add(dataRow);
            }
        }

        private decimal GetSum(IGrouping<dynamic, DataRow> gr, string fieldName, int iDC)
        {
            if (curPeriodFields.Any((string f) => f.Equals(fieldName)))
            {
                return gr.Sum((DataRow r) => r.GetField<decimal>(fieldName));
            }
            if (bShowBalDC)
            {
                return gr.Sum((DataRow r) => r.GetField<decimal>(fieldName));
            }
            decimal num = gr.Sum((DataRow r) => (r.GetField<int>("DC") != 1) ? 0m : r.GetField<decimal>(fieldName));
            decimal num2 = gr.Sum((DataRow r) => (r.GetField<int>("DC") != -1) ? 0m : r.GetField<decimal>(fieldName));
            return (num - num2) * (decimal)iDC;
        }

        private void DealShowDCBalCol(DataTable dtbl, IRptParams filter)
        {
            if (bShowBalDC)
            {
                RemoveColumnFromDataTable(dtbl, "F_S");
                RemoveColumnFromDataTable(dtbl, "F_E");
                RemoveColumnFromDataTable(dtbl, "F_SR");
                RemoveColumnFromDataTable(dtbl, "F_ER");
                if (!bShowColSB)
                {
                    if (currencyId == 0)
                    {
                        RemoveColumnFromDataTable(dtbl, "F_SD");
                        RemoveColumnFromDataTable(dtbl, "F_SC");
                    }
                    else if (currencyId == bookCurrencyId)
                    {
                        RemoveColumnFromDataTable(dtbl, "F_SDR");
                        RemoveColumnFromDataTable(dtbl, "F_SCR");
                    }
                }
                if (!bShowColEB)
                {
                    if (currencyId == 0)
                    {
                        RemoveColumnFromDataTable(dtbl, "F_ED");
                        RemoveColumnFromDataTable(dtbl, "F_EC");
                    }
                    else if (currencyId == bookCurrencyId)
                    {
                        RemoveColumnFromDataTable(dtbl, "F_EDR");
                        RemoveColumnFromDataTable(dtbl, "F_ECR");
                    }
                }
                RemoveColumnFromDataTable(dtbl, "F_SQ");
                RemoveColumnFromDataTable(dtbl, "F_EQ");
                if (!bShowColSQ)
                {
                    RemoveColumnFromDataTable(dtbl, "F_SQD");
                    RemoveColumnFromDataTable(dtbl, "F_SQC");
                }
                if (!bShowColEQ)
                {
                    RemoveColumnFromDataTable(dtbl, "F_EQD");
                    RemoveColumnFromDataTable(dtbl, "F_EQC");
                }
            }
            else
            {
                RemoveColumnFromDataTable(dtbl, new string[12]
                {
                    "F_SDR", "F_SCR", "F_EDR", "F_ECR", "F_SD", "F_SC", "F_ED", "F_EC", "F_SQD", "F_SQC",
                    "F_EQD", "F_EQC"
                });
            }
        }

        private void RemoveColumnFromDataTable(DataTable dtbl, string colName)
        {
            if (dtbl.Columns.Contains(colName))
            {
                dtbl.Columns.Remove(colName);
            }
        }

        private void RemoveColumnFromDataTable(DataTable dtbl, string[] colNameArr)
        {
            foreach (string name in colNameArr)
            {
                if (dtbl.Columns.Contains(name))
                {
                    dtbl.Columns.Remove(name);
                }
            }
        }

        private void DealData(DataTable dt, IRptParams filter)
        {
            if (dt != null)
            {
                SetDataTableIndexFilter(filter, dt);
                if (currencyId == 0)
                {
                    dt.Columns.Remove("F_SR");
                    dt.Columns.Remove("F_DR");
                    dt.Columns.Remove("F_CR");
                    dt.Columns.Remove("F_ER");
                    dt.Columns.Remove("F_DYR");
                    dt.Columns.Remove("F_CYR");
                    dt.Columns.Remove("F_SDR");
                    dt.Columns.Remove("F_SCR");
                    dt.Columns.Remove("F_EDR");
                    dt.Columns.Remove("F_ECR");
                    RemoveColumn(filter, dt, 0);
                }
                else if (currencyId == bookCurrencyId)
                {
                    dt.Columns.Remove("F_S");
                    dt.Columns.Remove("F_D");
                    dt.Columns.Remove("F_C");
                    dt.Columns.Remove("F_E");
                    dt.Columns.Remove("F_DY");
                    dt.Columns.Remove("F_CY");
                    dt.Columns.Remove("F_SD");
                    dt.Columns.Remove("F_SC");
                    dt.Columns.Remove("F_ED");
                    dt.Columns.Remove("F_EC");
                    RemoveColumn(filter, dt, 1);
                }
                else
                {
                    RemoveColumn(filter, dt, 2);
                }
            }
        }

        private void RemoveColumn(IRptParams filter, DataTable dtable, int type)
        {
            List<ColumnField> columnInfo = filter.FilterParameter.ColumnInfo;
            if (columnInfo.Count == 12)
            {
                return;
            }
            List<string> list = new List<string>();
            list.Add("FDEBIT");
            list.Add("FCREDIT");
            list.Add("FYTDDEBIT");
            list.Add("FYTDCREDIT");
            list.Add("FSTARTDATEBALANCE");
            list.Add("FENDDATEBALANCE");
            list.Add("FDEBITQTY");
            list.Add("FCREDITQTY");
            list.Add("FYTDDIBITQTY");
            list.Add("FYTDCREDITQTY");
            list.Add("FSTARTQTY");
            list.Add("FENDQTY");
            foreach (ColumnField item in columnInfo)
            {
                list.Remove(item.FieldName);
            }
            switch (type)
            {
                case 0:
                    {
                        using (List<string>.Enumerator enumerator3 = list.GetEnumerator())
                        {
                            while (enumerator3.MoveNext())
                            {
                                switch (enumerator3.Current)
                                {
                                    case "FDEBIT":
                                        dtable.Columns.Remove("F_D");
                                        break;
                                    case "FCREDIT":
                                        dtable.Columns.Remove("F_C");
                                        break;
                                    case "FYTDDEBIT":
                                        dtable.Columns.Remove("F_DY");
                                        break;
                                    case "FYTDCREDIT":
                                        dtable.Columns.Remove("F_CY");
                                        break;
                                    case "FSTARTDATEBALANCE":
                                        dtable.Columns.Remove("F_S");
                                        break;
                                    case "FENDDATEBALANCE":
                                        dtable.Columns.Remove("F_E");
                                        break;
                                    case "FDEBITQTY":
                                        dtable.Columns.Remove("F_DQ");
                                        break;
                                    case "FCREDITQTY":
                                        dtable.Columns.Remove("F_CQ");
                                        break;
                                    case "FYTDDIBITQTY":
                                        dtable.Columns.Remove("F_DYQ");
                                        break;
                                    case "FYTDCREDITQTY":
                                        dtable.Columns.Remove("F_CYQ");
                                        break;
                                    case "FSTARTQTY":
                                        dtable.Columns.Remove("F_SQ");
                                        break;
                                    case "FENDQTY":
                                        dtable.Columns.Remove("F_EQ");
                                        break;
                                }
                            }
                        }
                        return;
                    }
                case 1:
                    {
                        using (List<string>.Enumerator enumerator2 = list.GetEnumerator())
                        {
                            while (enumerator2.MoveNext())
                            {
                                switch (enumerator2.Current)
                                {
                                    case "FDEBIT":
                                        dtable.Columns.Remove("F_DR");
                                        break;
                                    case "FCREDIT":
                                        dtable.Columns.Remove("F_CR");
                                        break;
                                    case "FYTDDEBIT":
                                        dtable.Columns.Remove("F_DYR");
                                        break;
                                    case "FYTDCREDIT":
                                        dtable.Columns.Remove("F_CYR");
                                        break;
                                    case "FSTARTDATEBALANCE":
                                        dtable.Columns.Remove("F_SR");
                                        break;
                                    case "FENDDATEBALANCE":
                                        dtable.Columns.Remove("F_ER");
                                        break;
                                    case "FDEBITQTY":
                                        dtable.Columns.Remove("F_DQ");
                                        break;
                                    case "FCREDITQTY":
                                        dtable.Columns.Remove("F_CQ");
                                        break;
                                    case "FYTDDIBITQTY":
                                        dtable.Columns.Remove("F_DYQ");
                                        break;
                                    case "FYTDCREDITQTY":
                                        dtable.Columns.Remove("F_CYQ");
                                        break;
                                    case "FSTARTQTY":
                                        dtable.Columns.Remove("F_SQ");
                                        break;
                                    case "FENDQTY":
                                        dtable.Columns.Remove("F_EQ");
                                        break;
                                }
                            }
                        }
                        return;
                    }
            }
            using List<string>.Enumerator enumerator4 = list.GetEnumerator();
            while (enumerator4.MoveNext())
            {
                switch (enumerator4.Current)
                {
                    case "FDEBIT":
                        dtable.Columns.Remove("F_D");
                        dtable.Columns.Remove("F_DR");
                        break;
                    case "FCREDIT":
                        dtable.Columns.Remove("F_C");
                        dtable.Columns.Remove("F_CR");
                        break;
                    case "FYTDDEBIT":
                        dtable.Columns.Remove("F_DY");
                        dtable.Columns.Remove("F_DYR");
                        break;
                    case "FYTDCREDIT":
                        dtable.Columns.Remove("F_CY");
                        dtable.Columns.Remove("F_CYR");
                        break;
                    case "FSTARTDATEBALANCE":
                        dtable.Columns.Remove("F_S");
                        dtable.Columns.Remove("F_SR");
                        break;
                    case "FENDDATEBALANCE":
                        dtable.Columns.Remove("F_E");
                        dtable.Columns.Remove("F_ER");
                        break;
                    case "FDEBITQTY":
                        dtable.Columns.Remove("F_DQ");
                        break;
                    case "FCREDITQTY":
                        dtable.Columns.Remove("F_CQ");
                        break;
                    case "FYTDDIBITQTY":
                        dtable.Columns.Remove("F_DYQ");
                        break;
                    case "FYTDCREDITQTY":
                        dtable.Columns.Remove("F_CYQ");
                        break;
                    case "FSTARTQTY":
                        dtable.Columns.Remove("F_SQ");
                        break;
                    case "FENDQTY":
                        dtable.Columns.Remove("F_EQ");
                        break;
                }
            }
        }

        private void SetDataTableIndexFilter(IRptParams filter, DataTable dtable)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            List<ColumnField> list = filter.FilterParameter.ColumnInfo.OrderBy((ColumnField s) => s.ColIndex).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                dictionary.Add(list[i].FieldName, i);
            }
            int num = 3;
            using Dictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                switch (enumerator.Current.Key)
                {
                    case "FDEBIT":
                        dtable.Columns["F_DR"].SetOrdinal(num);
                        dtable.Columns["F_D"].SetOrdinal(num + 1);
                        num += 2;
                        break;
                    case "FCREDIT":
                        dtable.Columns["F_CR"].SetOrdinal(num);
                        dtable.Columns["F_C"].SetOrdinal(num + 1);
                        num += 2;
                        break;
                    case "FYTDDEBIT":
                        dtable.Columns["F_DYR"].SetOrdinal(num);
                        dtable.Columns["F_DY"].SetOrdinal(num + 1);
                        num += 2;
                        break;
                    case "FYTDCREDIT":
                        dtable.Columns["F_CYR"].SetOrdinal(num);
                        dtable.Columns["F_CY"].SetOrdinal(num + 1);
                        num += 2;
                        break;
                    case "FSTARTDATEBALANCE":
                        dtable.Columns["F_SR"].SetOrdinal(num);
                        dtable.Columns["F_SDR"].SetOrdinal(num + 1);
                        dtable.Columns["F_SCR"].SetOrdinal(num + 2);
                        dtable.Columns["F_S"].SetOrdinal(num + 3);
                        dtable.Columns["F_SD"].SetOrdinal(num + 4);
                        dtable.Columns["F_SC"].SetOrdinal(num + 5);
                        num += 6;
                        break;
                    case "FENDDATEBALANCE":
                        dtable.Columns["F_ER"].SetOrdinal(num);
                        dtable.Columns["F_EDR"].SetOrdinal(num + 1);
                        dtable.Columns["F_ECR"].SetOrdinal(num + 2);
                        dtable.Columns["F_E"].SetOrdinal(num + 3);
                        dtable.Columns["F_ED"].SetOrdinal(num + 4);
                        dtable.Columns["F_EC"].SetOrdinal(num + 5);
                        num += 6;
                        break;
                    case "FDEBITQTY":
                        dtable.Columns["F_DQ"].SetOrdinal(num);
                        num++;
                        break;
                    case "FCREDITQTY":
                        dtable.Columns["F_CQ"].SetOrdinal(num);
                        num++;
                        break;
                    case "FYTDDIBITQTY":
                        dtable.Columns["F_DYQ"].SetOrdinal(num);
                        num++;
                        break;
                    case "FYTDCREDITQTY":
                        dtable.Columns["F_CYQ"].SetOrdinal(num);
                        num++;
                        break;
                    case "FSTARTQTY":
                        dtable.Columns["F_SQ"].SetOrdinal(num);
                        dtable.Columns["F_SQD"].SetOrdinal(num + 1);
                        dtable.Columns["F_SQC"].SetOrdinal(num + 2);
                        num += 3;
                        break;
                    case "FENDQTY":
                        dtable.Columns["F_EQ"].SetOrdinal(num);
                        dtable.Columns["F_EQD"].SetOrdinal(num + 1);
                        dtable.Columns["F_EQC"].SetOrdinal(num + 2);
                        num += 3;
                        break;
                }
            }
        }

        private Dictionary<string, DynamicObject> GetValueSources()
        {
            if (valueSources == null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("select t2.FVALUETYPE,t2.FVALUESOURCE,t2.FFLEXNUMBER,t3.FMASTERIDFIELDNAME,t3.FORGFIELDNAME,ISNULL(t4.FSTRATEGYTYPE,0) as FSTRATEGYTYPE,");
                stringBuilder.AppendLine("Case t2.FVALUETYPE When '0' then t3.FTABLENAME When '1' then 't_BAS_AssistantDataEntry' end FTABLENAME,");
                stringBuilder.AppendLine("Case t2.FVALUETYPE When '0' then t3.FPKFIELDNAME When '1' then 'FEntryID' End FPKFIELDNAME,");
                stringBuilder.AppendLine("Case t2.FVALUETYPE When '0' then t3.FNAMEISLOCALE When '1' then 1 End FNAMEISLOCALE,");
                stringBuilder.AppendLine("Case t2.FVALUETYPE When '0' then t3.FNAMEFIELDNAME When '1' then 'FDATAVALUE' End FNAMEFIELDNAME,");
                stringBuilder.AppendLine("Case t2.FVALUETYPE When '0' then t3.FNUMBERFIELDNAME When '1' then 'FNumber' End FNUMBERFIELDNAME");
                stringBuilder.AppendLine("From T_BD_FLEXITEMPROPERTY t2");
                stringBuilder.AppendLine("left join t_meta_lookupclass t3 on t2.fvaluesource = t3.fformid");
                stringBuilder.AppendLine("left join T_META_BASEDATATYPE t4 on t2.FVALUESOURCE=t4.FBASEDATATYPEID");
                DynamicObjectCollection source = DBUtils.ExecuteDynamicObject(base.Context, stringBuilder.ToString(), null, null, CommandType.Text);
                valueSources = source.ToDictionary((DynamicObject p) => p["FVALUESOURCE"].ToString());
            }
            return valueSources;
        }

        private DataTable GetVoucherData(DateTime startMonth)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("SELECT R.FACCOUNTBOOKID,R.FDATE,R.FPOSTED,\r\n                            Y.FACCOUNTID,acct.FLEVEL,acct.FPARENTID,Y.FDETAILID,Y.FCURRENCYID,\r\n                            SUM(case Y.fdc when 1 then Y.FAMOUNT else 0 end) as FDEBIT,\r\n                            SUM(case Y.fdc when 1 then Y.FAMOUNTFOR else 0 end) as FDEBITFOR,\r\n                            SUM(case Y.fdc when -1 then Y.FAMOUNT else 0 end) as FCREDIT,\r\n                            SUM(case Y.fdc when -1 then Y.FAMOUNTFOR else 0 end) as FCREDITFOR,\r\n                            SUM(case Y.fdc when 1 then ISNULL(Y.FACCTUNITQTY,0) else 0 end) as FDEBITQTY,\r\n                            SUM(case Y.fdc when -1 then ISNULL(Y.FACCTUNITQTY,0) else 0 end) as FCREDITQTY,\r\n                            Y.FDC,{0}\r\n                            FROM T_GL_VOUCHERENTRY Y \r\n                            LEFT JOIN T_GL_VOUCHER r ON Y.FVOUCHERID=r.FVOUCHERID\r\n                            left join t_bd_account acct on y.faccountid=acct.facctid", flexFields);
            stringBuilder.AppendFormat(" inner join {0} as detail on y.fdetailid=detail.fid", strFlexTmpTableName);
            if (!forbidAcct)
            {
                stringBuilder.AppendFormat(" {0} ", new AccountFilterService().GetForbidSqlString("acct", bookId));
            }
            stringBuilder.AppendFormat(" INNER JOIN (SELECT /*+ CARDINALITY(TBL {0})*/FID FROM TABLE(fn_StrSplit(@IDS, ',',1)) TBL) SP ON Y.FACCOUNTID=SP.FID", lstAcctIds.Count);
            SqlParam param = new SqlParam("@IDS", KDDbType.udt_inttable, lstAcctIds.Distinct().ToArray());
            stringBuilder.AppendFormat(" WHERE r.FACCOUNTBOOKID={0} \r\n                                AND R.FDOCUMENTSTATUS<>'Z' \r\n                                AND R.FINVALID='0' \r\n                                AND r.FDATE>=TO_DATE('{1}') \r\n                                AND r.FDATE<TO_DATE('{2}')\r\n                                AND Y.FDETAILID>0 {3} ", bookId, startMonth.ToString("yyyy-MM-dd HH:mm:ss"), endDate.AddDays(1.0).ToString("yyyy-MM-dd HH:mm:ss"), (currencyId == 0) ? "" : (" AND y.FCURRENCYID=" + currencyId));
            if (!notPostVercher)
            {
                stringBuilder.Append(" AND R.FPOSTED='1'");
            }
            if (excludeAdjustVch)
            {
                stringBuilder.Append(" AND R.FISADJUSTVOUCHER='0' ");
            }
            if (lstBaseDataTempTable != null && lstBaseDataTempTable.Count > 0)
            {
                foreach (BaseDataTempTable item in lstBaseDataTempTable)
                {
                    if (item.BaseDataFormId == "BD_Account")
                    {
                        stringBuilder.AppendFormat(" AND EXISTS (SELECT 1 FROM {0} ACTDR WHERE ACTDR.{1}=Y.FACCOUNTID)", item.TempTable, item.PKFieldName);
                    }
                }
            }
            stringBuilder.Append(" GROUP BY R.FACCOUNTBOOKID,R.FDATE,R.FPOSTED,Y.FACCOUNTID,acct.FLEVEL,acct.FPARENTID,Y.FDETAILID,Y.FCURRENCYID,Y.FDC");
            stringBuilder.AppendFormat(",{0}", flexFields);
            return DBUtils.ExecuteDataSet(base.Context, stringBuilder.ToString(), param).Tables[0];
        }

        private string GetNeverUsedFlexJoinSql(out string flexFields)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<string> list = new List<string>();
            List<string> list2 = new List<string>();
            Dictionary<string, DynamicObject> dictionary = GetValueSources();
            int num = 0;
            StringBuilder stringBuilder2 = new StringBuilder();
            List<SqlParam> list3 = new List<SqlParam>();
            StringBuilder stringBuilder3 = new StringBuilder();
            stringBuilder3.Append("SELECT DISTINCT ISNULL(v.FID,-1) AS FID,");
            StringBuilder stringBuilder4 = new StringBuilder();
            string tempTableName = CommonFunction.GetTempTableName(base.Context);
            for (int i = 0; i < detailGrp.Count; i++)
            {
                stringBuilder4.AppendFormat("t.{0},\r\n                        t.{0}_fnumber,\r\n                        t.{0}_fname,\r\n                        t.{0}_fgroupid,\r\n                        t.{0}_fparentgroupid,\r\n                        t.{0}_fflexlevel,\r\n                        t.{0}_ffullparentid,", detailGrp[i].FlexNumber);
            }
            stringBuilder3.AppendFormat(stringBuilder4.ToString().TrimEnd(','));
            stringBuilder3.AppendFormat(" INTO {0} FROM T_BD_FLEXITEMDETAILV V ", tempTableName);
            if (bShowNeverUseFlex)
            {
                stringBuilder3.AppendFormat(" RIGHT JOIN ");
            }
            else if (isShowAnyFlex)
            {
                stringBuilder3.AppendFormat(" LEFT JOIN ");
            }
            else
            {
                stringBuilder3.AppendFormat(" INNER JOIN ");
            }
            stringBuilder3.Append("(select ");
            for (int j = 0; j < detailGrp.Count; j++)
            {
                stringBuilder3.AppendFormat("tbl{0}.{1},\r\n                        tbl{0}.{1}_fnumber,\r\n                        tbl{0}.{1}_fname,\r\n                        tbl{0}.{1}_fgroupid,\r\n                        tbl{0}.{1}_fparentgroupid,\r\n                        tbl{0}.{1}_fflexlevel,\r\n                        tbl{0}.{1}_ffullparentid,", j, detailGrp[j].FlexNumber);
            }
            stringBuilder3.Remove(stringBuilder3.Length - 1, 1);
            stringBuilder3.Append(" from ");
            for (int k = 0; k < detailGrp.Count; k++)
            {
                if (!dictionary.TryGetValue(detailGrp[k].DetailType, out var value))
                {
                    continue;
                }
                string value2 = value.GetValue<string>("FTABLENAME");
                string value3 = value.GetValue<string>("FNUMBERFIELDNAME");
                string value4 = value.GetValue<string>("FPKFIELDNAME");
                string value5 = value.GetValue<string>("FORGFIELDNAME");
                string value6 = value.GetValue<string>("FVALUESOURCE");
                string value7 = value.GetValue<string>("FMASTERIDFIELDNAME");
                num = value.GetValue<int>("FSTRATEGYTYPE");
                bool flag;
                string text;
                if (detailGrp[k].ValueType == "0")
                {
                    flag = detailGrp[k].GroupInfo.LookupInfo.NameIsLocale;
                    text = detailGrp[k].GroupInfo.LookupInfo.NameFieldName;
                }
                else if (detailGrp[k].ValueType == "1")
                {
                    flag = true;
                    text = "FDATAVALUE";
                }
                else
                {
                    flag = true;
                    text = "FNAME";
                }
                stringBuilder.Clear();
                stringBuilder.AppendFormat("{0}_tf.{1} {0}", detailGrp[k].FlexNumber, value4);
                stringBuilder.AppendFormat(",{0}_tf.{1} {0}_FNUMBER", detailGrp[k].FlexNumber, value3);
                stringBuilder.AppendFormat(",{0}_tf{1}.{2} {0}_FNAME", detailGrp[k].FlexNumber, flag ? "_l" : "", text);
                if (detailGrp[k].ValueType == "1")
                {
                    stringBuilder2.AppendFormat(" ' ' as {0},null {0}_{1},N'{2}' {0}_{3},null {0}_FGROUPID, null {0}_FPARENTGROUPID, null {0}_FFLEXLEVEL,null {0}_FFULLPARENTID", detailGrp[k].FlexNumber, value3, GlRptConst.UnknownItemName, text);
                }
                else
                {
                    stringBuilder2.AppendFormat(" 0 as {0},null {0}_{1},N'{2}' {0}_{3},null {0}_FGROUPID, null {0}_FPARENTGROUPID, null {0}_FFLEXLEVEL,null {0}_FFULLPARENTID", detailGrp[k].FlexNumber, value3, GlRptConst.UnknownItemName, text);
                }
                bool flag2 = detailGrp[k].GroupInfo != null && detailGrp[k].GroupInfo.HasGroupField;
                bool flag3 = detailGrp[k].GroupInfo != null && detailGrp[k].GroupInfo.HasGroupLevelTable;
                list.Add(string.Format("{0}_FGROUPID,{0}_FPARENTGROUPID,{0}_FNUMBER,{0}_FNAME", detailGrp[k].FlexNumber));
                if (flag2)
                {
                    stringBuilder.AppendFormat(",{1}_tf.{0} {1}_FGROUPID,{1}_tf.{0} {1}_FPARENTGROUPID", detailGrp[k].GroupInfo.GroupFieldName, detailGrp[k].FlexNumber);
                }
                else
                {
                    stringBuilder.AppendFormat(",0 {0}_FGROUPID,0 {0}_FPARENTGROUPID", detailGrp[k].FlexNumber);
                }
                list.Add(string.Format("{0},{0}_FFLEXLEVEL,{0}_FFULLPARENTID", detailGrp[k].FlexNumber));
                if (flag3)
                {
                    stringBuilder.AppendFormat(",isnull({0}_btGrpLevel.FLEVEL,0)+1 {0}_FFLEXLEVEL,(isnull({0}_btGrpLevel.FFULLPARENTID,'.')||'.'|| {0}_tf.{1}) {0}_FFULLPARENTID", detailGrp[k].FlexNumber, value3);
                }
                else
                {
                    stringBuilder.AppendFormat(",1 {0}_FFLEXLEVEL,'.' {0}_FFULLPARENTID", detailGrp[k].FlexNumber);
                }
                list2.Add(string.Format(" t.{0}=v.{0}", detailGrp[k].FlexNumber));
                stringBuilder3.AppendFormat("(SELECT {0} from {1} AS {2}_tf ", stringBuilder.ToString(), value2, detailGrp[k].FlexNumber);
                if (flag)
                {
                    stringBuilder3.AppendFormat("left join {0}_L as {1}_tf_l on {1}_tf_l.{2}={1}_tf.{2} and {1}_tf_l.FLOCALEID={3} ", value2, detailGrp[k].FlexNumber, value4, base.Context.UserLocale.LCID);
                }
                if (flag2)
                {
                    stringBuilder3.AppendFormat("left join {0} {2}_btGrp on {2}_tf.{1}={2}_btGrp.FID ", detailGrp[k].GroupInfo.GroupTable, detailGrp[k].GroupInfo.GroupFieldName, detailGrp[k].FlexNumber);
                }
                if (flag3)
                {
                    stringBuilder3.AppendFormat("left join {0} {1}_btGrpLevel on {1}_btGrp.FID={1}_btGrpLevel.FID ", detailGrp[k].GroupInfo.GroupLevelTable, detailGrp[k].FlexNumber);
                }
                stringBuilder3.Append("where 1=1 ");
                if (num == 2 || num == 3)
                {
                    if (num == 2)
                    {
                        stringBuilder3.AppendFormat("AND {0}_tf.{1}={0}_tf.{2} AND EXISTS(SELECT 1 FROM {3} {4} WHERE {0}_tf.{2}={4}.{2} ", detailGrp[k].FlexNumber, value4, value7, value2, $"T{k}");
                    }
                    else
                    {
                        stringBuilder3.AppendFormat("AND EXISTS(SELECT 1 FROM {2} {3} WHERE {0}_tf.{1}={3}.{1} ", detailGrp[k].FlexNumber, value4, value2, $"T{k}");
                    }
                    if (IsByFlexScope)
                    {
                        stringBuilder3.Append(GetFlexScopeFilterSql($"T{k}", value3, detailGrp[k].BeginNo, detailGrp[k].EndNo));
                    }
                    else
                    {
                        stringBuilder3.Append(GetFlexNotScopeSql($"T{k}", value3, list3, detailGrp[k].AcctDems));
                    }
                    stringBuilder3.Append(CommonFunction.BaseDataOrgSeprString(base.Context, intAcctSystemId, intAccountOrgId, num, value5, $"T{k}"));
                    stringBuilder3.Append(")");
                }
                else if (IsByFlexScope)
                {
                    stringBuilder3.Append(GetFlexScopeFilterSql($"{detailGrp[k].FlexNumber}_tf", value3, detailGrp[k].BeginNo, detailGrp[k].EndNo));
                }
                else
                {
                    stringBuilder3.Append(GetFlexNotScopeSql($"{detailGrp[k].FlexNumber}_tf", value3, list3, detailGrp[k].AcctDems));
                }
                if (detailGrp[k].ValueType == "1")
                {
                    stringBuilder3.AppendFormat(" and {0}_tf.FID = '{1}' ", detailGrp[k].FlexNumber, value6);
                }
                if (CheckIsInsertUnInputFlex(detailGrp[k]))
                {
                    stringBuilder3.AppendFormat(" UNION ALL SELECT {0} ", stringBuilder2);
                }
                stringBuilder3.AppendFormat(")TBL{0},", k);
                stringBuilder2.Clear();
            }
            flexFields = string.Join(",", list);
            stringBuilder3.Remove(stringBuilder3.Length - 1, 1);
            stringBuilder3.Append(")T on ");
            if (isShowAnyFlex)
            {
                stringBuilder3.AppendFormat("{0}", string.Join(" or ", list2));
            }
            else
            {
                stringBuilder3.AppendFormat("{0}", string.Join(" and ", list2));
            }
            List<SqlObject> list4 = new List<SqlObject>();
            list4.Add(new SqlObject(stringBuilder3.ToString(), list3));
            string sql = $"CREATE CLUSTERED INDEX IDX_{tempTableName.Substring(4)} ON {tempTableName}(FID)";
            list4.Add(new SqlObject(sql, new List<SqlParam>()));
            DBUtils.ExecuteBatch(base.Context, list4);
            return tempTableName;
        }

        private string GetAnyFlexJoinSql(out string flexFields)
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<string> list = new List<string>();
            Dictionary<string, DynamicObject> dictionary = GetValueSources();
            int num = 0;
            StringBuilder stringBuilder2 = new StringBuilder();
            List<SqlParam> list2 = new List<SqlParam>();
            Dictionary<string, bool> dictionary2 = new Dictionary<string, bool>();
            FlexDetailReportService flexDetailReportService = new FlexDetailReportService();
            int maxFlexGroupCount = flexDetailReportService.GetMaxFlexGroupCount(base.Context, 100000);
            StringBuilder stringBuilder3 = new StringBuilder();
            stringBuilder3.AppendFormat("SELECT TOP {0} ISNULL(v.FID,-1) AS FID,", maxFlexGroupCount);
            StringBuilder stringBuilder4 = new StringBuilder();
            string tempTableName = CommonFunction.GetTempTableName(base.Context);
            for (int i = 0; i < detailGrp.Count; i++)
            {
                stringBuilder4.AppendFormat("v.{0},\r\n                        tbl{1}.{0}_fnumber,\r\n                        tbl{1}.{0}_fname,\r\n                        tbl{1}.{0}_fgroupid,\r\n                        tbl{1}.{0}_fparentgroupid,\r\n                        tbl{1}.{0}_fflexlevel,\r\n                        tbl{1}.{0}_ffullparentid,", detailGrp[i].FlexNumber, i);
            }
            stringBuilder3.AppendFormat(stringBuilder4.ToString().TrimEnd(','));
            stringBuilder3.AppendFormat(" INTO {0} FROM T_BD_FLEXITEMDETAILV V ", tempTableName);
            if (bShowNeverUseFlex)
            {
                stringBuilder3.AppendFormat(" RIGHT JOIN ");
            }
            else
            {
                stringBuilder3.AppendFormat(" LEFT JOIN ");
            }
            List<string> list3 = new List<string>();
            for (int j = 0; j < detailGrp.Count; j++)
            {
                if (!dictionary.TryGetValue(detailGrp[j].DetailType, out var value))
                {
                    continue;
                }
                string value2 = value.GetValue<string>("FTABLENAME");
                string value3 = value.GetValue<string>("FNUMBERFIELDNAME");
                string value4 = value.GetValue<string>("FPKFIELDNAME");
                string value5 = value.GetValue<string>("FORGFIELDNAME");
                string value6 = value.GetValue<string>("FVALUESOURCE");
                string value7 = value.GetValue<string>("FMASTERIDFIELDNAME");
                num = value.GetValue<int>("FSTRATEGYTYPE");
                bool flag;
                string text;
                if (detailGrp[j].ValueType == "0")
                {
                    flag = detailGrp[j].GroupInfo.LookupInfo.NameIsLocale;
                    text = detailGrp[j].GroupInfo.LookupInfo.NameFieldName;
                }
                else if (detailGrp[j].ValueType == "1")
                {
                    flag = true;
                    text = "FDATAVALUE";
                }
                else
                {
                    flag = true;
                    text = "FNAME";
                }
                stringBuilder.Clear();
                stringBuilder.AppendFormat("{0}_tf.{1} {0}", detailGrp[j].FlexNumber, value4);
                stringBuilder.AppendFormat(",{0}_tf.{1} {0}_FNUMBER", detailGrp[j].FlexNumber, value3);
                stringBuilder.AppendFormat(",{0}_tf{1}.{2} {0}_FNAME", detailGrp[j].FlexNumber, flag ? "_l" : "", text);
                string empty = string.Empty;
                if (detailGrp[j].ValueType == "1")
                {
                    stringBuilder2.AppendFormat(" ' ' as {0},null {0}_{1},N'{2}' {0}_{3},null {0}_FGROUPID, null {0}_FPARENTGROUPID, null {0}_FFLEXLEVEL,null {0}_FFULLPARENTID", detailGrp[j].FlexNumber, value3, GlRptConst.UnknownItemName, text);
                    empty = string.Format(" A.{0} <> '' OR A.{0} <>' ' ", detailGrp[j].FlexNumber);
                }
                else
                {
                    stringBuilder2.AppendFormat(" 0 as {0},null {0}_{1},N'{2}' {0}_{3},null {0}_FGROUPID, null {0}_FPARENTGROUPID, null {0}_FFLEXLEVEL,null {0}_FFULLPARENTID", detailGrp[j].FlexNumber, value3, GlRptConst.UnknownItemName, text);
                    empty = $"A.{detailGrp[j].FlexNumber} > 0";
                }
                string text2 = string.Format("UPDATE {0} SET ({1}_FNUMBER,{1}_FNAME)=(SELECT DISTINCT {2},{5}\r\n                                                                                FROM {3} T0 LEFT OUTER JOIN {3}_L T1 ON T0.{4} = T1.{4} AND T1.FLOCALEID={8}\r\n                                                                                INNER JOIN {0} A ON A.{1} = T0.{7}\r\n                                                                                AND (({6}) AND (A.{1}_FNUMBER IS NULL))) ", tempTableName, detailGrp[j].FlexNumber, value3, value2, value4, text, empty, (num == 2) ? value7 : value4, base.Context.UserLocale.LCID);
                string empty2 = string.Empty;
                empty2 = ((!(detailGrp[j].ValueType == "0")) ? string.Format(" WHERE {0} <> N' ' AND ({0}_FNUMBER IS NULL OR {0}_FNUMBER=N' ')", detailGrp[j].FlexNumber) : string.Format(" WHERE {0}>0 AND ({0}_FNUMBER IS NULL OR {0}_FNUMBER=N' ')", detailGrp[j].FlexNumber));
                list3.Add(text2 + empty2);
                bool flag2 = detailGrp[j].GroupInfo != null && detailGrp[j].GroupInfo.HasGroupField;
                bool flag3 = detailGrp[j].GroupInfo != null && detailGrp[j].GroupInfo.HasGroupLevelTable;
                list.Add(string.Format("{0}_FGROUPID,{0}_FPARENTGROUPID,{0}_FNUMBER,{0}_FNAME", detailGrp[j].FlexNumber));
                if (flag2)
                {
                    stringBuilder.AppendFormat(",{1}_tf.{0} {1}_FGROUPID,{1}_tf.{0} {1}_FPARENTGROUPID", detailGrp[j].GroupInfo.GroupFieldName, detailGrp[j].FlexNumber);
                }
                else
                {
                    stringBuilder.AppendFormat(",0 {0}_FGROUPID,0 {0}_FPARENTGROUPID", detailGrp[j].FlexNumber);
                }
                list.Add(string.Format("{0},{0}_FFLEXLEVEL,{0}_FFULLPARENTID", detailGrp[j].FlexNumber));
                if (flag3)
                {
                    stringBuilder.AppendFormat(",isnull({0}_btGrpLevel.FLEVEL,0)+1 {0}_FFLEXLEVEL,(isnull({0}_btGrpLevel.FFULLPARENTID,'.')||'.'|| {0}_tf.{1}) {0}_FFULLPARENTID", detailGrp[j].FlexNumber, value3);
                }
                else
                {
                    stringBuilder.AppendFormat(",1 {0}_FFLEXLEVEL,'.' {0}_FFULLPARENTID", detailGrp[j].FlexNumber);
                }
                stringBuilder3.AppendFormat("(SELECT {0} from {1} AS {2}_tf ", stringBuilder.ToString(), value2, detailGrp[j].FlexNumber);
                if (flag)
                {
                    stringBuilder3.AppendFormat("left join {0}_L as {1}_tf_l on {1}_tf_l.{2}={1}_tf.{2} and {1}_tf_l.FLOCALEID={3} ", value2, detailGrp[j].FlexNumber, value4, base.Context.UserLocale.LCID);
                }
                if (flag2)
                {
                    stringBuilder3.AppendFormat("left join {0} {2}_btGrp on {2}_tf.{1}={2}_btGrp.FID ", detailGrp[j].GroupInfo.GroupTable, detailGrp[j].GroupInfo.GroupFieldName, detailGrp[j].FlexNumber);
                }
                if (flag3)
                {
                    stringBuilder3.AppendFormat("left join {0} {1}_btGrpLevel on {1}_btGrp.FID={1}_btGrpLevel.FID ", detailGrp[j].GroupInfo.GroupLevelTable, detailGrp[j].FlexNumber);
                }
                stringBuilder3.Append("where 1=1 ");
                if (num == 2 || num == 3)
                {
                    if (num == 2)
                    {
                        stringBuilder3.AppendFormat("AND {0}_tf.{1}={0}_tf.{2} AND EXISTS(SELECT 1 FROM {3} {4} WHERE {0}_tf.{2}={4}.{2} ", detailGrp[j].FlexNumber, value4, value7, value2, $"T{j}");
                    }
                    else
                    {
                        stringBuilder3.AppendFormat("AND EXISTS(SELECT 1 FROM {2} {3} WHERE {0}_tf.{1}={3}.{1} ", detailGrp[j].FlexNumber, value4, value2, $"T{j}");
                    }
                    if (IsByFlexScope)
                    {
                        stringBuilder3.Append(GetFlexScopeFilterSql($"T{j}", value3, detailGrp[j].BeginNo, detailGrp[j].EndNo));
                    }
                    else
                    {
                        stringBuilder3.Append(GetFlexNotScopeSql($"T{j}", value3, list2, detailGrp[j].AcctDems));
                    }
                    stringBuilder3.Append(CommonFunction.BaseDataOrgSeprString(base.Context, intAcctSystemId, intAccountOrgId, num, value5, $"T{j}"));
                    stringBuilder3.Append(")");
                }
                else if (IsByFlexScope)
                {
                    stringBuilder3.Append(GetFlexScopeFilterSql($"{detailGrp[j].FlexNumber}_tf", value3, detailGrp[j].BeginNo, detailGrp[j].EndNo));
                }
                else
                {
                    stringBuilder3.Append(GetFlexNotScopeSql($"{detailGrp[j].FlexNumber}_tf", value3, list2, detailGrp[j].AcctDems));
                }
                if (detailGrp[j].ValueType == "1")
                {
                    stringBuilder3.AppendFormat(" and {0}_tf.FID = '{1}' ", detailGrp[j].FlexNumber, value6);
                }
                dictionary2[detailGrp[j].FlexNumber] = CheckIsInsertUnInputFlex(detailGrp[j]);
                if (dictionary2[detailGrp[j].FlexNumber])
                {
                    stringBuilder3.AppendFormat(" UNION ALL SELECT {0} ", stringBuilder2);
                }
                stringBuilder3.AppendFormat(")TBL{0} ON TBL{0}.{1}=V.{1} ", j, detailGrp[j].FlexNumber);
                if (j < detailGrp.Count - 1)
                {
                    stringBuilder3.Append(" left join ");
                }
                stringBuilder2.Clear();
            }
            List<string> list4 = new List<string>();
            int num2 = 0;
            foreach (DetailGroup item in detailGrp)
            {
                if (string.IsNullOrWhiteSpace(item.BeginNo) && string.IsNullOrWhiteSpace(item.EndNo) && dictionary2[detailGrp[num2].FlexNumber])
                {
                    if (!list4.Contains("v.fid=1"))
                    {
                        list4.Add("v.fid=1");
                    }
                    if (item.ValueType == "0")
                    {
                        list4.Add($" tbl{num2}.{item.FlexNumber}=0 ");
                    }
                    else
                    {
                        list4.Add(string.Format(" (tbl{0}.{1}='' or  tbl{0}.{1}=' ' )", num2, item.FlexNumber));
                    }
                }
                if (item.ValueType == "0")
                {
                    list4.Add($" tbl{num2}.{item.FlexNumber}>0 ");
                }
                else
                {
                    list4.Add(string.Format(" (tbl{0}.{1}<>'' or  tbl{0}.{1}<>' ' )", num2, item.FlexNumber));
                }
                num2++;
            }
            if (list4.Count > 0)
            {
                stringBuilder3.AppendFormat(" where ({0}) ", string.Join(" or ", list4));
            }
            flexFields = string.Join(",", list);
            List<SqlObject> list5 = new List<SqlObject>();
            list5.Add(new SqlObject(stringBuilder3.ToString(), list2));
            string sql = $"CREATE CLUSTERED INDEX IDX_{tempTableName.Substring(4)} ON {tempTableName}(FID)";
            list5.Add(new SqlObject(sql, new List<SqlParam>()));
            DBUtils.ExecuteBatch(base.Context, list5);
            DBUtils.ExecuteBatch(base.Context, list3, 50);
            return tempTableName;
        }

        private Dictionary<int, dynamic> GetAllAcctDct()
        {
            if (dctAcctName == null)
            {
                List<SqlParam> list = new List<SqlParam>();
                list.Add(new SqlParam("@IDS", KDDbType.udt_inttable, lstBookAcctIds.Distinct().ToArray()));
                list.Add(new SqlParam("@FLOCALEID", KDDbType.Int32, base.Context.UserLocale.LCID));
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("SELECT T.FACCTID,T.FNUMBER,L.FNAME,L.FFULLNAME,T.FDC,T.FPARENTID\r\n                        ,ISNULL(U.FPRECISION,0) AS FPRECISION, T.FISQUANTITIES, T.FLEVEL\r\n                        FROM T_BD_ACCOUNT T INNER JOIN (SELECT /*+ CARDINALITY(TBL {0})*/FID FROM TABLE(fn_StrSplit(@IDS, ',',1)) TBL) SP ON T.FACCTID=SP.FID\r\n                        LEFT JOIN T_BD_UNIT U ON T.FUNITID = U.FUNITID\r\n                        LEFT JOIN T_BD_ACCOUNT_L L ON T.FACCTID=L.FACCTID AND L.FLOCALEID=@FLOCALEID ", lstBookAcctIds.Count);
                dctAcctName = new Dictionary<int, object>();
                using IDataReader dataReader = DBUtils.ExecuteReader(base.Context, stringBuilder.ToString(), list);
                while (dataReader.Read())
                {
                    string empty = string.Empty;
                    string name = (showAllAct ? "FFULLNAME" : "FNAME");
                    string text = Convert.ToString(dataReader[name]);
                    int num = Convert.ToInt32(dataReader["FACCTID"]);
                    empty = ((!bNotShowAcctNumber) ? string.Format("{0} {1}", dataReader["FNUMBER"], text) : text);
                    if (string.IsNullOrWhiteSpace(empty))
                    {
                        empty = Convert.ToString(num);
                    }
                    dctAcctName.Add(num, new
                    {
                        Name = empty,
                        DC = Convert.ToInt16(dataReader["FDC"]),
                        Number = Convert.ToString(dataReader["FNUMBER"]),
                        ParentId = Convert.ToInt32(dataReader["FPARENTID"]),
                        Precision = ((Convert.ToChar(dataReader["FISQUANTITIES"]) == '1') ? Convert.ToInt16(dataReader["FPRECISION"]) : 0),
                        Level = Convert.ToInt32(dataReader["FLEVEL"])
                    });
                }
            }
            return dctAcctName;
        }

        private void SetIsStartAcctQty()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("SELECT COUNT(*) FROM T_BD_ACCOUNT A WHERE A.FACCTTBLID=@FACCTTBLID AND A.FISQUANTITIES='1' ");
            if (!forbidAcct)
            {
                stringBuilder.Append(" AND A.FFORBIDSTATUS='A' ");
            }
            if (!string.IsNullOrWhiteSpace(acctNumber))
            {
                string[] array = (from n in acctNumber.Split(',')
                                  where !string.IsNullOrWhiteSpace(n)
                                  select n).ToArray();
                if (array != null && array.Length > 0)
                {
                    stringBuilder.Append("AND (");
                    int num = 1;
                    string[] array2 = array;
                    foreach (string arg in array2)
                    {
                        stringBuilder.AppendFormat(" (A.FNUMBER = '{0}' OR A.FNUMBER LIKE '{0}.%') ", arg);
                        if (num < array.Length)
                        {
                            stringBuilder.Append(" OR ");
                        }
                        num++;
                    }
                    stringBuilder.Append(" ) ");
                }
            }
            List<SqlParam> list = new List<SqlParam>();
            list.Add(new SqlParam("@FACCTTBLID", KDDbType.Int32, AcctTableID));
            int num2 = DBUtils.ExecuteScalar(base.Context, stringBuilder.ToString(), 0, list.ToArray());
            blIsStartQty = ((num2 > 0) ? true : false);
        }

        private string GetAcctWhere(string account_alias = "acct")
        {
            string[] array = new string[0];
            if (!string.IsNullOrEmpty(acctNumber))
            {
                array = acctNumber.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            List<string> list = new List<string>();
            IEnumerable<string> flexNumbers = detailGrp.Select((DetailGroup p) => p.FlexNumber.ToUpperInvariant()).Distinct();
            list.Add(MulAcctBookFlexSubledgerService.GetAccountFlexSql(flexNumbers, account_alias));
            List<string> list2 = new List<string>();
            string[] array2 = array;
            foreach (string arg in array2)
            {
                list2.Add(string.Format("({1}.FNUMBER='{0}' or {1}.FNUMBER LIKE '{0}.%')", arg, account_alias));
            }
            if (list2.Count > 0)
            {
                list.Add(string.Format("({0})", string.Join(" or ", list2)));
            }
            return string.Join(" and ", list);
        }

        private string GetCurrencyName(long currencyId)
        {
            return BillPlugInBaseFun.GetSpecialCyName(base.Context, currencyId);
        }

        private Tuple<DateTime, DateTime> GetDateRangeByPeriod(long bookId, int year, int period)
        {
            List<SqlParam> paramList = new List<SqlParam>(new SqlParam[3]
            {
                new SqlParam("@BookId", KDDbType.Int64, bookId),
                new SqlParam("@Year", KDDbType.Int32, year),
                new SqlParam("@Period", KDDbType.Int32, period)
            });
            DateTime item = DateTime.MaxValue;
            DateTime item2 = DateTime.MaxValue;
            using (IDataReader dataReader = DBUtils.ExecuteReader(base.Context, "select cp.FPERIODSTARTDATE,cp.FPERIODENDDATE\r\n                 from t_bd_accountbook b \r\n                 left join t_fa_acctpolicy p on b.FACCTPOLICYID=p.FACCTPOLICYID\r\n                 left join T_BD_ACCOUNTCALENDAR c on p.FACCTCALENDARID=c.fid\r\n                 left join T_BD_ACCOUNTPERIOD cp on c.fid=cp.fid\r\n                 where b.FBOOKID=@BookId and cp.FYEAR=@Year and cp.FPERIOD=@Period", paramList))
            {
                if (dataReader != null && dataReader.Read())
                {
                    item = (DateTime)dataReader["FPERIODSTARTDATE"];
                    item2 = (DateTime)dataReader["FPERIODENDDATE"];
                }
            }
            return new Tuple<DateTime, DateTime>(item, item2);
        }

        public DataTable GetBalance(out DateTime startMonthDate)
        {
            YearPeriodBalanceObject queryYearPeriod = GetQueryYearPeriod(base.Context, bookId);
            StringBuilder stringBuilder = new StringBuilder();
            new Dictionary<string, object>();
            int num = 0;
            int num2 = 0;
            Tuple<int, int> yearPeriodByDate = CommonFunction.GetYearPeriodByDate(base.Context, bookId, startDate);
            int num3 = yearPeriodByDate.Item1 * 100 + yearPeriodByDate.Item2;
            int num4 = queryYearPeriod.InitYear * 100 + queryYearPeriod.InitPeriod;
            int num5 = queryYearPeriod.YearBalance * 100 + queryYearPeriod.PeriodBalance;
            if (num3 <= num4)
            {
                num = queryYearPeriod.InitYear;
                num2 = queryYearPeriod.InitPeriod;
            }
            else if (num3 <= num5)
            {
                num = yearPeriodByDate.Item1;
                num2 = yearPeriodByDate.Item2;
            }
            else if (yearPeriodByDate.Item1 > queryYearPeriod.YearBalance)
            {
                num = yearPeriodByDate.Item1;
                num2 = yearPeriodByDate.Item2;
            }
            else
            {
                num = queryYearPeriod.YearBalance;
                num2 = queryYearPeriod.PeriodBalance;
            }
            Tuple<DateTime, DateTime> dateRangeByPeriod = GetDateRangeByPeriod(bookId, num, num2);
            startMonthDate = dateRangeByPeriod.Item1;
            string text;
            string text2;
            if (notPostVercher)
            {
                VoucherPostParameters voucherPostParameters = new VoucherPostParameters();
                voucherPostParameters.PostType = GlVoucherPostType.PeriodScope;
                voucherPostParameters.BegingYear = num;
                voucherPostParameters.BegingPeriod = num2;
                voucherPostParameters.EndYear = num;
                voucherPostParameters.EndPeriod = num2;
                voucherPostParameters.IsCheckUncontinuity = false;
                voucherPostParameters.IsContinueWhenError = true;
                voucherPostParameters.IsContinueWhenUncontinuity = true;
                voucherPostParameters.BookId = bookId;
                voucherPostParameters.IsNoContainQty = false;
                if (currencyId > 0)
                {
                    voucherPostParameters.CurrencyId = currencyId;
                }
                else
                {
                    voucherPostParameters.CurrencyId = 0L;
                }
                if (lstBookAcctIds.Count() > 0)
                {
                    voucherPostParameters.LstAccountIds = lstBookAcctIds.Distinct().ToList();
                }
                voucherPostParameters.IsContainAdjustPeriod = !excludeAdjustVch;
                VirtualPostTemps voucherVirPostBalance = VoucherVirPostService.GetVoucherVirPostBalance(base.Context, voucherPostParameters);
                text = voucherVirPostBalance.BalanceTemp;
                text2 = voucherVirPostBalance.QtyBalanceTemp;
            }
            else
            {
                BalanceTableObject balanceTableName = BalanceAdjustService.GetBalanceTableName(base.Context, bookId, excludeAdjustVch);
                text = balanceTableName.BalanceTbl;
                text2 = balanceTableName.QtyBalanceTbl;
            }
            List<SqlParam> list = new List<SqlParam>();
            stringBuilder.AppendFormat("SELECT ISNULL(A.FYEAR,{0}) FYEAR,ISNULL(A.FPERIOD,{1}) FPERIOD,\r\n                            ISNULL(ACCT.FACCTID,0) FACCOUNTID,\r\n                            ISNULL(ACCT.FLEVEL,0) FLEVEL,\r\n                            ISNULL(ACCT.FPARENTID,0) FPARENTID,\r\n                            ISNULL(A.FCURRENCYID,{2}) FCURRENCYID,\r\n                            ISNULL(A.FDETAILID,2) FDETAILID,ACCT.FNUMBER,\r\n                            ISNULL(A.FBEGINBALANCE,0) FBEGINBALANCE,ISNULL(A.FBEGINBALANCEFOR,0) FBEGINBALANCEFOR,\r\n                            (ISNULL(A.FYTDDEBIT,0)-ISNULL(A.FDEBIT,0)) AS FYTDDEBIT,\r\n                            (ISNULL(A.FYTDDEBITFOR,0)-ISNULL(A.FDEBITFOR,0)) AS FYTDDEBITFOR,\r\n                            (ISNULL(A.FYTDCREDIT,0)-ISNULL(A.FCREDIT,0)) AS FYTDCREDIT,\r\n                            (ISNULL(A.FYTDCREDITFOR,0)-ISNULL(A.FCREDITFOR,0)) AS FYTDCREDITFOR,", num, num2, currencyId);
            if (blIsStartQty)
            {
                stringBuilder.AppendFormat("\r\n                            ISNULL(Y.FBEGINQTY,0) FBEGINQTY,\r\n                            ISNULL(Y.FDEBITQTY,0) FDEBITQTY,\r\n                            ISNULL(Y.FCREDITQTY,0) FCREDITQTY,\r\n                            (ISNULL(Y.FYTDDEBITQTY,0)-ISNULL(Y.FDEBITQTY,0)) AS FYTDDEBITQTY,\r\n                            (ISNULL(Y.FYTDCREDITQTY,0)-ISNULL(Y.FCREDITQTY,0)) AS FYTDCREDITQTY,");
            }
            else
            {
                stringBuilder.Append("0 FBEGINQTY,0 FDEBITQTY,0 FCREDITQTY,0FYTDDEBITQTY,0 FYTDCREDITQTY,");
            }
            stringBuilder.AppendFormat(" {0} ", flexFields);
            if (bShowNeverUseAcct || (bShowNeverUseAcct && bShowNeverUseFlex))
            {
                stringBuilder.AppendFormat(" FROM {0} ACCT INNER JOIN (SELECT /*+ CARDINALITY(TBL {1})*/FID FROM TABLE(fn_StrSplit(@IDS, ',',1)) TBL) SP ON ACCT.FACCTID=SP.FID\r\n                            LEFT JOIN {2} A ON A.FACCOUNTID=ACCT.FACCTID AND ACCT.FISDETAIL='1'\r\n                            AND A.FDETAILID>0 AND A.FYEARPERIOD=@FYEARPERIOD AND A.FCURRENCYID=@FCURRENCYID AND A.FACCOUNTBOOKID=@FACCOUNTBOOKID ", "T_BD_ACCOUNT", lstAcctIds.Count(), text);
            }
            else
            {
                stringBuilder.AppendFormat(" FROM {0} A INNER JOIN (SELECT /*+ CARDINALITY(TBL {1})*/FID FROM TABLE(fn_StrSplit(@IDS, ',',1)) TBL) SP ON A.FACCOUNTID=SP.FID\r\n                            INNER JOIN {2} ACCT ON A.FACCOUNTID=ACCT.FACCTID AND ACCT.FISDETAIL='1' \r\n                            AND A.FDETAILID>0 AND A.FYEARPERIOD=@FYEARPERIOD AND A.FCURRENCYID=@FCURRENCYID AND A.FACCOUNTBOOKID=@FACCOUNTBOOKID ", text, lstAcctIds.Count(), "T_BD_ACCOUNT");
            }
            list.Add(new SqlParam("@IDS", KDDbType.udt_inttable, lstAcctIds.ToArray()));
            list.Add(new SqlParam("@FYEARPERIOD", KDDbType.Int32, num * 100 + num2));
            list.Add(new SqlParam("@FCURRENCYID", KDDbType.Int32, currencyId));
            list.Add(new SqlParam("@FACCOUNTBOOKID", KDDbType.Int32, bookId));
            if (blIsStartQty)
            {
                stringBuilder.AppendFormat(" LEFT JOIN {0} Y ON A.FACCOUNTBOOKID=Y.FACCOUNTBOOKID AND A.FYEAR=Y.FYEAR AND A.FPERIOD=Y.FPERIOD\r\n                                AND A.FACCOUNTID=Y.FACCOUNTID AND A.FDETAILID=Y.FDETAILID AND A.FCURRENCYID=Y.FCURRENCYID ", text2);
            }
            if (!forbidAcct)
            {
                stringBuilder.AppendFormat(" {0} ", new AccountFilterService().GetForbidSqlString("ACCT", bookId));
            }
            if (bShowNeverUseFlex || (bShowNeverUseAcct && bShowNeverUseFlex))
            {
                stringBuilder.AppendFormat(" RIGHT ");
            }
            else if (bShowNeverUseAcct)
            {
                stringBuilder.AppendFormat(" LEFT ");
            }
            else
            {
                stringBuilder.AppendFormat(" INNER ");
            }
            stringBuilder.AppendFormat(" JOIN {0} AS DETAIL ON A.FDETAILID=DETAIL.FID", strFlexTmpTableName);
            stringBuilder.AppendLine(" WHERE  1=1 ");
            if (lstBaseDataTempTable != null && lstBaseDataTempTable.Count > 0)
            {
                foreach (BaseDataTempTable item in lstBaseDataTempTable)
                {
                    if (item.BaseDataFormId == "BD_Account")
                    {
                        stringBuilder.AppendFormat(" AND EXISTS (SELECT 1 FROM {0} ACTDR WHERE ACTDR.{1}=A.FACCOUNTID)", item.TempTable, item.PKFieldName);
                    }
                }
            }
            DataTable dataTable = DBUtils.ExecuteDataSet(base.Context, stringBuilder.ToString(), list).Tables[0];
            if (bShowNeverUseAcct && bShowNeverUseFlex)
            {
                AppendBalanceRecond(dataTable, num, num2);
            }
            if (notPostVercher)
            {
                CommonFunction.DropTempTable(base.Context, new List<string> { text, text2 }, immediate: true);
            }
            return dataTable;
        }

        private YearPeriodBalanceObject GetQueryYearPeriod(Context ctx, long bookId)
        {
            YearPeriodBalanceObject yearPeriodBalanceObject = new YearPeriodBalanceObject();
            string strSQL = "SELECT B.FCURRENTYEAR,B.FCURRENTPERIOD,B.FSTARTYEAR,B.FSTARTPERIOD,A.FPERIODCOUNT FROM T_BD_ACCOUNTCALENDAR A\r\n                        INNER JOIN T_BD_ACCOUNTBOOK B ON B.FPERIODID=A.FID WHERE B.FBOOKID=@FBOOKID";
            SqlParam param = new SqlParam("@FBOOKID", KDDbType.Int64, bookId);
            using IDataReader dataReader = DBUtils.ExecuteReader(ctx, strSQL, param);
            if (dataReader.Read())
            {
                yearPeriodBalanceObject.YearBalance = Convert.ToInt16(dataReader["FCURRENTYEAR"]);
                yearPeriodBalanceObject.PeriodBalance = Convert.ToInt16(dataReader["FCURRENTPERIOD"]);
                yearPeriodBalanceObject.PeriodCount = dataReader.GetValue<int>("FPERIODCOUNT");
                yearPeriodBalanceObject.InitYear = Convert.ToInt16(dataReader["FSTARTYEAR"]);
                yearPeriodBalanceObject.InitPeriod = Convert.ToInt16(dataReader["FSTARTPERIOD"]);
                return yearPeriodBalanceObject;
            }
            return yearPeriodBalanceObject;
        }

        private bool CheckIsInsertUnInputFlex(DetailGroup detailGroup)
        {
            if ((!string.IsNullOrWhiteSpace(detailGroup.BeginNo) && !string.IsNullOrWhiteSpace(detailGroup.EndNo)) || (detailGroup.AcctDems != null && detailGroup.AcctDems.Count > 0))
            {
                return false;
            }
            string empty = string.Empty;
            empty = ((!(detailGroup.ValueType == "1")) ? $" C.{detailGroup.FlexNumber}=0 " : ((base.Context.DatabaseType != DatabaseType.MS_SQL_Server) ? $" C.{detailGroup.FlexNumber}=' ' " : $" C.{detailGroup.FlexNumber}='' "));
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("SELECT 1 FROM T_GL_VOUCHERENTRY E INNER JOIN T_GL_VOUCHER V ON E.FVOUCHERID=V.FVOUCHERID INNER JOIN T_BD_FLEXITEMDETAILV C ON E.FDETAILID = C.FID\r\n                                 INNER JOIN {0} AC ON AC.FID=E.FACCOUNTID ", StringUtils.GetSqlWithCardinality(lstAcctIds.Count, "@FID1", 1));
            stringBuilder.AppendFormat(" WHERE E.FDETAILID > 0 ");
            stringBuilder.AppendFormat(" AND EXISTS (SELECT DISTINCT B.FACCTID FROM T_BD_ACCOUNTFLEXENTRY B WHERE B.FDATAFIELDNAME='{0}' AND B.FINPUTTYPE = 2 AND B.FACCTID = E.FACCOUNTID)\r\n                                                       AND ({1})", detailGroup.FlexNumber, empty);
            stringBuilder.Append(QueryVchCondition());
            stringBuilder.AppendLine(" UNION ");
            stringBuilder.AppendFormat(" SELECT 1 FROM T_GL_BALANCE BAL INNER JOIN T_BD_FLEXITEMDETAILV C ON BAL.FDETAILID=C.FID\r\n                                  INNER JOIN {0} AC ON AC.FID=BAL.FACCOUNTID ", StringUtils.GetSqlWithCardinality(lstAcctIds.Count, "@FID2", 1));
            stringBuilder.AppendFormat(" WHERE BAL.FDETAILID > 0 ");
            stringBuilder.AppendFormat(" AND EXISTS (SELECT DISTINCT B.FACCTID FROM T_BD_ACCOUNTFLEXENTRY B WHERE B.FDATAFIELDNAME='{0}' AND B.FINPUTTYPE = 2 AND B.FACCTID = BAL.FACCOUNTID)\r\n                                                         AND ({1})", detailGroup.FlexNumber, empty);
            stringBuilder.AppendFormat(" AND BAL.FACCOUNTBOOKID={0} ", bookId);
            if (currencyId > 0)
            {
                stringBuilder.AppendFormat(" AND BAL.FCURRENCYID={0} ", currencyId);
            }
            SqlParam[] paramList = new SqlParam[2]
            {
                new SqlParam("@FID1", KDDbType.udt_inttable, lstAcctIds.Distinct().ToArray()),
                new SqlParam("@FID2", KDDbType.udt_inttable, lstAcctIds.Distinct().ToArray())
            };
            return DBUtils.ExecuteScalar(base.Context, stringBuilder.ToString(), 0, paramList) == 1;
        }

        private string QueryVchCondition()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(" AND V.FACCOUNTBOOKID={0}", bookId);
            stringBuilder.Append(" AND V.FINVALID='0' AND V.FDOCUMENTSTATUS<>'Z' ");
            if (currencyId > 0)
            {
                stringBuilder.AppendFormat(" AND E.FCURRENCYID={0} ", currencyId);
            }
            return stringBuilder.ToString();
        }

        private void SetAcctId()
        {
            AccountFilterService accountFilterService = new AccountFilterService();
            lstBookAcctIds = accountFilterService.GetViewableAcctIdByBookId(base.Context, bookId, forbidAcct, isAllDocState: true);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("SELECT A.FACCTID,A.FISQUANTITIES FROM T_BD_ACCOUNT A \r\n                                        INNER JOIN (SELECT /*+ CARDINALITY(TBL {0})*/FID FROM TABLE(fn_StrSplit(@IDS, ',',1)) TBL) SP ON A.FACCTID=SP.FID\r\n                                        WHERE A.FACCTTBLID=@FACCTTBLID ", lstBookAcctIds.Count());
            if (!string.IsNullOrWhiteSpace(acctNumber))
            {
                string[] array = (from n in acctNumber.Split(',')
                                  where !string.IsNullOrWhiteSpace(n)
                                  select n).ToArray();
                if (array != null && array.Length > 0)
                {
                    stringBuilder.Append("AND (");
                    int num = 1;
                    string[] array2 = array;
                    foreach (string arg in array2)
                    {
                        stringBuilder.AppendFormat(" (A.FNUMBER = '{0}' OR A.FNUMBER LIKE '{0}.%') ", arg);
                        if (num < array.Length)
                        {
                            stringBuilder.Append(" OR ");
                        }
                        num++;
                    }
                    stringBuilder.Append(" ) ");
                }
            }
            string text = string.Empty;
            foreach (DetailGroup item in detailGrp)
            {
                text += $"'{item.FlexNumber}',";
            }
            text = text.TrimEnd(',');
            if (isShowAnyFlex)
            {
                stringBuilder.AppendFormat(" AND EXISTS (SELECT 1 FROM T_BD_ACCOUNTFLEXENTRY B WHERE B.FDATAFIELDNAME IN({0}) AND B.FACCTID = A.FACCTID) ", text);
            }
            else
            {
                stringBuilder.AppendFormat(" AND EXISTS (SELECT DISTINCT B.FACCTID FROM T_BD_ACCOUNTFLEXENTRY B WHERE B.FDATAFIELDNAME IN({0}) AND B.FACCTID = A.FACCTID\r\n                                        GROUP BY B.FACCTID HAVING COUNT(1)={1})", text, detailGrp.Count);
            }
            List<SqlParam> list = new List<SqlParam>();
            list.Add(new SqlParam("@FACCTTBLID", KDDbType.Int32, AcctTableID));
            list.Add(new SqlParam("@IDS", KDDbType.udt_inttable, lstBookAcctIds.Distinct().ToArray()));
            long num2 = 0L;
            lstAcctIds = new List<long>();
            using (IDataReader dataReader = DBUtils.ExecuteReader(base.Context, stringBuilder.ToString(), list))
            {
                while (dataReader.Read())
                {
                    num2 = Convert.ToInt64(dataReader["FACCTID"]);
                    if (!lstAcctIds.Contains(num2))
                    {
                        lstAcctIds.Add(num2);
                    }
                }
            }
            if (lstAcctIds.Count == 0)
            {
                lstAcctIds.Add(0L);
            }
        }

        private void AppendBalanceRecond(DataTable dtBal, int actYear, int actPeriod)
        {
            List<int> list = (from f in dtBal.AsEnumerable()
                              where f["FACCOUNTID"] != DBNull.Value
                              select Convert.ToInt32(f["FACCOUNTID"])).Distinct().ToList();
            List<int> list2 = new List<int>();
            int num = 0;
            foreach (long lstAcctId in lstAcctIds)
            {
                num = Convert.ToInt32(lstAcctId);
                if (!list.Contains(num))
                {
                    list2.Add(num);
                }
            }
            Dictionary<int, object> allAcctDct = GetAllAcctDct();
            foreach (int item in list2)
            {
                dynamic val = allAcctDct[item];
                DataRow dataRow = dtBal.NewRow();
                dataRow["FYEAR"] = actYear;
                dataRow["FPERIOD"] = actPeriod;
                dataRow["FACCOUNTID"] = item;
                dataRow["FLEVEL"] = (object)val.Level;
                dataRow["FPARENTID"] = (object)val.ParentId;
                dataRow["FCURRENCYID"] = currencyId;
                dataRow["FDETAILID"] = 2;
                dataRow["FNUMBER"] = (object)val.Number;
                dataRow["FBEGINBALANCE"] = 0;
                dataRow["FBEGINBALANCEFOR"] = 0;
                dataRow["FYTDDEBIT"] = 0;
                dataRow["FYTDDEBITFOR"] = 0;
                dataRow["FYTDCREDIT"] = 0;
                dataRow["FYTDCREDITFOR"] = 0;
                dataRow["FBEGINQTY"] = 0;
                dataRow["FDEBITQTY"] = 0;
                dataRow["FCREDITQTY"] = 0;
                dataRow["FYTDDEBITQTY"] = 0;
                dataRow["FYTDCREDITQTY"] = 0;
                dtBal.Rows.Add(dataRow);
            }
        }

        private DataTable SumDataByFlexField(DataTable dt)
        {
            DataTable tableScheme = GetTableScheme();
            IEnumerable<IGrouping<object, DataRow>> enumerable = from r in dt.AsEnumerable()
                                                                 group r by new
                                                                 {
                                                                     flexKey = GetFlexGrpKeyVal(r),
                                                                     AcctName = r.GetField<string>("AcctName"),
                                                                     DC = r.GetField<int>("DC"),
                                                                     AcctLevel = r.GetField<int>("FLEVEL"),
                                                                     parentId = r.GetField<int>("FPARENTID"),
                                                                     RowType = r.GetField<int>("RowType")
                                                                 };
            foreach (IGrouping<object, DataRow> item in enumerable)
            {
                DataRow dataRow = tableScheme.NewRow();
                dynamic val = ((dynamic)item.Key).flexKey.Split(new string[1] { GlRptConst.StrDetailSplitTag }, StringSplitOptions.RemoveEmptyEntries);
                foreach (dynamic item2 in val)
                {
                    dynamic val2 = item2.Split(';');
                    if (!((val2.Length < 2 || string.IsNullOrWhiteSpace(val2[1])) ? true : false))
                    {
                        dataRow[val2[0]] = item2.Substring(item2.IndexOf(';') + 1);
                    }
                }
                dataRow["AcctName"] = (object)((dynamic)item.Key).AcctName;
                dataRow["DC"] = (object)((dynamic)item.Key).DC;
                dataRow["FLEVEL"] = (object)((dynamic)item.Key).AcctLevel;
                dataRow["FPARENTID"] = (object)((dynamic)item.Key).parentId;
                dataRow["FIDENTITYID"] = 0;
                dataRow["detailId"] = 0;
                dataRow["RowType"] = (object)((dynamic)item.Key).RowType;
                dataRow["F_DR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_DR"));
                dataRow["F_CR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_CR"));
                dataRow["F_SR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_SR"));
                dataRow["F_ER"] = item.Sum((DataRow r) => r.GetField<decimal>("F_ER"));
                dataRow["F_DYR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_DYR"));
                dataRow["F_CYR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_CYR"));
                dataRow["F_S"] = item.Sum((DataRow r) => r.GetField<decimal>("F_S"));
                dataRow["F_D"] = item.Sum((DataRow r) => r.GetField<decimal>("F_D"));
                dataRow["F_C"] = item.Sum((DataRow r) => r.GetField<decimal>("F_C"));
                dataRow["F_E"] = item.Sum((DataRow r) => r.GetField<decimal>("F_E"));
                dataRow["F_DY"] = item.Sum((DataRow r) => r.GetField<decimal>("F_DY"));
                dataRow["F_CY"] = item.Sum((DataRow r) => r.GetField<decimal>("F_CY"));
                dataRow["F_SQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_SQ"));
                dataRow["F_DQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_DQ"));
                dataRow["F_CQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_CQ"));
                dataRow["F_EQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_EQ"));
                dataRow["F_DYQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_DYQ"));
                dataRow["F_CYQ"] = item.Sum((DataRow r) => r.GetField<decimal>("F_CYQ"));
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SCR")) > 0m)
                {
                    dataRow["F_SDR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SCR"));
                    dataRow["F_SCR"] = 0;
                }
                else
                {
                    dataRow["F_SDR"] = 0;
                    dataRow["F_SCR"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_SDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SCR")));
                }
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_ECR")) > 0m)
                {
                    dataRow["F_EDR"] = item.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_ECR"));
                    dataRow["F_ECR"] = 0;
                }
                else
                {
                    dataRow["F_EDR"] = 0;
                    dataRow["F_ECR"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_EDR")) - item.Sum((DataRow r) => r.GetField<decimal>("F_ECR")));
                }
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SC")) > 0m)
                {
                    dataRow["F_SD"] = item.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SC"));
                    dataRow["F_SC"] = 0;
                }
                else
                {
                    dataRow["F_SD"] = 0;
                    dataRow["F_SC"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_SD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SC")));
                }
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EC")) > 0m)
                {
                    dataRow["F_ED"] = item.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EC"));
                    dataRow["F_EC"] = 0;
                }
                else
                {
                    dataRow["F_ED"] = 0;
                    dataRow["F_EC"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_ED")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EC")));
                }
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SQC")) > 0m)
                {
                    dataRow["F_SQD"] = item.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SQC"));
                    dataRow["F_SQC"] = 0;
                }
                else
                {
                    dataRow["F_SQD"] = 0;
                    dataRow["F_SQC"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_SQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_SQC")));
                }
                if (item.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EQC")) > 0m)
                {
                    dataRow["F_EQD"] = item.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EQC"));
                    dataRow["F_EQC"] = 0;
                }
                else
                {
                    dataRow["F_EQD"] = 0;
                    dataRow["F_EQC"] = -1m * (item.Sum((DataRow r) => r.GetField<decimal>("F_EQD")) - item.Sum((DataRow r) => r.GetField<decimal>("F_EQC")));
                }
                tableScheme.Rows.Add(dataRow);
            }
            return tableScheme;
        }

        private void InitFlexItemPropertyInfo()
        {
            if (dctFlexItemProperty != null)
            {
                return;
            }
            string strSQL = "SELECT F.FVALUESOURCE,F.FFLEXNUMBER,L.FNAME FROM T_BD_FLEXITEMPROPERTY F LEFT JOIN T_BD_FLEXITEMPROPERTY_L L ON F.FID=L.FID AND L.FLOCALEID=@FLOCALEID \r\n                                    WHERE F.FDOCUMENTSTATUS='C'";
            List<SqlParam> list = new List<SqlParam>();
            list.Add(new SqlParam("@FLOCALEID", KDDbType.Int32, base.Context.UserLocale.LCID));
            Dictionary<string, FlexItemPropertySubObject> dictionary = new Dictionary<string, FlexItemPropertySubObject>();
            FlexItemPropertySubObject flexItemPropertySubObject = null;
            using (IDataReader dataReader = DBUtils.ExecuteReader(base.Context, strSQL, list))
            {
                while (dataReader.Read())
                {
                    flexItemPropertySubObject = new FlexItemPropertySubObject();
                    flexItemPropertySubObject.Number = dataReader["FFLEXNUMBER"].ToString();
                    flexItemPropertySubObject.Name = dataReader["FNAME"].ToString();
                    dictionary[dataReader["FVALUESOURCE"].ToString()] = flexItemPropertySubObject;
                }
            }
            dctFlexItemProperty = dictionary;
        }

        private List<DetailGroup> SetMutilFlexGroupInfo(List<DetailGroup> lstDetailGroupInfo)
        {
            GLReportFilterService gLReportFilterService = new GLReportFilterService();
            useDetailGroup = true;
            foreach (DetailGroup item in lstDetailGroupInfo)
            {
                item.GroupInfo = gLReportFilterService.GetBaseDataTypeGroupInfo(base.Context, item.DetailType);
                if (item.GroupInfo.LookupInfo == null)
                {
                    item.ValueType = "1";
                    item.FlexNumber = dctFlexItemProperty[item.DetailType].Number;
                }
                else
                {
                    item.FlexNumber = item.GroupInfo.LookupInfo.FlexNumber;
                    item.ValueType = "0";
                    item.DetailType = item.GroupInfo.LookupInfo.FormId;
                }
                if (item.AcctDems == null || item.AcctDems.Count <= 0)
                {
                    continue;
                }
                for (int num = item.AcctDems.Count - 1; num >= 0; num--)
                {
                    if (item.AcctDems[num].IsNullOrEmptyOrWhiteSpace())
                    {
                        item.AcctDems.RemoveAt(num);
                    }
                }
            }
            return lstDetailGroupInfo;
        }

        private List<DetailGroup> SetSingleFlexGroupInfo(DetailGroup objDetailGroup, string strFlexSourceNumber)
        {
            if (objDetailGroup == null)
            {
                objDetailGroup = new DetailGroup();
            }
            useDetailGroup = false;
            strDetail = strFlexSourceNumber;
            GLReportFilterService gLReportFilterService = new GLReportFilterService();
            BaseDataTypeGroupInfo baseDataTypeGroupInfo = gLReportFilterService.GetBaseDataTypeGroupInfo(base.Context, strFlexSourceNumber);
            if (baseDataTypeGroupInfo.LookupInfo == null)
            {
                objDetailGroup.ValueType = "1";
                objDetailGroup.DetailType = strFlexSourceNumber;
                objDetailGroup.FlexNumber = dctFlexItemProperty[strFlexSourceNumber].Number;
            }
            else
            {
                objDetailGroup.GroupInfo = baseDataTypeGroupInfo;
                objDetailGroup.ValueType = "0";
                objDetailGroup.FlexNumber = baseDataTypeGroupInfo.LookupInfo.FlexNumber;
                objDetailGroup.DetailType = objDetailGroup.GroupInfo.LookupInfo.FormId;
            }
            objDetailGroup.BeginNo = startFlexNo;
            objDetailGroup.EndNo = endFlexNo;
            List<DetailGroup> list = new List<DetailGroup>();
            list.Add(objDetailGroup);
            return list;
        }

        private string GetFlexScopeFilterSql(string strTableAs, string strNumberFieldName, string strBeginNumber, string strEndNumber)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(strBeginNumber))
            {
                stringBuilder.AppendFormat(" AND {0}.{1}>='{2}' ", strTableAs, strNumberFieldName, strBeginNumber);
            }
            if (!string.IsNullOrWhiteSpace(strEndNumber))
            {
                stringBuilder.AppendFormat(" AND {0}.{1}<='{2}' ", strTableAs, strNumberFieldName, strEndNumber);
            }
            return stringBuilder.ToString();
        }

        private string GetFlexNotScopeSql(string strTableAs, string strNumberFieldName, List<SqlParam> lstParam, List<string> lstFlexNumbers)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (lstFlexNumbers != null && lstFlexNumbers.Count != 0)
            {
                if (lstFlexNumbers.Count == 1)
                {
                    stringBuilder.AppendFormat(" AND {0}.{1}='{2}' ", strTableAs, strNumberFieldName, lstFlexNumbers[0]);
                }
                else
                {
                    StringBuilder stringBuilder2 = new StringBuilder();
                    foreach (string lstFlexNumber in lstFlexNumbers)
                    {
                        stringBuilder2.AppendFormat("'{0}',", lstFlexNumber);
                    }
                    stringBuilder.AppendFormat(" AND {0}.{1} IN({2}) ", strTableAs, strNumberFieldName, stringBuilder2.ToString().TrimEnd(','));
                }
            }
            return stringBuilder.ToString();
        }

        private SqlObject GetFlexNotScopeSqlObject(string strTableAs, string strNumberFieldName, string[] strFlexNumbers)
        {
            return new SqlObject(string.Format(" AND EXISTS ( SELECT 1 FROM ({2}) FN_{0} WHERE FN_{0}.FID={0}.{1} )", strTableAs, strNumberFieldName, StringUtils.GetSqlWithCardinality(strFlexNumbers.Length, $"@FNUMBER_{strTableAs}", 3)), new SqlParam($"@FNUMBER_{strTableAs}", KDDbType.udt_nvarchartable, strFlexNumbers));
        }

        private void BuildFlexItemTableObject()
        {
            bool isByFlexScope = true;
            if (IsByFlexScope)
            {
                return;
            }
            foreach (DetailGroup item2 in detailGrp)
            {
                if (useDetailGroup)
                {
                    if (item2.AcctDems != null && item2.AcctDems.Count != 0)
                    {
                        isByFlexScope = false;
                    }
                    continue;
                }
                if (string.IsNullOrWhiteSpace(FlexNumber))
                {
                    IsByFlexScope = true;
                    continue;
                }
                item2.AcctDems = new List<string>();
                string[] array = FlexNumber.Replace(",", ";").Split(';');
                string[] array2 = array;
                foreach (string item in array2)
                {
                    item2.AcctDems.Add(item);
                }
            }
            if (!IsByFlexScope && useDetailGroup)
            {
                IsByFlexScope = isByFlexScope;
            }
        }

        private void SetFieldTotalDragProperty(SettingField field, bool bShowSubTotal, bool reCalTotalRows)
        {
            if (reCalTotalRows)
            {
                field.IsShowTotal = false;
                field.IsAllowDrag = false;
            }
            else if (!bShowSubTotal)
            {
                field.IsShowTotal = false;
            }
        }

        private void GetCurrencyFormat()
        {
            if (!dctCurrencyFormat.ContainsKey(bookCurrencyId))
            {
                dctCurrencyFormat[bookCurrencyId] = CommonFunction.GetAmountDigits(base.Context, bookCurrencyId);
            }
            int num = ((currencyId == 0) ? bookCurrencyId : currencyId);
            if (!dctCurrencyFormat.ContainsKey(num))
            {
                dctCurrencyFormat[num] = CommonFunction.GetAmountDigits(base.Context, num);
            }
        }

        private void DelZerosByFlexAndAcct(DataTable dtTable, Func<DataRow, string> keyFunc)
        {
            if (!noBalance && showZero)
            {
                return;
            }
            var dictionary = (from r in dtTable.AsEnumerable()
                              where Convert.ToInt32(r["FLEVEL"]) <= acctLevel
                              select r).GroupBy(keyFunc).ToDictionary((IGrouping<string, DataRow> g) => g.Key, delegate (IGrouping<string, DataRow> g)
                              {
                                  decimal num2 = g.Sum((DataRow r) => Convert.ToDecimal(r["F_E"]));
                                  decimal num3 = g.Sum((DataRow r) => Convert.ToDecimal(r["F_ER"]));
                                  decimal dR = g.Sum((DataRow r) => Convert.ToDecimal(r["F_DR"]));
                                  decimal cR = g.Sum((DataRow r) => Convert.ToDecimal(r["F_CR"]));
                                  decimal d = g.Sum((DataRow r) => Convert.ToDecimal(r["F_D"]));
                                  decimal c = g.Sum((DataRow r) => Convert.ToDecimal(r["F_C"]));
                                  return new
                                  {
                                      D = d,
                                      C = c,
                                      DR = dR,
                                      CR = cR,
                                      E = num2,
                                      ER = num3,
                                      EER = num2 + num3
                                  };
                              });
            for (int num = dtTable.Rows.Count - 1; num >= 0; num--)
            {
                DataRow arg = dtTable.Rows[num];
                string key = keyFunc(arg);
                if (dictionary.ContainsKey(key))
                {
                    var anon = dictionary[key];
                    if (noBalance && ((currencyId == 0 && anon.E == 0m) || (currencyId == bookCurrencyId && anon.ER == 0m) || anon.EER == 0m))
                    {
                        dtTable.Rows.RemoveAt(num);
                    }
                    else if (!showZero && anon.DR + anon.CR == 0m && anon.D + anon.C == 0m)
                    {
                        dtTable.Rows.RemoveAt(num);
                    }
                }
            }
        }

        private string GetDelSumGroupKey(DataRow r)
        {
            return string.Format("{0}_{1}", GetFlexGrpKeyVal(r), r["AcctName"]);
        }

        private void RemoveNoNeededColumn(DataTable dtTable)
        {
            string[] array = flexFieldsArr;
            foreach (string text in array)
            {
                if (!text.Contains("FFLEXLEVEL") && !text.Contains("_FFULLPARENTID"))
                {
                    dtTable.Columns.Remove(text);
                }
            }
            dtTable.Columns.Remove("FLEVEL");
            dtTable.Columns.Remove("FPARENTID");
            dtTable.Columns.Remove("DC");
            dtTable.Columns.Remove("RowType");
        }


    }
}

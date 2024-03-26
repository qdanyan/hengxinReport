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
using System.Net;
using static Kingdee.BOS.Core.Const.BOSConst;
using System.Runtime.InteropServices.ComTypes;
using Kingdee.BOS.Core.Metadata.Util;

namespace ANY.HX.K3.Report
{
    [Description("ANY | 恒信销售利润报表")]
    [Kingdee.BOS.Util.HotUpdate]
    public class AnySaleProfitReport: SysReportBaseService
    {

        private string year;
        private string period;
        private int currency;
        private string currencyCondition;
        private string detail;
        private string fromDate;
        private string toDate;
        private int saleSeq=0;
        private int customerSeq=0;
        private int deptSeq=0;
        private bool notPostVercher;
        private string addcOnditions;

        public override void Initialize()

        {
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.BillKeyFieldName = "FIDENTITYID";
            this.ReportProperty.ReportName = new Kingdee.BOS.LocaleValue("销售利润分析报表");
            base.Initialize();
        }

        public override ReportTitles GetReportTitles(IRptParams filter)

        {
            var result = base.GetReportTitles(filter);
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (dyFilter != null)
            {
                if (result == null)
                {
                    result = new ReportTitles();
                }
                //设置报表title
                result.AddTitle("F_ANY_Date", Convert.ToString(dyFilter["F_ANY_Date"]));
                result.AddTitle("F_ANY_EndDate", Convert.ToString(dyFilter["F_ANY_EndDate"]));

            }
            return result;
        }


        private void InitFilter(DynamicObject param)
        {
            string currencyStr = Convert.ToString(param["F_ANY_Currency"]);
            if (!string.IsNullOrEmpty(currencyStr))
            {
                currency = Convert.ToInt32(currencyStr);
            }
            else
            {
                currency = 0;
            }
            DateTime fDate = Convert.ToDateTime(param["F_ANY_Date"]);
            DateTime fDateEnd = Convert.ToDateTime(param["F_ANY_EndDate"]);
            DynamicObject detailObject = param["F_ANY_Detail"] as DynamicObject;
            detail = Convert.ToString(detailObject["Name"]);
            notPostVercher = Convert.ToBoolean(param["F_ANY_NOTPOSTVOUCHER"]);
            if (!notPostVercher)
            {
                addcOnditions = " AND V.FPOSTED='1'";
            }
            else
            {
                addcOnditions = " ";
            }

            year = fDate.Year.ToString();
            period = fDate.Month.ToString();
            fromDate = fDate.ToString("yyyy-MM-dd");
            toDate = fDateEnd.ToString("yyyy-MM-dd");
            if (currency > 0)
            {
                currencyCondition = string.Format(" AND I.FCURRENCYID = {0}", currency);
            }
            else
            {
                currencyCondition = " ";
            }
            addcOnditions += currencyCondition;
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)

        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            InitFilter(customFilter);

            string strCreateTable = string.Format(@"
                /*dialect*/SELECT
	                ROW_NUMBER ( ) OVER ( ORDER BY ( SELECT NULL ) ) AS FIDENTITYID,
	                A.FNAME AS ACCOUNTNAME, C.FNAME AS CUSTOMER, H.FNAME AS SALEMAN,
	                D.FNAME AS FDEPT,E.FNAME AS FEXPENSE,
                CASE
		                WHEN A.FNAME = '主营业务收入' THEN
		                ISNULL( SUM ( I.FDEBIT ), 0 ) 
		                WHEN A.FNAME = '主营业务成本' THEN
		                ISNULL( SUM ( I.FCREDIT ), 0 ) 
		                WHEN A.FNAME = '销售费用' THEN
		                ISNULL( SUM ( I.FDEBIT ), 0 ) 
		                WHEN A.FNAME = '管理费用' THEN
		                ISNULL( SUM ( I.FDEBIT ), 0 )
	                END AS Amount 
                INTO {0}
                FROM
	                T_GL_VOUCHERENTRY I
	                LEFT JOIN T_GL_VOUCHER V ON ( I.FVOUCHERID= V.FVOUCHERID )
	                LEFT JOIN T_BD_FLEXITEMDETAILV F ON ( F.FID= I.FDETAILID )
	                LEFT JOIN T_BD_CUSTOMER_L C ON ( C.FCUSTID= F.FFLEX6 AND C.FLOCALEID= 2052 )
	                LEFT JOIN T_BD_OPERATORENTRY_L H ON ( H.FENTRYID=F.FF100006 AND H.FLOCALEID= 2052 )
	                LEFT JOIN T_BD_DEPARTMENT_L D ON ( D.FDEPTID= F.FFLEX5 AND D.FLOCALEID= 2052 )
                    LEFT JOIN T_BD_EXPENSE_L E ON ( E.FEXPID= F.FFLEX9 AND E.FLOCALEID= 2052 )
	                LEFT JOIN T_BD_ACCOUNT_L A ON ( A.FACCTID= I.FACCOUNTID AND A.FLOCALEID= 2052 )
                WHERE
                    FDATE >= '{1}' 
                    AND FDATE <= '{2}'
	                AND FACCOUNTID IN ( 4075, 4080, 4083, 148657 ) {3}
                GROUP BY A.FNAME, I.FACCOUNTID, C.FNAME, H.FNAME, D.FNAME,E.FNAME", tableName, fromDate, toDate, addcOnditions);

            DBUtils.ExecuteDynamicObject(this.Context, strCreateTable);

#region 拆分费用
            string detailCategory = string.Empty;
            switch (detail)
            {
                case "销售员":
                    detailCategory = "SALEMAN";
                    customerSeq = 1;
                    deptSeq = 2;
                    break;
                case "客户":
                    detailCategory = "CUSTOMER";
                    saleSeq = 1;
                    deptSeq = 2;
                    break;
                case "部门":
                    detailCategory = "FDEPT";
                    saleSeq = 1;
                    customerSeq = 2;
                    break;
                default:
                    throw new Exception("未知的拆分类型: " + detail);
            }

            string emptyCostSql = string.Format(@"
                SELECT SUM(Amount) as TotalCost 
                FROM {0} 
                WHERE {1} IS NULL AND ACCOUNTNAME = '销售费用'", tableName, detailCategory);
            double totalEmptyCost = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, emptyCostSql, 0));

            //管理费用
            string adminCostSql = string.Format(@"
                SELECT SUM(Amount) as TotalCost 
                FROM {0} 
                WHERE {1} IS NULL AND ACCOUNTNAME = '管理费用'", tableName, detailCategory);
            double totalAdminCost = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, adminCostSql, 0));

            //计算主营业务收入
            string totalIncomeSql = string.Format(@"
                SELECT SUM(Amount) as TotalIncome 
                FROM {0} 
                WHERE {1} IS NOT NULL AND ACCOUNTNAME = '主营业务收入'", tableName, detailCategory);
            double totalIncome = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, totalIncomeSql, 0));

            string categoryIncomeSql = string.Format(@"
                SELECT {1}, SUM(Amount) as CategoryIncome 
                FROM {0} 
                WHERE {1} IS NOT NULL AND ACCOUNTNAME = '主营业务收入'
                GROUP BY {1}", tableName, detailCategory);
            DataTable dtCategoryIncome = DBUtils.ExecuteDataSet(this.Context, categoryIncomeSql).Tables[0];


            //销售费用分摊
            foreach (DataRow row in dtCategoryIncome.Rows)
            {
                string category = row[detailCategory].ToString();
                double categoryIncome = Convert.ToDouble(row["CategoryIncome"]);
                double shareCost = totalEmptyCost * (categoryIncome / totalIncome);

                string insertSql = string.Format(@"
                    INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, {1}, Amount)
                    VALUES ((SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), '销售费用分摊', '{2}', {3})",
                    tableName, detailCategory, category, shareCost);
                DBUtils.Execute(this.Context, insertSql);
            }

            //管理费用分摊
            foreach (DataRow row in dtCategoryIncome.Rows)
            {
                string category = row[detailCategory].ToString();
                double categoryIncome = Convert.ToDouble(row["CategoryIncome"]);
                double shareCost = totalAdminCost * (categoryIncome / totalIncome);

                string insertSql = string.Format(@"
                    INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, {1}, Amount)
                    VALUES ((SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), '管理费用分摊', '{2}', {3})",
                    tableName, detailCategory, category, shareCost);
                DBUtils.Execute(this.Context, insertSql);
            }
#endregion

#region 主营业务成本拆分
            string mainCostSql = string.Format(@"
                SELECT SUM(Amount) as TotalCost 
                FROM {0} 
                WHERE {1} IS NULL AND ACCOUNTNAME = '主营业务成本'", tableName, detailCategory);
            double totalMainCost = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, mainCostSql, 0));

            // 使用之前计算的 totalIncome
            double totalMainIncome = totalIncome;

            string categoryMainIncomeSql = string.Format(@"
                SELECT {1}, SUM(Amount) as CategoryIncome 
                FROM {0} 
                WHERE {1} IS NOT NULL AND ACCOUNTNAME = '主营业务收入'
                GROUP BY {1}", tableName, detailCategory);
            DataTable dtCategoryMainIncome = DBUtils.ExecuteDataSet(this.Context, categoryMainIncomeSql).Tables[0];

            foreach (DataRow row in dtCategoryMainIncome.Rows)
            {
                string category = row[detailCategory].ToString();
                double categoryIncome = Convert.ToDouble(row["CategoryIncome"]);
                double shareCost = totalMainCost * (categoryIncome / totalMainIncome);

                string insertSql = string.Format(@"
                    INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, {1}, Amount)
                    VALUES ((SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), '主营业务成本分摊', '{2}', {3})",
                    tableName, detailCategory, category, shareCost);
                DBUtils.Execute(this.Context, insertSql);
            }
#endregion



            string strProfitSql = string.Format(@"
                INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, CUSTOMER, SALEMAN, FDEPT, Amount)
                SELECT (SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), 
                '利润', T.CUSTOMER, T.SALEMAN, T.FDEPT,
                (SUM(CASE WHEN ACCOUNTNAME = '主营业务收入' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '主营业务成本' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '销售费用' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '管理费用' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '管理费用分摊' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '主营业务成本分摊' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '销售费用分摊' THEN Amount ELSE 0 END))
                FROM {0} T
                GROUP BY T.CUSTOMER, T.SALEMAN, T.FDEPT", tableName);
            DBUtils.ExecuteDynamicObject(this.Context, strProfitSql);

            //计算毛利
            string strGrossProfitSql = string.Format(@"
                INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, CUSTOMER, SALEMAN, FDEPT, Amount)
                SELECT (SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), 
                '毛利', T.CUSTOMER, T.SALEMAN, T.FDEPT,
                (SUM(CASE WHEN ACCOUNTNAME = '主营业务收入' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '主营业务成本' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '主营业务成本分摊' THEN Amount ELSE 0 END))
                FROM {0} T
                GROUP BY T.CUSTOMER, T.SALEMAN, T.FDEPT", tableName);
            DBUtils.ExecuteDynamicObject(this.Context, strGrossProfitSql);

            string[] old_names = new string[] { "主营业务收入", "主营业务成本", "主营业务成本分摊", "毛利", "销售费用", "销售费用分摊", "管理费用", "管理费用分摊", "利润" };
            string[] new_names = new string[] { "6001 主营业务收入", "6401 主营业务成本", "6401.01 主营业务成本分摊", "6401.02 毛利", "6601 销售费用", "6601.01 销售费用分摊", "6602 管理费用", "6602.01 管理费用分摊", "6666 利润" };

            for (int i = 0; i < old_names.Length; i++)
            {
                string updateSql = string.Format("UPDATE {0} SET ACCOUNTNAME = '{1}' WHERE ACCOUNTNAME = '{2}'", tableName, new_names[i], old_names[i]);
                DBUtils.Execute(this.Context, updateSql);
            }


            DataTable reportSouce = DBUtils.ExecuteDataSet(this.Context, string.Format("SELECT * FROM {0}", tableName)).Tables[0];
            
            
            this.SettingInfo = new PivotReportSettingInfo();
            TextField field;
            DecimalField fieldData;
            //构造透视表列

            Dictionary<string, SettingField> settings = new Dictionary<string, SettingField>();
            List<string> order = new List<string>();

            // 创建设置
            settings["FDEPT"] = CreateSettingField("FDEPT", "部门", deptSeq);
            settings["SALEMAN"] = CreateSettingField("SALEMAN", "销售员", saleSeq);
            settings["CUSTOMER"] = CreateSettingField("CUSTOMER", "客户", customerSeq);

            // 确定添加顺序
            switch (detail)
            {
                case "销售员":
                    order = new List<string> { "SALEMAN", "FDEPT", "CUSTOMER" };
                    break;
                case "客户":
                    order = new List<string> { "CUSTOMER", "SALEMAN", "FDEPT" };
                    break;
                case "部门":
                    order = new List<string> { "FDEPT", "SALEMAN", "CUSTOMER" };
                    break;
                default:
                    throw new Exception("未知的细节类型: " + detail);
            }

            // 根据顺序添加设置
            foreach (string key in order)
            {
                this.SettingInfo.RowTitleFields.Add(settings[key]);
                this.SettingInfo.SelectedFields.Add(settings[key]);
            }

            // 创建设置的辅助方法
            SettingField CreateSettingField(string key, string name, int seq)
            {
                TextField field = new TextField
                {
                    Key = key,
                    FieldName = key,
                    Name = new LocaleValue(name)
                };
                return PivotReportSettingInfo.CreateColumnSettingField(field, seq);
            }



                

            ////FDEPT
            //field = new TextField();
            //field.Key = "FDEPT";
            //field.FieldName = "FDEPT";
            //field.Name = new LocaleValue("部门");
            //SettingField settingDept = PivotReportSettingInfo.CreateColumnSettingField(field, deptSeq);
            //this.SettingInfo.RowTitleFields.Add(settingDept);
            //this.SettingInfo.SelectedFields.Add(settingDept);

            ////SALEMAN
            //field = new TextField();
            //field.Key = "SALEMAN";
            //field.FieldName = "SALEMAN";
            //field.Name = new LocaleValue("业务员");
            //SettingField settingSaleman = PivotReportSettingInfo.CreateColumnSettingField(field, saleSeq);
            //this.SettingInfo.RowTitleFields.Add(settingSaleman);
            //this.SettingInfo.SelectedFields.Add(settingSaleman);

            ////CUSTOMER
            //field = new TextField();
            //field.Key = "CUSTOMER";
            //field.FieldName = "CUSTOMER";
            //field.Name = new LocaleValue("客户");
            //SettingField settingBillNo = PivotReportSettingInfo.CreateColumnSettingField(field, customerSeq);
            //this.SettingInfo.RowTitleFields.Add(settingBillNo);
            //this.SettingInfo.SelectedFields.Add(settingBillNo);




            //构造行
            field = new TextField();
            field.Key = "ACCOUNTNAME";
            field.FieldName = "ACCOUNTNAME";
            field.Name = new LocaleValue("科目");
            SettingField settingAccount = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.ColTitleFields.Add(settingAccount);
            this.SettingInfo.SelectedFields.Add(settingAccount);

            //EXPENSE
            field = new TextField();
            field.Key = "FEXPENSE";
            field.FieldName = "FEXPENSE";
            field.Name = new LocaleValue("费用项目");
            SettingField settingExpense = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.ColTitleFields.Add(settingExpense);
            this.SettingInfo.SelectedFields.Add(settingExpense);

            //构造数据
            fieldData = new DecimalField();
            fieldData.Key = "Amount";
            fieldData.FieldName = "Amount";
            fieldData.Name = new LocaleValue("金额");
            SettingField settingAmount = PivotReportSettingInfo.CreateDataSettingField(fieldData, 0, GroupSumType.Sum, "N2"); //N3表示3位小数
            this.SettingInfo.AggregateFields.Add(settingAmount);
            this.SettingInfo.SelectedFields.Add(settingAmount);
        }

    }
}

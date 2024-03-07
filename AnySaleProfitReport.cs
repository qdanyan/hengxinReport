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


namespace ANY.HX.K3.Report
{
    [Description("ANY | 恒信销售利润报表")]
    [Kingdee.BOS.Util.HotUpdate]
    public class AnySaleProfitReport: SysReportBaseService
    {

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
            }
            return result;
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)

        {
            string strCreateTable = string.Format(@"
                /*dialect*/SELECT
	                ROW_NUMBER ( ) OVER ( ORDER BY ( SELECT NULL ) ) AS FIDENTITYID,
	                A.FNAME AS ACCOUNTNAME, C.FNAME AS CUSTOMER, H.FNAME AS SALEMAN,
	                D.FNAME AS FDEPT,
                CASE
		                WHEN A.FNAME = '主营业务收入' THEN
		                ISNULL( SUM ( I.FDEBIT ), 0 ) 
		                WHEN A.FNAME = '主营业务成本' THEN
		                ISNULL( SUM ( I.FCREDIT ), 0 ) 
		                WHEN A.FNAME = '销售费用' THEN
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
	                LEFT JOIN T_BD_ACCOUNT_L A ON ( A.FACCTID= I.FACCOUNTID AND A.FLOCALEID= 2052 ) 
                WHERE
	                FYEAR = 2023 
	                AND FPERIOD = 9 
	                AND FACCOUNTID IN ( 4075, 4080, 4083 ) 
                GROUP BY A.FNAME, I.FACCOUNTID, C.FNAME, H.FNAME, D.FNAME", tableName);

            DBUtils.ExecuteDynamicObject(this.Context, strCreateTable);


            //业务员为空的销售费用
            string emptySalesmanCostSql = string.Format(@"
                SELECT SUM(Amount) as TotalCost 
                FROM {0} 
                WHERE SALEMAN IS NULL AND ACCOUNTNAME = '销售费用'", tableName);
            
            double totalEmptySalesmanCost = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, emptySalesmanCostSql, 0));
            
            //业务员不为空的销售费用
            string totalIncomeSql = string.Format(@"
                SELECT SUM(Amount) as TotalIncome 
                FROM {0} 
                WHERE SALEMAN IS NOT NULL AND ACCOUNTNAME = '主营业务收入'", tableName);
                        double totalIncome = Convert.ToDouble(DBUtils.ExecuteScalar(this.Context, totalIncomeSql, 0));

            string salesmanIncomeSql = string.Format(@"
                SELECT SALEMAN, SUM(Amount) as SalesmanIncome 
                FROM {0} 
                WHERE SALEMAN IS NOT NULL AND ACCOUNTNAME = '主营业务收入'
                GROUP BY SALEMAN", tableName);
            DataTable dtSalesmanIncome = DBUtils.ExecuteDataSet(this.Context, salesmanIncomeSql).Tables[0];

            foreach (DataRow row in dtSalesmanIncome.Rows)
            {
                string salesMan = row["SALEMAN"].ToString();
                double salesManIncome = Convert.ToDouble(row["SalesmanIncome"]);
                double shareCost = totalEmptySalesmanCost * (salesManIncome / totalIncome);

                string insertSql = string.Format(@"
                    INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, SALEMAN, FDEPT, Amount)
                    VALUES ((SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), '分摊费用', '{1}', '', {2})",
                    tableName, salesMan, shareCost);
                DBUtils.Execute(this.Context, insertSql);
            }

            string strProfitSql = string.Format(@"
                INSERT INTO {0} (FIDENTITYID, ACCOUNTNAME, CUSTOMER, SALEMAN, FDEPT, Amount)
                SELECT (SELECT ISNULL(MAX(FIDENTITYID), 0) + 1 FROM {0}), 
                '利润', T.CUSTOMER, T.SALEMAN, T.FDEPT,
                (SUM(CASE WHEN ACCOUNTNAME = '主营业务收入' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '主营业务成本' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '销售费用' THEN Amount ELSE 0 END) -
                SUM(CASE WHEN ACCOUNTNAME = '分摊费用' THEN Amount ELSE 0 END))
                FROM {0} T
                GROUP BY T.CUSTOMER, T.SALEMAN, T.FDEPT", tableName);
                        DBUtils.ExecuteDynamicObject(this.Context, strProfitSql);


            DataTable reportSouce = DBUtils.ExecuteDataSet(this.Context, string.Format("SELECT * FROM {0}", tableName)).Tables[0];
            this.SettingInfo = new PivotReportSettingInfo();
            TextField field;
            DecimalField fieldData;
            //构造透视表列
            //CUSTOMER
            field = new TextField();
            field.Key = "CUSTOMER";
            field.FieldName = "CUSTOMER";
            field.Name = new LocaleValue("客户");
            SettingField settingBillNo = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.RowTitleFields.Add(settingBillNo);
            this.SettingInfo.SelectedFields.Add(settingBillNo);

            //SALEMAN
            field = new TextField();
            field.Key = "SALEMAN";
            field.FieldName = "SALEMAN";
            field.Name = new LocaleValue("业务员");
            SettingField settingSaleman = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.RowTitleFields.Add(settingSaleman);
            this.SettingInfo.SelectedFields.Add(settingSaleman);

            //FDEPT
            field = new TextField();
            field.Key = "FDEPT";
            field.FieldName = "FDEPT";
            field.Name = new LocaleValue("部门");
            SettingField settingDept = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.RowTitleFields.Add(settingDept);
            this.SettingInfo.SelectedFields.Add(settingDept);


            //构造行
            field = new TextField();
            field.Key = "ACCOUNTNAME";
            field.FieldName = "ACCOUNTNAME";
            field.Name = new LocaleValue("科目");
            SettingField settingAccount = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.ColTitleFields.Add(settingAccount);
            this.SettingInfo.SelectedFields.Add(settingAccount);

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

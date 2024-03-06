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

                /*dialect*/SELECT TM.FID,TM.FBILLNO,TM.FCREATORID,TM.FCREATEDATE,TF.FALLAMOUNT,ml.FNAME as FMaterialName,TE.FENTRYID as FIDENTITYID INTO {0}

                FROM T_PUR_POORDER TM 

                INNER JOIN T_PUR_POORDERENTRY_F TF ON TM.FID = TF.FID

                INNER JOIN T_PUR_POORDERENTRY TE ON TM.FID = TE.FID

                inner join T_BD_MATERIAL m on te.FMATERIALID = m.FMATERIALID

                inner join T_BD_MATERIAL_L ml on m.FMATERIALID = ml.FMATERIALID

                WHERE 1=1 ", tableName);

            var custFilter = filter.FilterParameter.CustomFilter;
            if (custFilter != null && custFilter.DynamicObjectType.Properties.Contains("F_PAEZ_Date"))
            {
                var date = custFilter["F_PAEZ_Date"].ToString();
                if (!string.IsNullOrEmpty(date))
                {
                    string strDate = DateTime.Parse(date).ToString("yyyy-MM-dd");
                    strCreateTable += string.Format(" AND TM.FCREATEDATE >= '{0}'", strDate);
                }
            }

            //base.AfterCreateTempTable(tablename);
            DBUtils.ExecuteDynamicObject(this.Context, strCreateTable);

            DataTable reportSouce = DBUtils.ExecuteDataSet(this.Context, string.Format("SELECT * FROM {0}", tableName)).Tables[0];
            this.SettingInfo = new PivotReportSettingInfo();
            TextField field;
            DecimalField fieldData;
            //构造透视表列
            //FID
            field = new TextField();
            field.Key = "FBILLNO";
            field.FieldName = "FBILLNO";
            field.Name = new LocaleValue("单据编号");
            SettingField settingBillNo = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.RowTitleFields.Add(settingBillNo);
            this.SettingInfo.SelectedFields.Add(settingBillNo);

            //构造行
            field = new TextField();
            field.Key = "FMaterialName";
            field.FieldName = "FMaterialName";
            field.Name = new LocaleValue("物料名称");
            SettingField settingMaterial = PivotReportSettingInfo.CreateColumnSettingField(field, 0);
            this.SettingInfo.ColTitleFields.Add(settingMaterial);
            this.SettingInfo.SelectedFields.Add(settingMaterial);

            //构造数据
            fieldData = new DecimalField();
            fieldData.Key = "FALLAMOUNT";
            fieldData.FieldName = "FALLAMOUNT";
            fieldData.Name = new LocaleValue("金额");
            SettingField settingAmount = PivotReportSettingInfo.CreateDataSettingField(fieldData, 0, GroupSumType.Sum, "N3"); //N3表示3位小数           
            this.SettingInfo.AggregateFields.Add(settingAmount);
            this.SettingInfo.SelectedFields.Add(settingAmount);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.ServiceHelper;
using Kingdee.K3.FIN.CN.Business.PlugIn.Filter;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.K3.FIN.CN.Business.PlugIn;

namespace ANY.HX.K3.Report
{
    [Description("Any | 银行流水账（横向）过滤条件")]
    [HotUpdate]
    public class AnyBankDailyReportFilter : AbstractCommonFilterPlugIn
    {

        protected List<long> OrgIds
        {
            get
            {
                object value = this.View.Model.GetValue("FOrgId");
                if (value == null)
                {
                    return new List<long>();
                }
                DynamicObjectCollection dynamicObjectCollection = value as DynamicObjectCollection;
                if (dynamicObjectCollection == null)
                {
                    return new List<long>();
                }
                List<long> list = (from a in dynamicObjectCollection
                                   select Convert.ToInt64(a["OrgId_Id"])).ToList<long>();
                if (list.Count == 0)
                {
                    return new List<long>();
                }
                return list;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                this.View.Model.SetValue("FOrgId", value.Cast<object>().ToArray());
            }

        }

        private void BindCurrencyIds()
        {
            QueryBuilderParemeter para = new QueryBuilderParemeter
            {
                FormId = "BD_Currency",
                SelectItems = SelectorItemInfo.CreateItems("FName,FCURRENCYID"),
                FilterClauseWihtKey = string.Format("  FDOCUMENTSTATUS = 'C' AND FFORBIDSTATUS = 'A' and FLocaleid = {0}", base.Context.UserLocale.LCID)
            };
            DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (DynamicObject dynamicObject in dynamicObjectCollection)
            {
                dictionary.Add(dynamicObject["FCURRENCYID"].ToString(), dynamicObject["FName"].ToString());
            }
            CNCommonFunction.BindComboBox(base.Context, dictionary, this.View.GetFieldEditor<ComboFieldEditor>("FCurrencyIds", -1));
        }


        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.BindCurrencyIds();
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            DateTime systemTime = CommonServiceHelper.GetSystemTime(base.Context);
            string a;
            if ((a = e.Key.ToUpperInvariant()) != null)
            {
                if (a == "FBTNOK")
                {
                    e.Cancel |= !this.CheckDate();
                    return;
                }
                if (a == "FYESTERDAY")
                {
                    this.Model.SetValue("FStartDate", systemTime.Date.AddDays(-1));
                    this.Model.SetValue("FEndDate", systemTime.Date.AddDays(-1));
                    return;
                }
                
                if (a == "FCURRENTDAY")
                {
                    this.Model.SetValue("FStartDate", systemTime.Date);
                    this.Model.SetValue("FEndDate", systemTime.Date);
                    return;
                }
                if (a == "FCURRENTMONTH")
                {
                    Tuple<DateTime, DateTime> monthBound = CommonServiceHelper.GetMonthBound(base.Context, systemTime.Date);
                    this.Model.SetValue("FStartDate", monthBound.Item1);
                    this.Model.SetValue("FEndDate", monthBound.Item2);
                    return;
                }
                if (!(a == "FCURRENTWEEK"))
                {
                    return;
                }
                Tuple<DateTime, DateTime> weekBound = CommonServiceHelper.GetWeekBound(base.Context, systemTime.Date);
                this.Model.SetValue("FStartDate", weekBound.Item1);
                this.Model.SetValue("FEndDate", weekBound.Item2);
            }
        }

        private bool CheckDate()
        {
            object value = this.Model.GetValue("FStartDate");
            object value2 = this.Model.GetValue("FEndDate");
            if (value == null || value2 == null)
            {
                return false;
            }
            if (Convert.ToDateTime(value) > Convert.ToDateTime(value2))
            {
                this.View.ShowMessage(ResManager.LoadKDString("结束时间不可小于起始时间", "003193000004657", SubSystemType.FIN, new object[0]), MessageBoxType.Notice);
                return false;
            }
            return true;
        }

    }
}

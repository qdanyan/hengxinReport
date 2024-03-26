using System;
using System.Collections.Generic;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.CommonFilter.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission.Objects;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.GL.Common.Core;
using Kingdee.K3.FIN.GL.Report.PlugIn.Base;
using Kingdee.K3.FIN.GL.ServiceHelper;
using Kingdee.K3.FIN.ServiceHelper;
using Kingdee.K3.FIN.CN.Business.PlugIn.Filter;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace ANY.HX.K3.Report
{
    [Description("Any | 销售利润报表过滤条件")]
    [HotUpdate]
    public class AnySaleProfitFilter : AbstractReportFilter
    {

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            UIInitialize(methodParams, CurrencyType.ComprehensiveCurrency);
            InitUIValue();
        }

        public override void SetFilterParams()
        {
            methodParams.Currency = "F_ANY_Currency";
        }

        private void InitUIValue(bool setDefaultValue = true)
        {
            DynamicObject dynamicObject = View.Model.GetValue("FACCTBOOKID") as DynamicObject;
            if (dynamicObject == null)
            {
                return;
            }
            long num = Convert.ToInt32(dynamicObject["Id"]);
            if (num == 0)
            {
                View.Model.SetValue("F_ANY_Currency", null);
            }
            FormMetadata formMetadata = MetaDataServiceHelper.Load(base.Context, "BD_AccountBook") as FormMetadata;
            BusinessInfo businessInfo = formMetadata.BusinessInfo;
            DynamicObject dynamicObject2 = BusinessDataServiceHelper.LoadSingle(base.Context, dynamicObject["Id"], businessInfo.GetDynamicObjectType());
            DynamicObject dynamicObject3 = dynamicObject2["AccountCalendar"] as DynamicObject;
            if (dynamicObject3 != null)
            {
                DateTime systemDateTime = TimeServiceHelper.GetSystemDateTime(base.Context);
                int num2 = Convert.ToInt32(dynamicObject2["STARTYEAR"]) * 100 + Convert.ToInt32(dynamicObject2["STARTPERIOD"]);
                int num3 = systemDateTime.Year * 100 + systemDateTime.Month;
                int num4 = Convert.ToInt32(dynamicObject2["CURRENTYEAR"]) * 100 + Convert.ToInt32(dynamicObject2["CURRENTPERIOD"]);
                DynamicObject dynamicObject4 = dynamicObject2["Currency"] as DynamicObject;
                if (setDefaultValue)
                {
                    if (dynamicObject4 != null)
                    {
                        View.Model.SetValue("F_ANY_Currency", dynamicObject4["Id"]);
                    }
                    else
                    {
                        View.Model.SetValue("F_ANY_Currency", null);
                    }
                }
            }
            View.UpdateView("F_ANY_Currency");
        }


    }
}

using Microsoft.AspNetCore.Components;

namespace JiwaCustomerPortal.Components.Pages.SalesOrder
{
    public class SalesOrderCustomFieldLookupDisplayResolverContext
    {
        public SalesOrder Host { get; set; }
        public JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields.CustomFieldValue CustomFieldValue { get; set; }
    }
}

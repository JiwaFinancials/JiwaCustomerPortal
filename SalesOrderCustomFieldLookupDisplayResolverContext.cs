using Microsoft.AspNetCore.Components;

namespace JiwaCustomerPortal
{
    public class SalesOrderCustomFieldLookupDisplayResolverContext
    {
        public JiwaCustomerPortal.Components.Pages.SalesOrder Host { get; set; }
        public JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields.CustomFieldValue CustomFieldValue { get; set; }
    }
}

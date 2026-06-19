using Microsoft.AspNetCore.Components;

namespace JiwaCustomerPortal.Components.Pages.SalesOrder
{
    public class SalesOrderCustomFieldLookupRenderFragmentContext
    {
        private RenderFragment? _CustomFieldLookupRenderFragment;
        public RenderFragment? CustomFieldLookupRenderFragment 
        { 
            get
            {
                return _CustomFieldLookupRenderFragment;
            }

            set
            {
                _CustomFieldLookupRenderFragment = value;
                if (Host != null)
                {
                    Host.CustomFieldLookupRenderFragment = value;
                }                
            }
        }
        public SalesOrder Host { get; set; }
        public JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields.CustomField CustomField { get; set; }
        // SalesOrderLine is only populated for sales order line custom fields
        public JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders.SalesOrderLine SalesOrderLine { get; set; }
    }
}

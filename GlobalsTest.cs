using Microsoft.AspNetCore.Components;

namespace JiwaCustomerPortal
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
        public JiwaCustomerPortal.Components.Pages.SalesOrder Host { get; set; }
        public JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields.CustomField CustomField { get; set; }
    }
}

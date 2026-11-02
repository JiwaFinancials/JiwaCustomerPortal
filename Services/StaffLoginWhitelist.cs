using System.Net;

namespace JiwaCustomerPortal.Services
{
    public sealed class StaffLoginWhitelist
    {
        public const string DenialMessage = "Staff login is not permitted from your IP address. Please contact your administrator.";
        private readonly bool enabled;
        private readonly IPNetwork[] networks;

        public StaffLoginWhitelist(IConfiguration configuration)
        {
            enabled = configuration.GetValue<bool>("AllowStaffLogin");
            string[] ranges = configuration.GetSection("StaffLoginAllowedCidrs").Get<string[]>() ?? [];
            networks = new IPNetwork[ranges.Length];

            for (int i = 0; i < ranges.Length; i++)
            {
                if (!IPNetwork.TryParse(ranges[i], out IPNetwork network))
                {
                    throw new InvalidOperationException($"StaffLoginAllowedCidrs:{i} must be a valid IPv4 or IPv6 CIDR range.");
                }

                if (network.BaseAddress.IsIPv4MappedToIPv6 && network.PrefixLength >= 96)
                {
                    network = new IPNetwork(network.BaseAddress.MapToIPv4(), network.PrefixLength - 96);
                }

                networks[i] = network;
            }
        }

        public bool IsAllowed(IPAddress? address)
        {
            if (!enabled || address == null)
            {
                return false;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            return networks.Any(network => network.Contains(address));
        }
    }
}

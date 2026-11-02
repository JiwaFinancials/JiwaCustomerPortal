using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Net;

namespace JiwaCustomerPortal.Services
{
    public sealed class StaffLoginAccess : CircuitHandler
    {
        private readonly StaffLoginWhitelist whitelist;
        private readonly IHttpContextAccessor httpContextAccessor;

        public StaffLoginAccess(StaffLoginWhitelist whitelist, IHttpContextAccessor httpContextAccessor)
        {
            this.whitelist = whitelist;
            this.httpContextAccessor = httpContextAccessor;
            RemoteIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        }

        public IPAddress? RemoteIpAddress { get; private set; }
        public bool IsAllowed => whitelist.IsAllowed(RemoteIpAddress);

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Refresh on reconnect as a remembered circuit can move to a different network.
            RemoteIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            RemoteIpAddress = null;
            return Task.CompletedTask;
        }
    }
}

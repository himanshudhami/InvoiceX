using Application.Services.EInvoice;
using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;

namespace Infrastructure.EInvoice
{
    /// <summary>
    /// Factory for creating GSP clients based on provider name
    /// </summary>
    public class EInvoiceGspClientFactory : IEInvoiceGspClientFactory
    {
        private readonly Dictionary<string, IEInvoiceGspClient> _clients;

        public EInvoiceGspClientFactory(IEnumerable<IEInvoiceGspClient> clients)
        {
            _clients = clients.ToDictionary(c => c.ProviderName.ToLowerInvariant());
        }

        public IEInvoiceGspClient? GetClient(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;

            return _clients.TryGetValue(providerName.ToLowerInvariant(), out var client)
                ? client
                : null;
        }
    }
}

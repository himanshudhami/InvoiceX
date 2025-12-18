using Core.Abstractions;
using Core.Entities.Approval;
using Core.Interfaces.Approval;

namespace Application.Services.Approval
{
    /// <summary>
    /// Factory for resolving the appropriate IApproverResolver based on approver type.
    /// Uses the registered resolvers to find the correct implementation.
    /// </summary>
    public class ApproverResolverFactory : IApproverResolverFactory
    {
        private readonly IEnumerable<IApproverResolver> _resolvers;

        public ApproverResolverFactory(IEnumerable<IApproverResolver> resolvers)
        {
            _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        }

        public IApproverResolver? GetResolver(string approverType)
        {
            return _resolvers.FirstOrDefault(r =>
                r.ApproverType.Equals(approverType, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Guid?> ResolveApproverAsync(Guid requestorId, ApprovalWorkflowStep step)
        {
            var resolver = GetResolver(step.ApproverType);
            if (resolver == null)
            {
                throw new InvalidOperationException($"No resolver registered for approver type: {step.ApproverType}");
            }

            return await resolver.ResolveApproverAsync(requestorId, step);
        }
    }
}

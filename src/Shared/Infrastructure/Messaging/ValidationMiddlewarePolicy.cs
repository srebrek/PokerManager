using System.Reflection;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using Wolverine.Configuration;
using Wolverine.Runtime.Handlers;

namespace Shared.Infrastructure.Messaging;

public sealed class ValidationMiddlewarePolicy : IChainPolicy
{
    public void Apply(IReadOnlyList<IChain> chains, GenerationRules rules, IServiceContainer container)
    {
        MethodInfo openMethod = typeof(ValidationMiddleware)
            .GetMethod(nameof(ValidationMiddleware.BeforeAsync))!;

        foreach (HandlerChain chain in chains.OfType<HandlerChain>())
        {
            MethodInfo closedMethod = openMethod.MakeGenericMethod(chain.MessageType);
            chain.Middleware.Insert(0, new MethodCall(typeof(ValidationMiddleware), closedMethod));
        }
    }
}

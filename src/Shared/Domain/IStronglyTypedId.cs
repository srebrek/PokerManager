namespace Shared.Domain;

public interface IStronglyTypedId<TSelf>
    where TSelf : IStronglyTypedId<TSelf>
{
    Guid Value { get; }

    static abstract TSelf From(Guid value);
}

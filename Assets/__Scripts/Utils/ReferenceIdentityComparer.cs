using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class ReferenceIdentityComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static readonly ReferenceIdentityComparer<T> Instance = new();

    public bool Equals(T left, T right) => ReferenceEquals(left, right);

    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}

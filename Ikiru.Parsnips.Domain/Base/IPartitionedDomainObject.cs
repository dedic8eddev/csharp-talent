using System;

namespace Ikiru.Parsnips.Domain.Base
{
    public interface IPartitionedDomainObject
    {
        static string PartitionKey { get; }
    }
}

namespace Ikiru.Parsnips.Domain.Base
{
    /// <summary>
    /// Interface for Domain Objects which can stored in a shared store and therefore need to be discriminated by type.
    /// </summary>
    public interface IDiscriminatedDomainObject
    {
        string Discriminator { get; }
    }
}
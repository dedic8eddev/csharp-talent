using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum GdprLawfulBasisOptionsStatusEnum
    {
        None,   
        NotStarted,
        ConsentRequestSent,
        ConsentGiven,
        ConsentRefused,
        NotificationSent,
        Objected
    }
}

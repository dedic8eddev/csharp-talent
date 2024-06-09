using System;
using System.Diagnostics.CodeAnalysis;
using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain
{
    public class SearchFirmUser : MultiTenantedDomainObject, IDiscriminatedDomainObject
    {
        [JsonIgnore]
        public static string DiscriminatorName { get; } = "SearchFirmUser";
        public string Discriminator => DiscriminatorName;

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string JobTitle { get; set; }
        public Guid? InviteToken { get; set; }
        public SearchFirmUserStatus Status { get; set; } = SearchFirmUserStatus.Complete;

        public Guid? InvitedBy { get; set; }
        public UserRole UserRole { get; set; }

        // Immutable Properties
        public bool ConfirmationEmailSent { get; private set; }
        public DateTimeOffset? ConfirmationEmailSentDate { get; private set; }

        public bool IsDisabled { get; set; }

        public Guid IdentityUserId { get; private set; } // Property containing link ID in Identity Database

        /* Serialiser Constructor */
        [JsonConstructor]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private SearchFirmUser(Guid id, DateTimeOffset createdDate, Guid searchFirmId, Guid identityUserId, bool confirmationEmailSent, DateTimeOffset? confirmationEmailSentDate) : base(id, createdDate, searchFirmId)
        {
            IdentityUserId = identityUserId;
            ConfirmationEmailSent = confirmationEmailSent;
            ConfirmationEmailSentDate = confirmationEmailSentDate;
        }

        /* Business Logic Constructor */
        public SearchFirmUser(Guid searchFirmId) : base(searchFirmId)
        {
        }

        public void SetIdentityUserId(Guid identityUserId)
        {
            IdentityUserId = identityUserId;
        }

        public void MarkConfirmationEmailSent()
        {
            ConfirmationEmailSent = true;
            ConfirmationEmailSentDate = DateTimeOffset.UtcNow;
        }

        public string FullName() => $"{FirstName} {LastName}";

        public bool IsEnabled
        {
            get => !IsDisabled;
            set => IsDisabled = !value;
        }
    }
}
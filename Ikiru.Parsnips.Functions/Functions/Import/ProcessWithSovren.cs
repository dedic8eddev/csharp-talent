using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Functions.Parsing;
using Ikiru.Parsnips.Functions.Parsing.Api;
using Ikiru.Parsnips.Functions.Parsing.Api.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.Functions.Import
{
    public class ProcessWithSovren
    {
        private readonly ParsingService m_ParsingService;
        private readonly BlobStorage m_BlobStorage;
        private readonly ILogger<ProcessWithSovren> m_Logger;

        public ProcessWithSovren(ParsingService parsingService, BlobStorage blobStorage, ILogger<ProcessWithSovren> logger)
        {
            m_ParsingService = parsingService;
            m_BlobStorage = blobStorage;
            m_Logger = logger;
        }

        public async Task Import(Person person, ImportBlob importBlob, Func<Person, Task<Person>> persistAction)
        {
            var parsedDocument = await m_ParsingService.ParseDocument(importBlob.Base64Document);

            UpdatePerson(person, parsedDocument.SovrenParsedDocument);

            await persistAction(person);
            await StoreRawResume(importBlob, parsedDocument.SovrenResponse);
        }

        private static void UpdatePerson(Person person, SovrenParsedDocument parsedDocument)
        {
            var resume = parsedDocument.Resume.StructuredXMLResume;
            person.Name = resume.ContactInfo?.PersonName?.FormattedName ?? "";
            person.ImportedLinkedInProfileUrl = resume.ContactInfo?.ContactMethod?.FirstOrDefault(c => c.Use == "linkedIn")?.InternetWebAddress;
            person.Location = resume.ContactInfo?.ContactMethod?.FirstOrDefault(c => c.PostalAddress != null)?.PostalAddress.Flatten();
            person.Organisation = resume.EmploymentHistory?.EmployerOrg?.FirstOrDefault()?.EmployerOrgName;
            person.JobTitle = resume.EmploymentHistory?.EmployerOrg?.FirstOrDefault()?.PositionHistory?.FirstOrDefault()?.Title;

            var phoneNumber = resume.ContactInfo?.ContactMethod?.FirstOrDefault(c => c.HasPhoneNumber())?.GetPhoneNumber();
            if (phoneNumber != null)
                person.AddPhoneNumbers(new List<string> { phoneNumber });

            var emailAddress = resume.ContactInfo?.ContactMethod?.FirstOrDefault(c => c.InternetEmailAddress != null)?.InternetEmailAddress;
            if (!string.IsNullOrWhiteSpace(emailAddress))
                person.AddTaggedEmail(emailAddress);
        }

        private async Task StoreRawResume(ImportBlob importBlob, SovrenResponse sovrenResponse)
        {
            try
            {
                var rawContent = JsonSerializer.Serialize(sovrenResponse);
                await m_BlobStorage.UploadAsync(BlobStorage.ContainerNames.RawResumes, $"{importBlob.SearchFirmId}/{importBlob.ImportId}", rawContent);
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, $"Cannot save raw resume for search firm id '{importBlob.SearchFirmId}', import id '{importBlob.ImportId}'");
            }
        }
    }
}
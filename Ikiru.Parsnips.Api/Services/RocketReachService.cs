using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.RocketReach.Enum;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services
{
    public class RocketReachService
    {
        private const string _COMPLETE_STATUS = "complete";
        private const string _FAILED_STATUS = "failed";
        private const string _INVALID_EMAIL = "invalid";
        private const string _UNVERIFIED = "unverified";

        private readonly IRocketReachApi m_RocketReachApi;
        private readonly ISearchFirmTokenProcessor m_SearchFirmTokenProcessor;
        private readonly ILogger<RocketReachService> m_Logger;
        private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
        private readonly RocketReachSettings m_RocketReachSettings;

        public RocketReachService(IRocketReachApi rocketReachApi, ISearchFirmTokenProcessor searchFirmTokenProcessor,
                                  ILogger<RocketReachService> logger, AuthenticatedUserAccessor authenticatedUserAccessor,
                                  IOptions<RocketReachSettings> rocketReachSettings)
        {
            m_RocketReachApi = rocketReachApi;
            m_SearchFirmTokenProcessor = searchFirmTokenProcessor;
            m_Logger = logger;
            m_AuthenticatedUserAccessor = authenticatedUserAccessor;
            m_RocketReachSettings = rocketReachSettings.Value;
        }

        public async Task<PersonTeaserResponse> GetTeaserInformation(Person person)
        {

            var personTeaserResponse = new PersonTeaserResponse()
            {
                Emails = new List<string>(),
                PhoneNumbers = new List<string>()
            };

            List<string> teaserEmails = new List<string>();

            var response = await SearchQueryByLinkedinProfileUrl(person.LinkedInProfileUrl);
            if (response?.profiles != null)
            {
                if (response.profiles.Any(x => x.teaser?.phones != null))
                    personTeaserResponse.PhoneNumbers.AddRange(response.profiles.SelectMany(x => x.teaser.phones).Select(p => p.number));

                if (response.profiles.Any(x => x.teaser?.preview != null))
                {
                    var previews = response.profiles
                                           .SelectMany(x => x.teaser.preview)
                                           .Cast<string>()
                                           .Select(ValidateEmailDomain)
                                           .Where(email => email != null);

                    personTeaserResponse.Emails.AddRange(previews);
                }

                if (response.profiles.Any(x => x.teaser?.emails != null))
                    personTeaserResponse.Emails.AddRange(response.profiles
                                                  .SelectMany(x => x.teaser.emails)
                                                  .Select(ValidateEmailDomain)
                                                  .Where(email => email != null));

            }

            return personTeaserResponse;
        }

        public async Task<PersonResponse> GetContactDetails(Domain.Person person)
        {
            var personResponse = new PersonResponse
            {
                LookupProfileResponsePhoneNumber = new List<LookupProfileResponsePhoneNumberModel>(),
                LookupProfileResponseEmail = new List<LookupProfileResponseEmailModel>()
            };

            SearchFirmToken searchFirmToken = null;
            var needToSpendToken = false;
            if (!m_RocketReachSettings.BypassCredits)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();
                searchFirmToken = await m_SearchFirmTokenProcessor.SpendToken(authenticatedUser.SearchFirmId, authenticatedUser.UserId);

                if (searchFirmToken == null)
                {
                    personResponse.RocketReachResponseEnum = RocketReachResponse.InsufficientCredits;
                    return personResponse;
                }
            }

            var response = await LookupProfileByLinkedinProfileUrl(person.LinkedInProfileUrl);

            if (response != null)
            {
                needToSpendToken = response.phones?.Any() == true || response.emails?.Any() == true;

                if (response.phones?.Any() == true)
                    personResponse.LookupProfileResponsePhoneNumber.AddRange(response.phones.Select(x => new LookupProfileResponsePhoneNumberModel
                    {
                        number = x.number,
                        type = x.type
                    }));

                if (response.emails?.Any() == true)
                    personResponse.LookupProfileResponseEmail.AddRange(response.emails
                                                      .Select(x =>
                                                                  new LookupProfileResponseEmailModel
                                                                  {
                                                                      email = ValidateEmail(x.email),
                                                                      smtp_valid = x.smtp_valid
                                                                  })
                                                      .Where(model => model.email != null));
            }

            personResponse.RocketReachResponseEnum = RocketReachResponse.Success;

            // check if valid data has been returned from rocket reach. if not then do not use a rocketreach token.
            if (personResponse.LookupProfileResponseEmail.All(e => e.smtp_valid == _INVALID_EMAIL ||
                                                                        e.smtp_valid == _UNVERIFIED) &&
                personResponse.LookupProfileResponsePhoneNumber == null || !personResponse.LookupProfileResponsePhoneNumber.Any())
            {
                needToSpendToken = false;
            }

            if (!needToSpendToken && searchFirmToken != null)
                await m_SearchFirmTokenProcessor.RestoreToken(searchFirmToken);

            return personResponse;
        }

        private async Task<LookupProfileResponseModel> LookupProfileByLinkedinProfileUrl(string linkedinProfileUrl)
        {
            if (string.IsNullOrEmpty(linkedinProfileUrl))
                return null;

            try
            {
                return await LookupProfileUntilResult(() => m_RocketReachApi.LookupProfile(m_RocketReachSettings.ApiKey, linkedinProfileUrl));
            }
            catch (Exception e)
            {
                m_Logger.LogError(e, "Rocket reach API error");

                throw new ResourceNotFoundException("RocketReachApi");
            }
        }

        private async Task<LookupProfileResponseModel> LookupProfileByNameAndCompany(string name, string companyName)
        {
            if (string.IsNullOrEmpty(companyName))
                throw new ParamValidationFailureException("CompanyName", " Unable to person email teaser search, Company name must exist");


            if (string.IsNullOrEmpty(name))
                throw new ParamValidationFailureException("PersonName", " Unable to person email teaser search, Person name must exist");

            try
            {
                return await LookupProfileUntilResult(() => m_RocketReachApi.LookupProfile(m_RocketReachSettings.ApiKey, name, companyName));
            }
            catch (Exception e)
            {
                m_Logger.LogError(e, "Rocket reach API error");

                throw new ResourceNotFoundException("RocketReachApi");
            }
        }

        private async Task<LookupProfileResponseModel> LookupProfileUntilResult(Func<Task<LookupProfileResponseModel>> lookupProfile)
        {
            var pollCounter = 1;

            var result = await lookupProfile();
            var searchId = result.id;

            //Todo: implement a web hook instead of polling periodically
            while (result.status != _COMPLETE_STATUS && result.status != _FAILED_STATUS && pollCounter++ < m_RocketReachSettings.RetryNumber)
            {
                m_Logger.LogWarning($"Polling RocketReach {pollCounter}st/nd/rd/th time as the result was not returned immediately.");
                await Task.Delay(m_RocketReachSettings.DelayBetweenRetriesMilliseconds);

                var results = await m_RocketReachApi.CheckStatus(m_RocketReachSettings.ApiKey, searchId);
                result = results.Single(r => r.id == searchId);
            }

            return result;
        }

        private async Task<SearchResponseModel> SearchQueryByLinkedinProfileUrl(string linkedinProfileUrl)
        {
            SearchRequestModel searchRequestModel = new SearchRequestModel();
            SearchResponseModel searchResponseModel;

            if (string.IsNullOrEmpty(linkedinProfileUrl))
                return null;

            searchRequestModel.query = new SearchRequestModel.Query
            {
                name = new string[] { },
                current_employer = new string[] { },
                keywords = new[] { linkedinProfileUrl }
            };

            try
            {
                searchResponseModel = await m_RocketReachApi.SearchForPersonDetails(m_RocketReachSettings.ApiKey, searchRequestModel);
            }
            catch (Exception e)
            {
                m_Logger.LogError(e, "Rocket reach API error");

                throw new ResourceNotFoundException("RocketReachApi");
            }

            return searchResponseModel;
        }

        private async Task<SearchResponseModel> SearchQueryByNameAndCompany(string name, string companyName)
        {
            SearchRequestModel searchRequestModel = new SearchRequestModel();
            SearchResponseModel searchResponseModel;

            if (string.IsNullOrEmpty(companyName))
                throw new ParamValidationFailureException("CompanyName", " Unable to person email teaser search, Company name must exist");


            if (string.IsNullOrEmpty(name))
                throw new ParamValidationFailureException("PersonName", " Unable to person email teaser search, Person name must exist");


            searchRequestModel.query = new SearchRequestModel.Query
            {
                name = new[] { name },
                current_employer = new[] { companyName },
                keywords = new string[] { }
            };

            try
            {
                searchResponseModel = await m_RocketReachApi.SearchForPersonDetails(m_RocketReachSettings.ApiKey, searchRequestModel);
            }
            catch (Exception e)
            {
                m_Logger.LogError(e, "Rocket reach API error");

                throw new ResourceNotFoundException("RocketReachApi");
            }

            return searchResponseModel;
        }

        private string ValidateEmailDomain(string email)
        {
            var emailDomainValidator = new Regex(@"([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$");

            return emailDomainValidator.Match(email).Success
                       ? email
                       : null;
        }
        private string ValidateEmail(string email)
        {
            var emailDomainValidator = new Regex(@"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})");

            return emailDomainValidator.Match(email).Success
                       ? email
                       : null;
        }

    }
}

using AutoMapper;
using Ikiru.Parsnips.Application.Command.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Services.Person;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command
{
    public class PersonScrapedCommand : ICommandHandler<PersonScrapedRequest, CommandResponse<PersonScrapedResponse>>
    {
        private readonly IMapper _mapper;
        private readonly IPersonInfrastructure _personInfrastructure;
        private readonly IPersonService _personService;

        public PersonScrapedCommand(IPersonInfrastructure personInfrastructure,
                                    ILogger<PersonScrapedCommand> logger,
                                    IMapper mapper,
                                    IPersonService personService
                                    )
        {
            _personInfrastructure = personInfrastructure;
            _mapper = mapper;
            _personService = personService;
        }

        public async Task<CommandResponse<PersonScrapedResponse>> Handle(PersonScrapedRequest command)
        {
            var response = new CommandResponse<PersonScrapedResponse>()
            {
                ResponseModel = new PersonScrapedResponse()
            };

            response.ResponseModel.LocalPerson = await GetTalentisResult(command);
            response.ResponseModel.DataPoolPerson = await GetDataPoolResult(command);

            response.ResponseModel.LocalPerson?.Websites.Sort((a, b) => a.WebsiteType.CompareTo(b.WebsiteType));
            response.ResponseModel.DataPoolPerson?.Websites.Sort((a, b) => a.WebsiteType.CompareTo(b.WebsiteType));

            return response;
        }

        private async Task<Shared.Models.Person> GetDataPoolResult(PersonScrapedRequest command)
        {
            var datapoolPerson = await  _personInfrastructure.SendScrapedPerson(command.ScrapedData);

            var datapoolPersonResult = _mapper.Map<Shared.Models.Person>(datapoolPerson);

            if (datapoolPersonResult != null)
            {
                if (datapoolPerson.PersonDetails != null)
                {
                    datapoolPersonResult.Photo = new Shared.Models.Photo { Url = datapoolPerson.PersonDetails.PhotoUrl };
                }
            }

            return datapoolPersonResult;
        }

         private async Task<Shared.Models.Person> GetTalentisResult(PersonScrapedRequest command)
        {
            var websiteUrl = string.Empty;

            //Search Talentis API if it exists first
            if (command.ScrapedData.RootElement.TryGetProperty("scrapeOriginatorUrl", out var scrapeOriginatorUrl))
            {
                websiteUrl = scrapeOriginatorUrl.ToString();
            }

            if (websiteUrl.ToLower().Contains("linkedin"))
            {
                var linkedinProfileId = Ikiru.Parsnips.Domain.Person.NormaliseLinkedInProfileUrl(websiteUrl);

                if (string.IsNullOrEmpty(linkedinProfileId))
                {
                    if (command.ScrapedData.RootElement.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("identifier", out var identifier))
                        {
                            websiteUrl = identifier.GetString();
                        }
                    }
                }
            }

            return await _personService.GetLocalPersonResult(new Ikiru.Parsnips.Application.Services.Person.Models.GetByWebsiteUrlRequest
            {
                SearchFirmId = command.SearchFirmId,
                UserId = command.UserId,
                WebsiteUrl = websiteUrl
            });
        }
    }
}

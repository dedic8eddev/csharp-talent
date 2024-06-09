using Ikiru.Parsnips.Api.Controllers.Persons.Scraped;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Infrastructure.Storage;
using Ikiru.Parsnips.Application.Persistence;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Ikiru.Parsnips.Application.Command.Models;
using System.Text.Json;
using System.Linq.Expressions;
using Ikiru.Parsnips.Api.Controllers.Persons;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Scraped
{
    public class PostTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly List<Person> m_StoredPersons = new List<Person>();
        private readonly string m_LinkedinId = "testaccount123456";
        private readonly Person m_Person;
        private readonly Assignment m_AssignmentTwo;
        private readonly List<Assignment> m_Assignments = new List<Assignment>();
        private readonly Candidate m_CandidateTwo;
        private readonly List<Candidate> m_Candidates = new List<Candidate>();
        private readonly List<Note> m_Notes = new List<Note>();
        private readonly SearchFirmUser m_SearchFirmUser;
        private readonly List<SearchFirmUser> m_SearchFirmUsers = new List<SearchFirmUser>();
        private readonly Mock<IDataPoolApi> m_DataPoolApiMock;

        private Stream m_RequestStream;
        private string m_Payload;

        public PostTests()
        {
            m_DataPoolApiMock = new Mock<IDataPoolApi>();

            m_SearchFirmUser = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "John",
                LastName = "Smith"
            };

            m_SearchFirmUsers.Add(m_SearchFirmUser);

            var linkedinUrl = $"https://Linkedin.com/in/{m_LinkedinId}";

            m_Person = new Person(m_SearchFirmId, null, linkedinUrl)
            {
                Name = "John Smith",
                JobTitle = "top dog",
                SectorsIds = new List<string>() { "I1269", "I12691", "3D Printing" },
                Location = "Southampton",
                WebSites = new List<PersonWebsite>()
                           {
                               new PersonWebsite
                               {
                                   Type = WebSiteType.LinkedIn,
                                   Url = linkedinUrl
                               }
                           }
            };

            m_StoredPersons.Add(m_Person);

            var personAlreadyHasGoogleDomSearchData = new Person(m_SearchFirmId, null, linkedinUrl + "dom data exists")
            {
                JobTitle = "top dog",
                SectorsIds = new List<string>() { "I12691" }, // Sector("I1269", "I12691", "3D Printing")
                Location = "Southampton",
                WebSites = new List<PersonWebsite>()
                                                                       {
                                                                           new PersonWebsite
                                                                           {
                                                                               Type = WebSiteType.LinkedIn,
                                                                               Url = linkedinUrl + "/dom data exists"
                                                                           }
                                                                       },
                ScrapedData = new List<ScrapedDataForPerson>
                                                                          {
                                                                              new ScrapedDataForPerson
                                                                              {
                                                                                  DomContent = "some html stuff",
                                                                                  PersonUrl = linkedinUrl + "dom data exists",
                                                                                  SourceOriginatorType = ScrapedPersonOriginatorType.google
                                                                              }
                                                                          }
            };

            m_StoredPersons.Add(personAlreadyHasGoogleDomSearchData);

            var assignmentOne = new Assignment(m_SearchFirmId)
            {
                Name = "Assignment1"
            };

            m_Assignments.Add(assignmentOne);

            m_AssignmentTwo = new Assignment(m_SearchFirmId)
            {
                Name = "Assignment2"
            };

            m_Assignments.Add(m_AssignmentTwo);

            var note = new Note(m_Person.Id, m_SearchFirmUser.Id, m_SearchFirmId)
            {
                NoteTitle = "Note title",
                AssignmentId = assignmentOne.Id,
                UpdatedBy = m_SearchFirmUser.Id,
                UpdatedDate = DateTimeOffset.Now
            };

            m_Notes.Add(note);

            var candidateOne = new Candidate(m_SearchFirmId, assignmentOne.Id, m_Person.Id)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Status = CandidateStatusEnum.LeftMessage,
                    Stage = CandidateStageEnum.FirstClientInterview
                }
            };

            m_Candidates.Add(candidateOne);


            m_CandidateTwo = new Candidate(m_SearchFirmId, m_AssignmentTwo.Id, m_Person.Id)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Status = CandidateStatusEnum.NoStatus,
                    Stage = CandidateStageEnum.Identified
                }
            };

            m_Candidates.Add(m_CandidateTwo);


            m_FakeCosmos = new FakeCosmos()
                            .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString())
                            .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons)
                            .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => m_Assignments)
                            .EnableContainerLinqQuery(FakeCosmos.CandidatesContainerName, m_SearchFirmId.ToString(), () => m_Candidates)
                            .EnableContainerLinqQuery(FakeCosmos.PersonNotesContainerName, m_SearchFirmId.ToString(), () => m_Notes)
                            .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => m_SearchFirmUsers);
        }

       
        private PersonsScrapedController CreateController()
        {
            var controller = new ControllerBuilder<PersonsScrapedController>()
                  .AddTransient(m_DataPoolApiMock.Object)
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .Build();
            m_RequestStream = new MemoryStream();
            var writer = new StreamWriter(m_RequestStream);
            writer.Write(m_Payload);
            writer.Flush();
            m_RequestStream.Position = 0;

            controller.Request.Body = m_RequestStream;

            return controller;
        }

    }
}

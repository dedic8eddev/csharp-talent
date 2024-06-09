using Ikiru.Parsnips.Api.Filters.Unauthorized;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.DatapoolLoopback
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class DpMediatrLoopbackController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly List<string> m_Messages = new List<string>();

        private Stopwatch m_Stopwatch = Stopwatch.StartNew();

        public DpMediatrLoopbackController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetDirect([FromQuery] int statusCode)
        {
            m_Stopwatch = Stopwatch.StartNew();
            return await m_Mediator.Send(new Direct.Query { StatusCode = statusCode, Stopwatch = m_Stopwatch, Messages = m_Messages });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRefitAsync([FromQuery] int statusCode)
        {
            m_Stopwatch = Stopwatch.StartNew();
            return await m_Mediator.Send(new ExternalRefit.Query { StatusCode = statusCode, Stopwatch = m_Stopwatch, Messages = m_Messages });
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery] int statusCode)
        {
            m_Stopwatch = Stopwatch.StartNew();
            return await m_Mediator.Send(new External.Query { StatusCode = statusCode, Stopwatch = m_Stopwatch, Messages = m_Messages });
        }
    }

    public class Direct
    {
        public class Query : IRequest<IActionResult>
        {
            public int StatusCode { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public List<string> Messages { get; set; }
        }

        public class Handler : IRequestHandler<Query, IActionResult>
        {
            public async Task<IActionResult> Handle(Query request, CancellationToken cancellationToken)
                => ProcessResponse(request);


            private IActionResult ProcessResponse(Query query)
                => query.StatusCode switch
                   {
                       400 => throw new ParamValidationFailureException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       401 => throw new UnauthorizedException(),
                       404 => throw new ResourceNotFoundException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       200 => new OkObjectResult($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       500 => throw new Exception($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       _ => new ObjectResult($"loopback exception - \r\n{GetMessagesAndFinalize(query)}")
                            {
                                StatusCode = query.StatusCode
                            }
                   };

            private void Log(string message, Query query) => query.Messages.Add($"{DateTimeOffset.UtcNow:s} - Stopwatch: {query.Stopwatch.ElapsedMilliseconds:D5} ms. {message}");

            private string GetMessagesAndFinalize(Query query)
            {
                Log("Last message...", query);
                var message = string.Join("\r\n", query.Messages);
                query.Messages.Clear();

                return message;
            }
        }
    }

    public class External
    {
        public class Query : IRequest<IActionResult>
        {
            public int StatusCode { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public List<string> Messages { get; set; }
        }

        public class Handler : IRequestHandler<Query, IActionResult>
        {
            private DataPoolApiHttpClient m_HttpClient;
            public Handler(DataPoolApiHttpClient httpClient) => m_HttpClient = httpClient;

            public async Task<IActionResult> Handle(Query request, CancellationToken cancellationToken)
            {
                Log("Starting...", request);
                try
                {
                    var client = m_HttpClient.HttpClient.Value;
                    var response = await client.GetAsync($"/api/v1.0/loopback?statusCode={request.StatusCode}", cancellationToken);
                    var result = await response.Content.ReadAsAsync<string>(cancellationToken);

                    Log($"Loopback succeeded, result: {result}.", request);

                    return new OkObjectResult($"{nameof(request.StatusCode)} - {request.StatusCode} Error\r\n{GetMessagesAndFinalize(request)}");
                }
                catch
                {
                    return ProcessResponse(request);
                }
            }

            private IActionResult ProcessResponse(Query query)
                => query.StatusCode switch
                   {
                       400 => throw new ParamValidationFailureException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       401 => throw new UnauthorizedException(),
                       404 => throw new ResourceNotFoundException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       500 => throw new Exception($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       200 => new OkObjectResult($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                       _ => new ObjectResult($"loopback exception - \r\n{GetMessagesAndFinalize(query)}")
                            {
                                StatusCode = query.StatusCode
                            }
                   };

            private void Log(string message, Query query) => query.Messages.Add($"{DateTimeOffset.UtcNow:s} - Stopwatch: {query.Stopwatch.ElapsedMilliseconds:D5} ms. {message}");

            private string GetMessagesAndFinalize(Query query)
            {
                Log("Last message...", query);
                var message = string.Join("\r\n", query.Messages);
                query.Messages.Clear();

                return message;
            }
        }
    }

    public class ExternalRefit
    {
        public class Query : IRequest<IActionResult>
        {
            public int StatusCode { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public List<string> Messages { get; set; }
        }

        public class Handler : IRequestHandler<Query, IActionResult>
        {
            private IDataPoolApi m_DataPoolApi;
            public Handler(IDataPoolApi dataPoolApi) => m_DataPoolApi = dataPoolApi;

            public async Task<IActionResult> Handle(Query request, CancellationToken cancellationToken)
            {
                Log("Starting...", request);
                try
                {
                    var result = await m_DataPoolApi.GetLoopback(request.StatusCode, cancellationToken);
                    Log($"Loopback succeeded, result: {result}.", request);

                    return new OkObjectResult($"{nameof(request.StatusCode)} - {request.StatusCode} Error\r\n{GetMessagesAndFinalize(request)}");
                }
                catch
                {
                    return ProcessResponse(request);
                }
            }

            private IActionResult ProcessResponse(Query query)
                => query.StatusCode switch
                {
                    400 => throw new ParamValidationFailureException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                    401 => throw new UnauthorizedException(),
                    404 => throw new ResourceNotFoundException(nameof(query.StatusCode), $"{query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                    500 => throw new Exception($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                    200 => new OkObjectResult($"{nameof(query.StatusCode)} - {query.StatusCode} Error\r\n{GetMessagesAndFinalize(query)}"),
                    _ => new ObjectResult($"loopback exception - \r\n{GetMessagesAndFinalize(query)}")
                    {
                        StatusCode = query.StatusCode
                    }
                };

            private void Log(string message, Query query) => query.Messages.Add($"{DateTimeOffset.UtcNow:s} - Stopwatch: {query.Stopwatch.ElapsedMilliseconds:D5} ms. {message}");

            private string GetMessagesAndFinalize(Query query)
            {
                Log("Last message...", query);
                var message = string.Join("\r\n", query.Messages);
                query.Messages.Clear();

                return message;
            }
        }
    }
}

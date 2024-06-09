using Ikiru.Parsnips.Api.Filters.Unauthorized;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.DatapoolLoopback
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class DpLoopbackController : ControllerBase
    {
        private readonly IDataPoolApi m_DataPoolApi;
        private readonly List<string> m_Messages = new List<string>();
        private readonly DataPoolApiHttpClient m_HttpClient;

        private Stopwatch m_Stopwatch = Stopwatch.StartNew();

        public DpLoopbackController(IDataPoolApi dataPoolApi, DataPoolApiHttpClient httpClient)
        {
            m_DataPoolApi = dataPoolApi;

            m_HttpClient = httpClient;
        }

        [HttpGet("[action]")]
        public IActionResult GetDirect([FromQuery] int statusCode) => ProcessResponse(statusCode);

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRefitAsync([FromQuery] int statusCode, CancellationToken cancellationToken)
        {
            m_Stopwatch = Stopwatch.StartNew();
            Log("Starting...");
            try
            {
                var result = await m_DataPoolApi.GetLoopback(statusCode, cancellationToken);
                Log($"Loopback succeeded, result: {result}.");

                return ProcessResponse(statusCode);
            }
            catch (Exception ex)
            {
                return ProcessResponse(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync([FromQuery] int statusCode, CancellationToken cancellationToken)
        {
            m_Stopwatch = Stopwatch.StartNew();
            Log("Starting...");
            try
            {
                var client = m_HttpClient.HttpClient.Value;
                var response = await client.GetAsync($"/api/v1.0/loopback?statusCode={statusCode}", cancellationToken);
                var result = await response.Content.ReadAsStringAsync();
                Log($"Loopback succeeded, result: {result}.");

                return ProcessResponse(statusCode);
            }
            catch (Exception ex)
            {
                return ProcessResponse(ex);
            }
        }

        private void Log(string message) => m_Messages.Add($"{DateTimeOffset.UtcNow:s} - Stopwatch: {m_Stopwatch.ElapsedMilliseconds:D5} ms. {message}");

        private IActionResult ThrowException()
        {
            Log("Throwing exception...");

            var message = GetMessagesAndFinalize();

            throw new Exception(message);
        }

        private string GetMessagesAndFinalize()
        {
            Log("Last message...");
            var message = string.Join("\r\n", m_Messages);
            m_Messages.Clear();

            return message;
        }

        private IActionResult ProcessResponse(int statusCode)
            => statusCode switch
            {
                400 => throw new ParamValidationFailureException(nameof(statusCode), $"{statusCode} Error\r\n{GetMessagesAndFinalize()}"),
                401 => throw new UnauthorizedException(),
                404 => throw new ResourceNotFoundException(nameof(statusCode), $"{statusCode} Error\r\n{GetMessagesAndFinalize()}"),
                500 => throw new Exception($"{nameof(statusCode)} - {statusCode} Error\r\n{GetMessagesAndFinalize()}"),
                200 => new OkObjectResult($"{nameof(statusCode)} - {statusCode} Error\r\n{GetMessagesAndFinalize()}"),
                _ => new ObjectResult($"loopback exception - \r\n{GetMessagesAndFinalize()}")
                {
                    StatusCode = statusCode
                }
            };

        private IActionResult ProcessResponse(Exception ex)
        {
            var resultCode = Regex.Match(ex.Message, @"\d+").Value;
            return int.TryParse(resultCode, out var statusCode) ? ProcessResponse(statusCode) : ThrowException();
        }
    }
}

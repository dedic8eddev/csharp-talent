using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using MediatR;

namespace Ikiru.Parsnips.Api.Controllers.Recaptcha
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public string Token { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.Token)
                   .NotEmpty();
            }
        }

        public class Result
        {
            public bool Success { get; set; }
            public DateTimeOffset? ChallengeTimestamp { get; set; }
            public string HostName { get; set; }
            public IEnumerable<string> ErrorCodes { get; set; }
            public double Score { get; set; }
            public string Action { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly RecaptchaService m_RecaptchaService;
            private readonly IMapper m_Mapper;

            public Handler(RecaptchaService recaptchaService,
                           IMapper mapper)
            {
                m_Mapper = mapper;
                m_RecaptchaService = recaptchaService;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var recaptchaResponse = await m_RecaptchaService.Verify(request.Token);

                return m_Mapper.Map<Result>(recaptchaResponse);
            }
        }
    }
}

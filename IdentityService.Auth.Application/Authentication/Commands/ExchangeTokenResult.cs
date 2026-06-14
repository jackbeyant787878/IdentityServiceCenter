using MediatR;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Auth.Application.Authentication.Commands
{
    public record ExchangeTokenCommand(OpenIddictRequest HttpContext) : IRequest<ExchangeTokenResult>;

    public class ExchangeTokenResult
    {
        public bool IsSuccess { get; private set; }
        public ClaimsPrincipal? Principal { get; private set; }
        public string? ErrorType { get; private set; } // "Forbid", "BadRequest"
        public string? ErrorCode { get; private set; }
        public string? ErrorDescription { get; private set; }

        public static ExchangeTokenResult Success(ClaimsPrincipal principal)
            => new() { IsSuccess = true, Principal = principal };

        public static ExchangeTokenResult Forbid()
            => new() { IsSuccess = false, ErrorType = "Forbid" };

        public static ExchangeTokenResult BadRequest(string errorCode, string description)
            => new() { IsSuccess = false, ErrorType = "BadRequest", ErrorCode = errorCode, ErrorDescription = description };
    }
}

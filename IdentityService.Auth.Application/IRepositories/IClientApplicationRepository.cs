using IdentityService.Auth.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Auth.Application.IRepositories
{
    public interface IClientApplicationRepository
    {
        Task AddAsync(ClientApplication application, string cleanSecret, CancellationToken cancellationToken);
    }
}

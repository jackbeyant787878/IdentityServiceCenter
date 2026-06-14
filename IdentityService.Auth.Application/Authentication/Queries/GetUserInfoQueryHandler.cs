using IdentityService.Auth.Application.IRepositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Auth.Application.Authentication.Queries
{
    public record GetUserInfoQuery(Guid UserId) : IRequest<UserResponse?>;
    public record UserResponse(Guid userId, string userName, string userType, long merchantId, long storeId, string status,bool isActive);
    public class GetUserInfoQueryHandler(ISysUserRepository userRepository) : IRequestHandler<GetUserInfoQuery, UserResponse?>
    {
        public  async Task<UserResponse?> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
        {

            var user = await userRepository.GetWithPermissionsAsync(request.UserId);
            var userInfo = new UserResponse(user.Id, user.Username, user.UserType.ToString(), 
            user.BelongMerchantId.Value, user.BelongStoreId.Value, "Active", user.IsActive);
            return userInfo;
        }
    }
}

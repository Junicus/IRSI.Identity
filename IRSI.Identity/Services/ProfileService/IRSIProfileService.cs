using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IRSI.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace IRSI.Identity.Services.ProfileService
{
    public class IRSIProfileService : IProfileService
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public IRSIProfileService(IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, UserManager<ApplicationUser> userManager)
        {
            _claimsFactory = claimsFactory;
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();

            var user = await _userManager.FindByIdAsync(sub);
            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims.ToList();

            //Add requested claims
            claims = claims.Where(claim => context.RequestedClaimTypes.Contains(claim.Type)).ToList();

            //Add given_name claim
            claims.Add(new Claim(JwtClaimTypes.GivenName, user.Name));

            //add email claim
            claims.Add(new Claim(JwtClaimTypes.Email, user.Email));

            var roleclaims = principal.Claims.ToList();
            roleclaims = roleclaims.Where(claim => claim.Type == "role").ToList();
            foreach (var roleclaim in roleclaims)
            {
                //if client is id manager and user is in admin role, add idManager.admin role
                if (context.Client.ClientId == "idManage.js" && roleclaim.Value == "admin")
                {
                    claims.Add(new Claim(JwtClaimTypes.Role, "idManager.admin"));
                }
                claims.Add(new Claim(JwtClaimTypes.Role, roleclaim.Value));
            }

            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}
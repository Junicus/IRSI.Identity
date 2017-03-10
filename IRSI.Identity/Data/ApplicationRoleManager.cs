using IRSI.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IRSI.Identity.Data
{
    public class ApplicationRoleManager : RoleManager<ApplicationRole>
    {
        public ApplicationRoleManager(IRoleStore<ApplicationRole> roleStore,
            IEnumerable<IRoleValidator<ApplicationRole>> roleValidator,
            ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors,
            ILogger<RoleManager<ApplicationRole>> logger,
            IHttpContextAccessor contextAccessor)
            : base(roleStore, roleValidator, keyNormalizer, errors, logger, contextAccessor)
        {
        }
    }
}


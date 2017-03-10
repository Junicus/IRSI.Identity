using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IRSI.Identity.Models;
using Microsoft.AspNetCore.Identity;
using IRSI.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace IRSI.Identity.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationRoleManager _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationRoleManager roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            return new JsonResult(await _userManager.Users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                _detailLink = Url.Action("GetUser", "Users", new { id = u.Id })
            }).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == id);
            var roles = await _roleManager.Roles.ToListAsync();

            if(user != null)
            {
                return new JsonResult(new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    roles = user.Roles.Select(r => new { id = r.RoleId, name = roles.Single(t => t.Id == r.RoleId).Name }),
                    claims = user.Claims.Select(c => new { type = c.ClaimType, value = c.ClaimValue })
                });
            }
            return NotFound();
        }
    }
}
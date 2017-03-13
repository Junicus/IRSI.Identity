﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IRSI.Identity.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult TestLogin()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IRSI.Identity.ViewModels.ConsentViewModels;
using IRSI.Identity.Services.Consent;

namespace IRSI.Identity.Controllers
{
    [SecurityHeaders]
    public class ConsentController : Controller
    {
        private readonly ConsentService _consent;

        public ConsentController(ConsentService consent)
        {
            _consent = consent;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl)
        {
            var vm = await _consent.BuildViewModelAsync(returnUrl);
            if(vm != null)
            {
                return View("Index", vm);
            }

            return View("Error");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsentInputModel model)
        {
            var result = await _consent.ProcessConsent(model);
            if (result.IsRedirect)
            {
                return Redirect(result.RedirectUri);
            }

            if (result.HasValidationError)
            {
                ModelState.AddModelError("", result.ValidationError);
            }

            if (result.ShowView)
            {
                return View("Index", result.ViewModel);
            }

            return View("Error");
        }
    }
}
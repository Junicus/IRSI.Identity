using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IRSI.Identity.Models.Account;
using IRSI.Identity.ViewModels.AccountViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IRSI.Identity.Services.Account
{
    public class AccountService
    {
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(
            IClientStore clientStore,
            IIdentityServerInteractionService interaction,
            IHttpContextAccessor httpContextAccessor)
        {
            _clientStore = clientStore;
            _interaction = interaction;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                return new LoginViewModel
                {
                    EnableLocalLogin = false,
                    ReturnUrl = returnUrl,
                    Email = context?.LoginHint,
                    ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
                };
            }

            var schemes = _httpContextAccessor.HttpContext.Authentication.GetAuthenticationSchemes();
            var providers = schemes
                .Where(x => x.DisplayName != null && !AccountOptions.WindowsAuthenticationSchemes.Contains(x.AuthenticationScheme))
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.AuthenticationScheme
                }).ToList();

            if (AccountOptions.WindowsAuthenticationEnabled)
            {
                var windowsSchemes = schemes.Where(s => AccountOptions.WindowsAuthenticationSchemes.Contains(s.AuthenticationScheme));
                if (windowsSchemes.Any())
                {
                    providers.Add(new ExternalProvider
                    {
                        AuthenticationScheme = AccountOptions.WindowsAuthenticationSchemes.First(),
                        DisplayName = AccountOptions.WindowsAuthenticationDisplayName
                    });
                }
            }

            var allowLocal = true;
            if(context?.ClientId != null)
            {
                var client = await _clientStore.FindClientByIdAsync(context.ClientId);
                if(client != null)
                {
                    allowLocal = client.EnableLocalLogin;
                    if(client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        public async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Email = model.Email;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        public async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };
            var user = await _httpContextAccessor.HttpContext.GetIdentityServerUserAsync();
            if(user == null || user.Identity.IsAuthenticated == false)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if(context?.ShowSignoutPrompt == false)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            return vm;
        }

        public async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            var logout = await _interaction.GetLogoutContextAsync(logoutId);
            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = logout?.ClientId,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            var user = await _httpContextAccessor.HttpContext.GetIdentityServerUserAsync();
            if(user  != null)
            {
                var idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if(idp != null && idp!= IdentityServerConstants.LocalIdentityProvider)
                {
                    if(vm.LogoutId == null)
                    {
                        vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                    }
                    vm.ExternalAuthenticationScheme = idp;
                }
            }
            return vm;
        }
    }
}

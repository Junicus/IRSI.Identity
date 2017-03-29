using IdentityServer4;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IRSI.Identity.IdentityServer
{
    public static class Config
    {
        public static IEnumerable<ApiResource> GetApiResources()
        {
            var apiResources = new List<ApiResource>();
            apiResources.Add(new ApiResource
            {
                Name = "sos_api",
                DisplayName = "SOS Api",
                Description = "Lets you use the Speed of Service Api",
                UserClaims =
                {
                    "sosApiEvent",
                    "sosApiRole",
                    "sosApiRegion",
                    "sosApiStore"
                }, 
                Scopes = {
                    new Scope {
                        Name="sos_api",
                        DisplayName="SOS Api"
                    }
                }
            });

            apiResources.Add(new ApiResource
            {
                Name = "team_api",
                DisplayName = "TeamSales Api",
                Description = "Lets you use the TeamSales Api",
                UserClaims =
                {
                    "teamApiEvent",
                    "teamApiConcept",
                    "teamApiStore"
                },
                Scopes = {
                    new Scope {
                        Name="team_api",
                        DisplayName = "TeamSales Api"
                    }
                }
            });

            apiResources.Add(new ApiResource("id_manage", "Manage Identity"));

            return apiResources;
        }

        public static IEnumerable<Client> GetClients()
        {
            var clients = new List<Client>();
            clients.Add(new Client
            {
                ClientId = "mvc.hybrid",
                ClientName = "IRSI Services",
                ClientSecrets = {
                    new Secret("secret".Sha256())
                },
                AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                RequireConsent = true,
                RedirectUris = {
                    "http://localhost:52000/signin-oidc",
                    "https://irsiservices.azurewebsites.net/signin-oidc"
                },
                PostLogoutRedirectUris = {
                    "http://localhost:52000",
                    "https://irsiservices.azurewebsites.net/"
                },

                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "sos_api", "team_api", "irsi_identity"
                },
                AllowOfflineAccess = true
            });

            clients.Add(new Client
            {
                ClientId = "sosFileUploader",
                ClientName = "SOS File Uploader",
                ClientSecrets = {
                    new Secret("secret".Sha256())
                },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = {
                    "sos_api"
                },
                Claims =
                {
                    new Claim("sosApiEvent", "true", ClaimValueTypes.Boolean),
                    new Claim("sosApiRole", "office_manager")
                }
            });

            clients.Add(new Client
            {
                ClientId = "idManage.js",
                ClientName = "Identity Manager Client",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,
                RedirectUris = { 
                    "http://localhost:4200"
                },
                RequireConsent = false,
                PostLogoutRedirectUris = {
                    "http://localhost:4200"
                },
                AllowedCorsOrigins = {
                    "http://localhost:4200"
                },
                AllowedScopes =
                {   
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,                    
                    "id_manage"
                }
            });
            return clients;
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            var identityResources = new List<IdentityResource>();
            identityResources.Add(new IdentityResources.OpenId());
            identityResources.Add(new IdentityResources.Profile());
            identityResources.Add(new IdentityResources.Email());
            identityResources.Add(new IdentityResource()
            {
                Name = "irsi_identity",
                DisplayName = "IRSI Identity",
                Description = "Identity scope for IRSI users",
                UserClaims =
                {
                    "UseAVTService",
                    "UseTeamSalesService",
                    "UseSOSService",
                    "teamApiConcept",
                    "teamApiStore"
                }
            });

            return identityResources;
        }
    }
}
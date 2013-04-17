﻿// <copyright file="Startup.cs" company="Microsoft Open Technologies, Inc.">
// Copyright 2011-2013 Microsoft Open Technologies, Inc. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Owin;
using Owin.Types;
using Owin.Types.Extensions;

namespace Katana.Sandbox.WebServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHandlerAsync(async (req, res, next) =>
            {
                req.TraceOutput.WriteLine("{0} {1}{2}", req.Method, req.PathBase, req.Path);
                await next();
                req.TraceOutput.WriteLine("{0} {1}{2}", res.StatusCode, req.PathBase, req.Path);
            });

            var dataProtectionProvider = new MachineKeyDataProtectionProvider();

            app.UseFormsAuthentication(new FormsAuthenticationOptions
            {
                LoginPath = "/Login",
                LogoutPath = "/Logout",
                DataProtection = dataProtectionProvider.Create("Katana.Sandbox.WebServer", "Forms Cookie"),
                Provider = new FormsAuthenticationProvider()
            });

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                DataProtection = dataProtectionProvider.Create("Katana.Sandbox.WebServer", "OAuth Bearer Token"),
                Provider = new OAuthBearerAuthenticationProvider
                {
                    OnValidateIdentity = async context =>
                    {
                    }
                }
            });

            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                AuthorizeEndpointPath = "/Authorize",
                TokenEndpointPath = "/Token",
                DataProtection = dataProtectionProvider.Create("Katana.Sandbox.WebServer", "OAuth Bearer Token"),
                Provider = new OAuthAuthorizationServerProvider
                {
                    OnLookupClientId = async context =>
                    {
                        if (context.ClientId == "123456")
                        {
                            context.ClientFound("abcdef", "http://localhost:18429/ClientApp.aspx");
                        }
                    },
                    OnAuthorizeEndpoint = async context =>
                    {
                        var request = new OwinRequest(context.Environment);
                        var response = new OwinResponse(context.Environment);

                        var user = await request.Authenticate("Forms", "Basic");
                        if (user == null)
                        {
                            response.Unauthorized("Forms", "Basic");
                            context.RequestCompleted = true;
                        }
                        else
                        {
                            request.User = user;
                        }
                    },
                    OnTokenEndpoint = async context =>
                    {
                        context.Issue();
                    }
                }
            });

            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "api/{controller}");
            app.UseWebApi(config);
        }
    }
}
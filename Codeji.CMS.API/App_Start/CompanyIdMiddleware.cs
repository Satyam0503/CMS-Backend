using System;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

namespace Codeji.CMS.API.App_Start
{
    public class CompanyIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CompanyIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {

                    var companyId = jwtToken.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
                    context.Items["CompanyId"] = companyId; // Store it in the HttpContext for use
                }
            }
            //else
            //{

            //context.Items["CompanyId"] = "jay123";
            //}
            await _next(context);
        }
    }
}


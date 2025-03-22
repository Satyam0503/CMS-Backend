using System;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Newtonsoft.Json;

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
                string token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                JwtSecurityToken? jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {

                    string? companyId = jwtToken.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
                    context.Items["CompanyId"] = companyId; // Store it in the HttpContext for use
                }
            }
            else
            {

                //try
                //{
                //    HttpRequest? request = context.Request;
                //    request.EnableBuffering();
                //    using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, true))
                //    {
                //        string content = await reader.ReadToEndAsync();
                //        UserCheckModel userForEdit = JsonConvert.DeserializeObject<UserCheckModel>(content) ?? new UserCheckModel();
                //        request.Body.Position = 0;
                //        if (userForEdit != null && !string.IsNullOrEmpty(userForEdit.CompanyId))
                //            context.Items["CompanyId"] = userForEdit.CompanyId; // Store it in the HttpContext for use   

                //    }
                //}
                //catch (Exception) { }
                string companyId = context.Request.Headers["cId"].ToString();
                context.Items["CompanyId"] = companyId;
            }
            await _next(context);
        }
    }
}


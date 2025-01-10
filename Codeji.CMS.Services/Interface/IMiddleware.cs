using System;
using Codeji.CMS.DTO;

namespace Codeji.CMS.Services.Interface
{
    public interface IMiddlewareService
    {
        UserModel GetUserById(string usierId);
    }
}


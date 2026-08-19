using Codeji.CMS.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public class AuthorizationMetadataTests
{
    [Fact]
    public void UserController_RequiresAuthorization()
    {
        Assert.Contains(typeof(UserController).GetCustomAttributes(true), attribute => attribute is AuthorizeAttribute);
    }
}

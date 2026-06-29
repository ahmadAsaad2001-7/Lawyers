

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lawyers.Tests.Helpers;

public static class MockHelpers
{
    // Creates a fully mocked UserManager<T>
    public static UserManager<T> GetUserManager<T>() where T : class
    {
        var store = new Mock<IUserStore<T>>();
        var mgr = new Mock<UserManager<T>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<T>>().Object,
            Array.Empty<IUserValidator<T>>(),
            Array.Empty<IPasswordValidator<T>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<T>>>().Object
        );
        return mgr.Object;
    }

    // Creates a fully mocked SignInManager<T>
    public static SignInManager<T> GetSignInManager<T>(UserManager<T> userManager) where T : class
    {
        var context = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
        var mgr = new Mock<SignInManager<T>>(
            userManager,
            new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<T>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<T>>>().Object,
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<T>>().Object
        );
        mgr.Setup(x => x.Context).Returns(context.Object);
        return mgr.Object;
    }
}
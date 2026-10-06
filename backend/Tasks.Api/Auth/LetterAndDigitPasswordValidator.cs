using Microsoft.AspNetCore.Identity;
using Tasks.Api.Data;
using Tasks.Api.Validation;

namespace Tasks.Api.Auth;

public sealed class LetterAndDigitPasswordValidator : IPasswordValidator<ApplicationUser>
{
    /// <summary>
    /// Applies the shared password rule and returns an Identity result.
    /// </summary>
    /// <param name="manager">The user manager calling the validator.</param>
    /// <param name="user">The account being created.</param>
    /// <param name="password">The password as typed. It is not rewritten.</param>
    /// <returns>Success, or a failure that carries the English policy message.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password)
    {
        if (FieldRules.TryPassword(password, out var error))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordPolicy",
            Description = error,
        }));
    }
}

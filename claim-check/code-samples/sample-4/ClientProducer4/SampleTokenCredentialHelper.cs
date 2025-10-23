using Azure.Core;
using System.IdentityModel.Tokens.Jwt;

namespace Azure.Identity;

internal static class SampleTokenCredentialHelper
{

    /// <summary>
    /// Extract the user principal from the token credential.
    /// </summary>     
    public static string GetUserPrincipal(this DefaultAzureCredential azureCredential)
    {
        const string DefaultEntraIdCredentialContext = "https://management.azure.com/.default";

        var token = azureCredential.GetToken(new TokenRequestContext([DefaultEntraIdCredentialContext]), CancellationToken.None);
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token.Token);
        return jwtSecurityToken.Subject;
    }
}

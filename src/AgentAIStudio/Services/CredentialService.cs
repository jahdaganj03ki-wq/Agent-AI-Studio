using Windows.Security.Credentials;

namespace AgentAIStudio.Services;

public class CredentialService
{
    private const string ResourceName = "AgentAIStudio";
    private const string UserName = "ApiKey";

    public async Task SaveApiKeyAsync(string apiKey)
    {
        await Task.CompletedTask;
        var vault = new PasswordVault();
        try
        {
            var existing = vault.Retrieve(ResourceName, UserName);
            vault.Remove(existing);
        }
        catch { }

        var credential = new PasswordCredential(ResourceName, UserName, apiKey);
        vault.Add(credential);
    }

    public async Task<string?> LoadApiKeyAsync()
    {
        await Task.CompletedTask;
        var vault = new PasswordVault();
        try
        {
            var credential = vault.Retrieve(ResourceName, UserName);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return null;
        }
    }

    public async Task ClearApiKeyAsync()
    {
        await Task.CompletedTask;
        var vault = new PasswordVault();
        try
        {
            var existing = vault.Retrieve(ResourceName, UserName);
            vault.Remove(existing);
        }
        catch { }
    }

    public async Task<bool> HasApiKeyAsync()
    {
        return await LoadApiKeyAsync() != null;
    }
}

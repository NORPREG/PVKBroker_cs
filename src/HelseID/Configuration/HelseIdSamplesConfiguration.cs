using HelseId.Samples.Common.Configuration;

namespace HelseId.Configuration;

/// <summary>
/// This class contains configurations that correspond to existing clients in the HelseID TEST environment.
/// </summary>
public class HelseIdSamplesConfiguration : HelseIdConfiguration
{
    private HelseIdSamplesConfiguration(
                SecurityKey privateKeyJwk,
                string clientId,
                string scope,
                bool clientCredentialsUseDpop,
                List<string>? resourceIndicators = null) :
            base(
                privateKeyJwk,
                clientId,
                scope,
                ConfigurationValues.StsUrl,
                resourceIndicators) {}

    // Configuration for the 'plain' client credentials application
    public static HelseIdSamplesConfiguration ClientCredentialsClient =>
        new(
            ConfigurationValues.PvkRsaKey,
            ConfigurationValues.PvkApiClientId,
            ConfigurationValues.PvkApiScope,
            ConfigurationValues.ClientCredentialsUseDpop);
}

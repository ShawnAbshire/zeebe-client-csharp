using System;
using Microsoft.Extensions.Logging;
using Zeebe.Client.Api.Builder;

namespace Zeebe.Client.Impl.Builder;

public class CamundaCloudClientBuilder : ICamundaCloudClientBuilder, ICamundaCloudClientBuilderStep1,
    ICamundaCloudClientBuilderStep2, ICamundaCloudClientBuilderFinalStep
{
    private const string ZeebeAddressEnvVar = "ZEEBE_ADDRESS";
    private const string ZeebeClientIdEnvVar = "ZEEBE_CLIENT_ID";
    private const string ZeebeClientSecretEnvVar = "ZEEBE_CLIENT_SECRET";
    private const string ZeebeAuthServerEnvVar = "ZEEBE_AUTHORIZATION_SERVER_URL";

    private readonly CamundaCloudTokenProviderBuilder camundaCloudTokenProviderBuilder;
    private string gatewayAddress;
    private ILoggerFactory loggerFactory;

    private CamundaCloudClientBuilder()
    {
        camundaCloudTokenProviderBuilder = CamundaCloudTokenProvider.Builder();
    }

    public ICamundaCloudClientBuilderStep1 UseClientId(string clientId)
    {
        _ = camundaCloudTokenProviderBuilder.UseClientId(clientId);
        return this;
    }

    public ICamundaCloudClientBuilderFinalStep FromEnv()
    {
        _ = UseClientId(GetFromEnv(ZeebeClientIdEnvVar))
            .UseClientSecret(GetFromEnv(ZeebeClientSecretEnvVar))
            .UseContactPoint(GetFromEnv(ZeebeAddressEnvVar))
            .UseAuthServer(GetFromEnv(ZeebeAuthServerEnvVar));
        return this;
    }

    public ICamundaCloudClientBuilderFinalStep UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        _ = camundaCloudTokenProviderBuilder.UseLoggerFactory(this.loggerFactory);
        return this;
    }

    public ICamundaCloudClientBuilderFinalStep UseAuthServer(string url)
    {
        if (url is null)
        {
            // use default
            return this;
        }

        _ = camundaCloudTokenProviderBuilder.UseAuthServer(url);
        return this;
    }

    public ICamundaCloudClientBuilderFinalStep UsePersistedStoragePath(string path)
    {
        if (path is null)
        {
            // use default
            return this;
        }

        _ = camundaCloudTokenProviderBuilder.UsePath(path);
        return this;
    }

    public IZeebeClient Build()
    {
        return ZeebeClient.Builder()
            .UseLoggerFactory(loggerFactory)
            .UseGatewayAddress(gatewayAddress)
            .UseTransportEncryption()
            .UseAccessTokenSupplier(camundaCloudTokenProviderBuilder.Build())
            .Build();
    }

    public ICamundaCloudClientBuilderStep2 UseClientSecret(string clientSecret)
    {
        _ = camundaCloudTokenProviderBuilder.UseClientSecret(clientSecret);
        return this;
    }

    public ICamundaCloudClientBuilderFinalStep UseContactPoint(string contactPoint)
    {
        _ = contactPoint ?? throw new ArgumentNullException(nameof(contactPoint));

        if (!contactPoint.EndsWith(":443"))
        {
            gatewayAddress = contactPoint + ":443";
            _ = camundaCloudTokenProviderBuilder.UseAudience(contactPoint);
        }
        else
        {
            gatewayAddress = contactPoint;
            _ = camundaCloudTokenProviderBuilder.UseAudience(contactPoint.Replace(":443", ""));
        }

        return this;
    }

    public static ICamundaCloudClientBuilder Builder()
    {
        return new CamundaCloudClientBuilder();
    }

    private string GetFromEnv(string key)
    {
        char[] charsToTrim =[' ', '\''];
        return Environment.GetEnvironmentVariable(key)?.Trim(charsToTrim);
    }
}
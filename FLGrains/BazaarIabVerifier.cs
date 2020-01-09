using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    public class BazaarIabVerifier : Grain, IBazaarIabVerifier
    {
        string? bazaarAccessCode = null;
        HttpClient httpClient = new HttpClient();

        ISystemSettingsProvider settings;
        ILogger logger;


        public BazaarIabVerifier(ISystemSettingsProvider settings, ILogger<BazaarIabVerifier> logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        async Task RefreshBazaarAccessCode()
        {
            try
            {
                if (bazaarAccessCode != null)
                    return;

                var conf = settings.Settings.Values;
                var values = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", conf.BazaarClientID },
                    { "client_secret", conf.BazaarClientSecret },
                    { "refresh_token", conf.BazaarRefreshToken }
                };
                var content = new FormUrlEncodedContent(values);

                var response = await httpClient.PostAsync("https://pardakht.cafebazaar.ir/devapi/v2/auth/token/", content);
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

                if (response.IsSuccessStatusCode)
                {
                    bazaarAccessCode = (string)result["access_token"] ?? throw new Exception("failed to read access_token from response");
                }
                else
                {
                    logger.LogError(0, $"Failed to refresh bazaar access code with status {response.StatusCode} and error response {result["error"]} - {result["error_description"]}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(0, $"Failed to refresh bazaar access code due to {ex}");
            }
        }

        public async Task<IabPurchaseResult> VerifyBazaarPurchase(string sku, string token)
        {
            if (bazaarAccessCode == null)
            {
                await RefreshBazaarAccessCode();
                if (bazaarAccessCode == null)
                {
                    logger.LogError(0, "Failed to get Bazaar access code, check configured credentials");
                    return IabPurchaseResult.FailedToContactValidationService;
                }
            }

            try
            {
                var response = await httpClient.GetAsync(
                    $"https://pardakht.cafebazaar.ir/devapi/v2/api/validate/{settings.Settings.Values.BazaarPackageName}/" +
                    $"inapp/{sku}/purchases/{token}/?access_token={bazaarAccessCode}"
                    );

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(0, $"Bazaar purchase {sku} {token} verified");
                    return IabPurchaseResult.Success;
                }
                else
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = Newtonsoft.Json.Linq.JObject.Parse(resultJson);

                    var error = (string)result["error"];
                    if (error == "not_found")
                        return IabPurchaseResult.Invalid;
                    else if (error == "invalid_credentials")
                    {
                        bazaarAccessCode = null;
                        return await VerifyBazaarPurchase(sku, token);
                    }
                    else
                    {
                        logger.LogError(0, $"Validating bazaar purchase {sku} {token} failed with {response.StatusCode} {resultJson}");

                        return IabPurchaseResult.FailedToContactValidationService;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(0, $"Failed to get purchase status from bazaar due to: {ex}");
                return IabPurchaseResult.UnknownError;
            }
        }
    }
}

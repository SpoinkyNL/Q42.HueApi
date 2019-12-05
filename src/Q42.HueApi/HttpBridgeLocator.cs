using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;

namespace Q42.HueApi
{
  /// <summary>
  /// Uses the special nupnp url from meethue.com to find registered bridges based on your external IP
  /// </summary>
  public class HttpBridgeLocator : IBridgeLocator
  {
    private readonly Uri NuPnPUrl = new Uri("https://discovery.meethue.com");

    /// <summary>
    /// Locate bridges
    /// </summary>
    /// <param name="timeout">Timeout before stopping the search</param>
    /// <returns>List of bridge IPs found</returns>
    public async Task<IEnumerable<LocatedBridge>> LocateBridgesAsync(TimeSpan timeout)
    {
      if (timeout <= TimeSpan.Zero)
      {
        throw new ArgumentException("Timeout value must be greater than zero.", nameof(timeout));
      }

      using (CancellationTokenSource cancelSource = new CancellationTokenSource(timeout))
      {
        return await LocateBridgesAsync(cancelSource.Token);
      }
    }

    /// <summary>
    /// Locate bridges
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the search</param>
    /// <returns>List of bridge IPs found</returns>
    public async Task<IEnumerable<LocatedBridge>> LocateBridgesAsync(CancellationToken cancellationToken)
    {
      using (HttpClient client = new HttpClient())
      {
        var response = await client.GetAsync(NuPnPUrl, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode && !cancellationToken.IsCancellationRequested)
        {
          string content = await response.Content.ReadAsStringAsync();

          NuPnPResponse[] responseModel = JsonConvert.DeserializeObject<NuPnPResponse[]>(content);

          return responseModel.Select(x => new LocatedBridge() { BridgeId = x.Id, IpAddress = x.InternalIpAddress }).ToList();
        }
        else
        {
          return new List<LocatedBridge>();
        }
      }
    }
  }
}

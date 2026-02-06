using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WhatTheDob.Application.Interfaces.Services.External;

namespace WhatTheDob.Infrastructure.Services.External
{
    public class MenuApiClient : IMenuApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MenuApiClient> _logger;

        public MenuApiClient(HttpClient httpClient, ILogger<MenuApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetMenuDataAsync(string url, string? menuDate = null, string? meal = null, int? campusId = null)
        {
            var hasFilters = !string.IsNullOrWhiteSpace(menuDate) || !string.IsNullOrWhiteSpace(meal) || campusId.HasValue;
            var method = hasFilters ? "POST" : "GET";
            
            _logger.LogDebug("Fetching menu data: Method={Method}, URL={URL}, Date={Date}, Meal={Meal}, CampusId={CampusId}", 
                method, url, menuDate ?? "N/A", meal ?? "N/A", campusId?.ToString() ?? "N/A");

            try
            {
                using var request = new HttpRequestMessage(hasFilters ? HttpMethod.Post : HttpMethod.Get, url);

                if (hasFilters)
                {
                    var formValues = new List<KeyValuePair<string, string>>();

                    if (!string.IsNullOrWhiteSpace(menuDate))
                    {
                        formValues.Add(new KeyValuePair<string, string>("selMenuDate", menuDate));
                    }

                    if (!string.IsNullOrWhiteSpace(meal))
                    {
                        formValues.Add(new KeyValuePair<string, string>("selMeal", meal));
                    }

                    if (campusId.HasValue)
                    {
                        formValues.Add(new KeyValuePair<string, string>("selCampus", campusId.Value.ToString()));
                    }

                    request.Content = new FormUrlEncodedContent(formValues);
                }

                using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogInformation("Successfully fetched menu data: Method={Method}, URL={URL}, ContentLength={ContentLength}", 
                    method, url, content?.Length ?? 0);
                
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed: Method={Method}, URL={URL}, Date={Date}, Meal={Meal}, CampusId={CampusId}", 
                    method, url, menuDate ?? "N/A", meal ?? "N/A", campusId?.ToString() ?? "N/A");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching menu data: Method={Method}, URL={URL}", method, url);
                throw;
            }
        }
    }
}

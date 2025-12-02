using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WhatTheDob.Application.Interfaces.Services.External;

namespace WhatTheDob.Infrastructure.Services.External
{
    public class MenuApiClient : IMenuApiClient
    {
        private readonly HttpClient _httpClient;

        public MenuApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetMenuDataAsync(string url, string? menuDate = null, string? meal = null, int? campusId = null)
        {
            var hasFilters = !string.IsNullOrWhiteSpace(menuDate) || !string.IsNullOrWhiteSpace(meal) || campusId.HasValue;

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

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}

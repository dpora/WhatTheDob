using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WhatTheDob.Application.Interfaces.Services.External;

namespace WhatTheDob.Infrastructure.Services.External
{
    public class MenuApiClient : IMenuApiClient
    {
        private static readonly HttpClient _client = new HttpClient();

        public async Task<string> GetMenuDataAsync(string url, string menuDate, string meal, int campusId)
        {
            //Build the POST request
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent($"selMenuDate={menuDate}&selMeal={meal}&selCampus={campusId}");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            //Send the request
            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            //Return the response content as a string
            return await response.Content.ReadAsStringAsync();
        }
    }
}

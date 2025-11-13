using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WhatTheDob.API
{
    public class MenuApiCaller
    {
        private static readonly HttpClient _client = new HttpClient();

        public async Task<string> GetMenuDataAsync(string url, string menuDate, string meal, string campus)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("selMenuDate", menuDate),
                new KeyValuePair<string, string>("selMeal", meal),
                new KeyValuePair<string, string>("selCampus", campus),
            });

            var response = await _client.PostAsync(url, data);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}

using Microsoft.Extensions.Configuration;
using WhatTheDob.Core.Entities;
using WhatTheDob.Core.Services;
using WhatTheDob.Core.Services.External;
using WhatTheDob.Core.Mapping;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WhatTheDob.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IMenuService declared in Core. Interfaces for Infrastructure classes are
    /// placed in the Core project to define stable contracts and decouple higher-level layers from
    /// concrete infrastructure implementations.
    /// </summary>
    public class MenuService : IMenuService
    {
        private readonly int _daysToFetch;
        private readonly string[] _meals;
        private readonly string _menuApiUrl;
        private readonly IMenuApiClient _menuApiClient;
        private readonly IMenuItemMapper _menuParser;
        private readonly int _campusId;

        public MenuService(IConfiguration configuration, IMenuApiClient menuApiClient, IMenuItemMapper menuParser)
        {
            _daysToFetch = configuration.GetValue<int>("DaysToFetch", 7);
            _meals = configuration.GetValue<string[]>("Meals", new string[] { "Breakfast", "Lunch", "Dinner" });
            _menuApiUrl = configuration.GetValue<string>("MenuApiUrl", "https://www.absecom.psu.edu/menus/user-pages/daily-menu.cfm");
            _campusId = configuration.GetValue<int>("CampusId", 46);

            _menuApiClient = menuApiClient;
            _menuParser = menuParser;
        }

        public async Task<List<Menu>> GetMenuPagesAsync()
        {
            var menus = new List<Menu>();

            for (int i = 0; i < _daysToFetch; i++)
            {
                var date = DateTime.Now.AddDays(i).ToString("MM/dd/yy");

                foreach (var meal in _meals)
                {
                    var menuHtml = await _menuApiClient.GetMenuDataAsync(_menuApiUrl, date, meal, _campusId);

                    if (!string.IsNullOrEmpty(menuHtml))
                    {
                        menus.Add(new Menu(
                            date,
                            meal,
                            _menuParser.ParseMenuItems(menuHtml),
                            _campusId
                        ));
                    }
                }
            }

            return menus;
        }
    }
}
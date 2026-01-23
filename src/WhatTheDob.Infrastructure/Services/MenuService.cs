using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.External;
using WhatTheDob.Domain.Entities;
using WhatTheDob.Infrastructure.Interfaces.Mapping;
using WhatTheDob.Infrastructure.Interfaces.Persistence;

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
        private readonly IMenuRepository _menuRepository;
        private readonly IMenuItemMapper _menuParser;
        private readonly IMenuFilterMapper _menuFilterMapper;
        private readonly int _campusId;

        public MenuService(
            IConfiguration configuration,
            IMenuApiClient menuApiClient,
            IMenuRepository menuRepository,
            IMenuItemMapper menuParser,
            IMenuFilterMapper menuFilterMapper)
        {
            var fetchSettings = configuration.GetSection("MenuFetch");

            _daysToFetch = fetchSettings.GetValue<int>("DaysToFetch", 7);
            _meals = fetchSettings.GetSection("Meals").Get<string[]>() ?? ["Breakfast", "Lunch", "Dinner"];
            _menuApiUrl = fetchSettings.GetValue<string>("MenuApiUrl", "https://www.absecom.psu.edu/menus/user-pages/daily-menu.cfm");
            _campusId = fetchSettings.GetValue<int>("SelectedCampus", 46);

            _menuApiClient = menuApiClient;
            _menuRepository = menuRepository;
            _menuParser = menuParser;
            _menuFilterMapper = menuFilterMapper;
        }
        // FetchMenusFromApiAsync fetches menu data from an external API, processes it, and stores it in the repository.
        // It retrieves campus and meal options, iterates over the specified days (_daysToFetch). 
        public async Task<List<Menu>> FetchMenusFromApiAsync()
        {
            var menus = new List<Menu>();

            var filterHtml = await _menuApiClient.GetMenuDataAsync(_menuApiUrl).ConfigureAwait(false);

            var campusOptionsRaw = _menuFilterMapper.ParseCampusOptions(filterHtml);
            var mealOptions = _menuFilterMapper.ParseMealOptions(filterHtml);

            var campusMap = new Dictionary<int, string>();
            foreach (var option in campusOptionsRaw)
            {
                if (int.TryParse(option.Key, out var campusId))
                {
                    campusMap[campusId] = option.Value;
                }
            }

            await _menuRepository.UpsertCampusesAsync(campusMap).ConfigureAwait(false);
            await _menuRepository.UpsertMealsAsync(mealOptions).ConfigureAwait(false);

            var campusIdsToProcess = campusMap.Keys.ToList();

            if (campusIdsToProcess.Count == 0)
            {
                campusIdsToProcess.Add(_campusId);
            }

            var configuredMeals = new HashSet<string>(_meals, StringComparer.OrdinalIgnoreCase);
            var mealsFromFilters = mealOptions
                .Where(meal => !string.IsNullOrWhiteSpace(meal))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var mealsToProcess = mealsFromFilters.Count > 0
                ? mealsFromFilters.Where(meal => configuredMeals.Count == 0 || configuredMeals.Contains(meal)).ToList()
                : new List<string>(_meals);

            if (mealsToProcess.Count == 0)
            {
                mealsToProcess.AddRange(_meals);
            }

            for (int i = 0; i < _daysToFetch; i++)
            {
                var date = DateTime.Now.AddDays(i).ToString("MM/dd/yy");

                foreach (var campusId in campusIdsToProcess)
                {
                    foreach (var meal in mealsToProcess)
                    {
                        var menuHtml = await _menuApiClient.GetMenuDataAsync(_menuApiUrl, date, meal, campusId).ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(menuHtml))
                        {
                            menus.Add(new Menu(
                                date,
                                meal,
                                _menuParser.ParseMenuItems(menuHtml),
                                campusId));
                        }
                    }
                }
            }

            if (menus.Count > 0)
            {
                await _menuRepository.UpsertMenusAsync(menus).ConfigureAwait(false);
            }

            return menus;
        }
        // GetMenuAsync retrieves a menu for a specific date, campus, and meal from the repository and maps it to the domain entity.
        public async Task<Menu> GetMenuAsync(string date, int campusId, int mealId)
        {
            var menuMappings = await _menuRepository.GetMenuMappingsAsync(date, campusId, mealId).ConfigureAwait(false);
            if (menuMappings == null)
            {
                return null;
            }
            var domainMenu = new Menu
            {
                Date = date,
                CampusId = campusId,
                Meal = mealId.ToString(),
                Items = [.. menuMappings.Select(mm => new MenuItem
                {
                    Value = mm.MenuItem.Value,
                    Category = mm.MenuItem.Category.Value,
                    Tags = string.IsNullOrEmpty(mm.MenuItem.Tags) ? new List<string>() : mm.MenuItem.Tags.Split(',').ToList(),
                    TotalRating = mm.MenuItem.ItemRating.TotalRating,
                    RatingCount = mm.MenuItem.ItemRating.RatingCount
                })]
            };
            return domainMenu;
        }

        public async Task<IEnumerable<Campus>> GetCampusesAsync()
        {
            var campuses = await _menuRepository.GetCampusesAsync().ConfigureAwait(false);
            return campuses.Select(c => new Campus
            {
                Id = c.Id,
                Value = c.Value
            });
        }

        public async Task<IEnumerable<Meal>> GetMealsAsync()
        {
            var meals = await _menuRepository.GetMealsAsync().ConfigureAwait(false);
            return meals.Select(m => new Meal
            {
                Id = m.Id,
                Value = m.Value
            });
        }
    }
}
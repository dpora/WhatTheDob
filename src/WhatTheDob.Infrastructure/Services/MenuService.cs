using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MenuService> _logger;

        public MenuService(
            IConfiguration configuration,
            IMenuApiClient menuApiClient,
            IMenuRepository menuRepository,
            IMenuItemMapper menuParser,
            IMenuFilterMapper menuFilterMapper,
            ILogger<MenuService> logger)
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
            _logger = logger;
            
            _logger.LogInformation("MenuService initialized with DaysToFetch={DaysToFetch}, SelectedCampus={CampusId}, MenuApiUrl={MenuApiUrl}", 
                _daysToFetch, _campusId, _menuApiUrl);
        }
        // FetchMenusFromApiAsync fetches menu data from an external API, processes it, and stores it in the repository.
        // It retrieves campus and meal options, iterates over the specified days (_daysToFetch). 
        public async Task<List<Menu>> FetchMenusFromApiAsync()
        {
            _logger.LogInformation("Starting menu fetch from API for {DaysToFetch} days", _daysToFetch);

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
            
            _logger.LogInformation("Parsed {CampusCount} campuses and {MealCount} meals from filter data", 
                campusMap.Count, mealOptions.Count());

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

            // Create a list to hold all the tasks then await them all at once
            var menuTasks = new List<Task<Menu?>>();
            for (int i = 0; i < _daysToFetch; i++)
            {
                var date = DateTime.Now.AddDays(i).ToString("MM/dd/yy");

                foreach (var campusId in campusIdsToProcess)
                {
                    foreach (var meal in mealsToProcess)
                    {
                        // Capture loop variables to avoid closure issues
                        var capturedDate = date;
                        var capturedCampusId = campusId;
                        var capturedMeal = meal;

                        _logger.LogDebug("Creating task: GetMenuDataAsync({MenuApiUrl}, {Date}, {Meal}, {CampusId})", 
                            _menuApiUrl, capturedDate, capturedMeal, capturedCampusId);

                        menuTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var menuHtml = await _menuApiClient.GetMenuDataAsync(_menuApiUrl, capturedDate, capturedMeal, capturedCampusId).ConfigureAwait(false);

                                if (string.IsNullOrEmpty(menuHtml)) return null;

                                return new Menu(
                                    capturedDate,
                                    capturedMeal,
                                    _menuParser.ParseMenuItems(menuHtml),
                                    capturedCampusId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error fetching menu for Date={Date}, Meal={Meal}, Campus={CampusId}", 
                                    capturedDate, capturedMeal, capturedCampusId);
                                return null;
                            }
                        }));
                    }
                }
            }

            // Await all tasks concurrently
            _logger.LogInformation("Awaiting {TaskCount} menu fetch tasks", menuTasks.Count);
            var results = await Task.WhenAll(menuTasks).ConfigureAwait(false);

            // Filter out nulls (failed requests or empty html)
            var menus = results.OfType<Menu>().ToList();
            
            _logger.LogInformation("Successfully fetched {MenuCount} menus out of {TaskCount} attempts", 
                menus.Count, menuTasks.Count);

            if (menus.Count > 0)
            {
                await _menuRepository.UpsertMenusAsync(menus).ConfigureAwait(false);
                _logger.LogInformation("Menus successfully upserted to repository");
            }

            return menus;
        }

        // Overloaded FetchMenusFromApiAsync fetches menu data for a specific date from an external API, processes it, and stores it in the repository.
        public async Task<List<Menu>> FetchMenusFromApiAsync(string date)
        {
            var menus = new List<Menu>();

            var filterHtml = await _menuApiClient.GetMenuDataAsync(_menuApiUrl).ConfigureAwait(false);

            var campusOptionsRaw = _menuFilterMapper.ParseCampusOptions(filterHtml);
            var mealOptions = _menuFilterMapper.ParseMealOptions(filterHtml);

            var campusMap = new Dictionary<int, string>();
            var validCampusOptions = campusOptionsRaw
                .Where(option => int.TryParse(option.Key, out _));

            foreach (var option in validCampusOptions)
            {
                var campusId = int.Parse(option.Key);
                campusMap[campusId] = option.Value;
            }

            async Task<(List<int> campusIdsToProcess, List<string> mealsToProcess)> InitializeCampusesAndMealsAsync()
            {
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

                return (campusIdsToProcess, mealsToProcess);
            }

            var initializationResult = await InitializeCampusesAndMealsAsync().ConfigureAwait(false);
            var campusIdsToProcess = initializationResult.campusIdsToProcess;
            var mealsToProcess = initializationResult.mealsToProcess;
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

            if (menus.Count > 0)
            {
                await _menuRepository.UpsertMenusAsync(menus).ConfigureAwait(false);
            }

            return menus;
        }

        // GetMenuAsync retrieves a menu for a specific date, campus, and meal from the repository and maps it to the domain entity.
        public async Task<Menu> GetMenuAsync(string date, int campusId, int mealId)
        {
            _logger.LogInformation("Retrieving menu for Date={Date}, CampusId={CampusId}, MealId={MealId}", 
                date, campusId, mealId);
                
            var menuMappings = await _menuRepository.GetMenuMappingsAsync(date, campusId, mealId).ConfigureAwait(false);
            if (menuMappings == null || !menuMappings.Any())
            {
                _logger.LogWarning("No menu found for Date={Date}, CampusId={CampusId}, MealId={MealId}", 
                    date, campusId, mealId);
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
            
            _logger.LogInformation("Successfully retrieved menu with {ItemCount} items for Date={Date}, CampusId={CampusId}, MealId={MealId}", 
                domainMenu.Items.Count, date, campusId, mealId);
            
            return domainMenu;
        }

        public async Task<IEnumerable<Campus>> GetCampusesAsync()
        {
            _logger.LogDebug("Retrieving campuses from repository");
            var campuses = await _menuRepository.GetCampusesAsync().ConfigureAwait(false);
            var campusesList = campuses.ToList();
            _logger.LogInformation("Retrieved {CampusCount} campuses", campusesList.Count);
            return campusesList.Select(c => new Campus
            {
                Id = c.Id,
                Value = c.Value
            });
        }

        public async Task<IEnumerable<Meal>> GetMealsAsync()
        {
            _logger.LogDebug("Retrieving meals from repository");
            var meals = await _menuRepository.GetMealsAsync().ConfigureAwait(false);
            var mealsList = meals.ToList();
            _logger.LogInformation("Retrieved {MealCount} meals", mealsList.Count);
            return mealsList.Select(m => new Meal
            {
                Id = m.Id,
                Value = m.Value
            });
        }

        public async Task SubmitUserRatingAsync(string sessionId, string itemValue, int rating)
        {
            _logger.LogInformation("Submitting user rating: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}", 
                sessionId, itemValue, rating);
                
            // Normalize values to avoid accidental duplicates due to whitespace/casing
            var trimmedSessionId = sessionId?.Trim();
            var trimmedItemValue = itemValue?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedSessionId))
            {
                _logger.LogWarning("Rating submission failed: Session ID is empty");
                throw new ArgumentException("Session id is required.", nameof(trimmedSessionId));
            }

            if (string.IsNullOrWhiteSpace(trimmedItemValue))
            {
                _logger.LogWarning("Rating submission failed: Item value is empty");
                throw new ArgumentException("Item value is required.", nameof(trimmedItemValue));
            }

            if (rating < 1 || rating > 5)
            {
                _logger.LogWarning("Rating submission failed: Rating {Rating} is out of range (1-5)", rating);
                throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");
            }

            await _menuRepository.UpsertUserRatingAsync(trimmedSessionId, trimmedItemValue, rating).ConfigureAwait(false);
            _logger.LogInformation("User rating successfully submitted: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}", 
                trimmedSessionId, trimmedItemValue, rating);
        }
    }
}
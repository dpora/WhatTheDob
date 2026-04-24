using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Domain.Entities;
using MenuModel = WhatTheDob.Domain.Entities.Menu;

namespace WhatTheDob.Web.Components.Pages
{
    public partial class Menu : ComponentBase
    {
        [Inject]
        private IMenuService MenuService { get; set; } = default!;

        [Inject]
        private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private ILogger<Menu> Logger { get; set; } = default!;

        [Inject]
        private IConfiguration Configuration { get; set; } = default!;

        private MenuModel? _menu;
        private bool _isLoading;
        private bool _isLoadingFilters = true;
        private bool _isSubmittingRating;
        private string? _menuErrorMessage;
        private string? _ratingErrorMessage;
        private bool _isRateLimitBannerVisible;
        private double _rateLimitCooldownPercent;
        private CancellationTokenSource? _rateLimitCooldownCts;

        private const int RateLimitCooldownSeconds = 60;
        private const int RateLimitTickMilliseconds = 200;

        private List<Campus> _campuses = new();
        private List<Meal> _meals = new();

        private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);
        private int? _selectedCampusId;
        private int? _selectedMealId;

        private Dictionary<string, int> _userRatings = new();
        private Dictionary<string, string> _tagMappings = new(StringComparer.OrdinalIgnoreCase);
        private string? _sessionId;

        protected override async Task OnInitializedAsync()
        {
            _tagMappings = Configuration.GetSection("TagMappings").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _sessionId = HttpContextAccessor.HttpContext?.Request.Cookies["UserSessionId"];
            Logger.LogInformation("Menu page initialized with SessionId={SessionId}", _sessionId ?? "null");

            await LoadUserRatingsFromCookiesAsync();

            try
            {
                Logger.LogDebug("Loading campuses and meals filters");
                var campusesTask = MenuService.GetCampusesAsync();
                var mealsTask = MenuService.GetMealsAsync();

                await Task.WhenAll(campusesTask, mealsTask);

                _campuses = (await campusesTask).ToList();
                _meals = (await mealsTask).ToList();

                if (_campuses.Any())
                {
                    _selectedCampusId = _campuses.First().Id;
                }

                if (_meals.Any())
                {
                    _selectedMealId = _meals.First().Id;
                }

                Logger.LogInformation("Filters loaded successfully: {CampusCount} campuses, {MealCount} meals",
                    _campuses.Count, _meals.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load filters in Menu page initialization");
                _menuErrorMessage = "Failed to load filters. Please refresh the page.";
            }
            finally
            {
                _isLoadingFilters = false;
            }
        }

        private async Task LoadMenuAsync()
        {
            if (!_selectedCampusId.HasValue || !_selectedMealId.HasValue)
            {
                _menuErrorMessage = "Please select both a campus and a meal.";
                Logger.LogWarning("Menu load attempted without valid selections: CampusId={CampusId}, MealId={MealId}",
                    _selectedCampusId, _selectedMealId);
                StateHasChanged();
                return;
            }

            _isLoading = true;
            _menuErrorMessage = null;
            StateHasChanged();

            try
            {
                var formattedDate = _selectedDate.ToString("MM/dd/yy");
                Logger.LogInformation("Loading menu for Date={Date}, CampusId={CampusId}, MealId={MealId}, SessionId={SessionId}",
                    formattedDate, _selectedCampusId.Value, _selectedMealId.Value, _sessionId ?? "null");

                _menu = await MenuService.GetMenuAsync(formattedDate, _selectedCampusId.Value, _selectedMealId.Value);

                if (_menu is null)
                {
                    _menuErrorMessage = "No menu found for the selected date, campus, and meal.";
                    Logger.LogInformation("No menu found for Date={Date}, CampusId={CampusId}, MealId={MealId}",
                        formattedDate, _selectedCampusId.Value, _selectedMealId.Value);
                }
                else
                {
                    Logger.LogInformation("Menu loaded successfully with {ItemCount} items for Date={Date}, CampusId={CampusId}, MealId={MealId}",
                        _menu.Items.Count, formattedDate, _selectedCampusId.Value, _selectedMealId.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load menu for Date={Date}, CampusId={CampusId}, MealId={MealId}",
                    _selectedDate.ToString("MM/dd/yy"), _selectedCampusId.Value, _selectedMealId.Value);
                _menuErrorMessage = "Failed to load menu. Please try again.";
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private async Task SubmitRating(string itemValue, int rating)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                _ratingErrorMessage = "Session ID not found. Please refresh the page.";
                Logger.LogWarning("Rating submission attempted without session ID for ItemValue={ItemValue}", itemValue);
                StateHasChanged();
                return;
            }

            if (!_isRateLimitBannerVisible)
            {
                _ratingErrorMessage = null;
            }

            _isSubmittingRating = true;
            StateHasChanged();

            try
            {
                Logger.LogInformation("Submitting rating: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}",
                    _sessionId, itemValue, rating);

                var (isSuccess, failureReason) = await MenuService.SubmitUserRatingAsync(_sessionId, itemValue, rating);

                if (!isSuccess)
                {
                    _ratingErrorMessage = failureReason ?? "Failed to submit rating. Please try again.";

                    if (IsRateLimitFailure(_ratingErrorMessage))
                    {
                        ShowRateLimitBanner(_ratingErrorMessage);
                    }

                    Logger.LogWarning("Rating submission rejected: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}, Reason={Reason}",
                        _sessionId, itemValue, rating, _ratingErrorMessage);
                    return;
                }

                _ratingErrorMessage = null;

                _userRatings[itemValue] = rating;
                await SaveUserRatingToCookiesAsync(itemValue, rating);
                await CleanupMismatchedRatingCookiesAsync();

                if (_menu != null && _selectedCampusId.HasValue && _selectedMealId.HasValue)
                {
                    var formattedDate = _selectedDate.ToString("MM/dd/yy");
                    _menu = await MenuService.GetMenuAsync(formattedDate, _selectedCampusId.Value, _selectedMealId.Value);
                }

                Logger.LogInformation("Rating submitted successfully: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}",
                    _sessionId, itemValue, rating);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to submit rating: SessionId={SessionId}, ItemValue={ItemValue}, Rating={Rating}",
                    _sessionId, itemValue, rating);
                _ratingErrorMessage = "Failed to submit rating. Please try again.";
            }
            finally
            {
                _isSubmittingRating = false;
                StateHasChanged();
            }
        }

        private static bool IsRateLimitFailure(string? failureReason)
        {
            return !string.IsNullOrWhiteSpace(failureReason)
                && failureReason.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowRateLimitBanner(string message)
        {
            _ratingErrorMessage = message;
            _isRateLimitBannerVisible = true;
            _rateLimitCooldownPercent = 100;

            _rateLimitCooldownCts?.Cancel();
            _rateLimitCooldownCts?.Dispose();

            _rateLimitCooldownCts = new CancellationTokenSource();
            _ = RunRateLimitCooldownAsync(_rateLimitCooldownCts.Token);
        }

        private async Task RunRateLimitCooldownAsync(CancellationToken cancellationToken)
        {
            var cooldownStart = DateTime.UtcNow;
            var cooldownDuration = TimeSpan.FromSeconds(RateLimitCooldownSeconds);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var elapsed = DateTime.UtcNow - cooldownStart;
                    if (elapsed >= cooldownDuration)
                    {
                        break;
                    }

                    var remaining = cooldownDuration - elapsed;
                    _rateLimitCooldownPercent = Math.Clamp((remaining.TotalMilliseconds / cooldownDuration.TotalMilliseconds) * 100d, 0d, 100d);

                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(RateLimitTickMilliseconds, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _isRateLimitBannerVisible = false;
            _ratingErrorMessage = null;
            _rateLimitCooldownPercent = 0;

            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            _rateLimitCooldownCts?.Cancel();
            _rateLimitCooldownCts?.Dispose();
        }

        private double GetDisplayRating(MenuItem item)
        {
            var userRating = GetUserRating(item.Value);
            if (userRating > 0)
            {
                return userRating;
            }

            if (item.RatingCount == 0)
            {
                return 0;
            }

            return (double)item.TotalRating / item.RatingCount;
        }

        private int GetFillPercent(double rating, int starIndex)
        {
            if (rating >= starIndex) return 100;
            if (rating < starIndex - 1) return 0;
            return (int)((rating - (starIndex - 1)) * 100);
        }

        private int GetUserRating(string itemValue)
        {
            return _userRatings.TryGetValue(itemValue, out var rating) ? rating : 0;
        }

        private string FormatCampusName(string campusName)
        {
            if (string.IsNullOrEmpty(campusName))
                return campusName;

            if (campusName.Contains(" - "))
            {
                return campusName.Replace(" - ", ": ");
            }

            if (campusName.Contains("@"))
            {
                var colonIndex = campusName.IndexOf(':');
                var atIndex = campusName.IndexOf('@');

                if (colonIndex > -1 && atIndex > colonIndex)
                {
                    var prefix = campusName.Substring(0, colonIndex);
                    var suffix = campusName.Substring(atIndex + 1).Trim();
                    return $"{prefix}: {suffix}";
                }
            }

            return campusName;
        }

        private string GetTagShortName(string tag)
        {
            return _tagMappings.TryGetValue(tag, out var shortName) ? shortName : tag;
        }
    }
}

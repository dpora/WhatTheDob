using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatTheDob.Application.Interfaces.Services;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;

namespace WhatTheDob.Infrastructure.Services.BackgroundTasks
{
    /// <summary>
    /// Implementation of IDailyMenuJob that schedules and runs daily menu related background tasks.
    /// Interface is declared in Core and implemented here in Infrastructure to allow separation of concerns.
    /// </summary>
    public class DailyMenuJob : IDailyMenuJob
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyMenuJob> _logger;

        public DailyMenuJob(IServiceScopeFactory scopeFactory, ILogger<DailyMenuJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task RunTaskAsync(int daysOffsetValue)
        {
            var dateToFetch = DateTime.Now.AddDays(daysOffsetValue).ToString("MM/dd/yy");
            _logger.LogInformation("Daily menu job starting: Date={Date}, Offset={DaysOffset}, CurrentTime={CurrentTime}", 
                dateToFetch, daysOffsetValue, DateTime.Now);
                
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var menuService = scope.ServiceProvider.GetRequiredService<IMenuService>();
                await menuService.FetchMenusFromApiAsync(dateToFetch).ConfigureAwait(false);
                _logger.LogInformation("Daily menu job completed successfully for Date={Date}", dateToFetch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily menu job failed for Date={Date}, Offset={DaysOffset}", 
                    dateToFetch, daysOffsetValue);
                throw;
            }
        }

        public void ScheduleDailyTask(int daysOffsetValue)
        {
            if (_timer != null)
            {
                _logger.LogWarning("Daily menu job timer already scheduled, skipping duplicate schedule");
                return;
            }

            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1); // tomorrow 00:00
            TimeSpan initialDelay = nextMidnight - now;
            TimeSpan interval = TimeSpan.FromDays(1);

            _timer = new Timer(async _ => await RunTaskAsync(daysOffsetValue).ConfigureAwait(false), null, initialDelay, interval);

            _logger.LogInformation("Daily menu job scheduled: FirstRun={FirstRun}, Interval={Interval}, DaysOffset={DaysOffset}", 
                nextMidnight, interval, daysOffsetValue);
        }
    }
}

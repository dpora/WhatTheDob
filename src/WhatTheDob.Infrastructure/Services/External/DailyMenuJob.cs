using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

        public DailyMenuJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task RunTaskAsync(int daysOffsetValue)
        {
            var dateToFetch = DateTime.Now.AddDays(daysOffsetValue).ToString("MM/dd/yy");
            Console.WriteLine("Scheduled task running at: " + DateTime.Now);
            using var scope = _scopeFactory.CreateScope();
            var menuService = scope.ServiceProvider.GetRequiredService<IMenuService>();
            await menuService.FetchMenusFromApiAsync(dateToFetch).ConfigureAwait(false);
        }

        public void ScheduleDailyTask(int daysOffsetValue)
        {
            if (_timer != null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1); // tomorrow 00:00
            TimeSpan initialDelay = now - now;
            TimeSpan interval = TimeSpan.FromDays(1);

            _timer = new Timer(async _ => await RunTaskAsync(daysOffsetValue).ConfigureAwait(false), null, initialDelay, interval);

            Console.WriteLine("First run at: " + nextMidnight);
        }
    }
}

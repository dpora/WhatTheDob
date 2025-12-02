using System;
using System.Threading;
using System.Threading.Tasks;
using WhatTheDob.Application.Interfaces.Services.BackgroundTasks;
using Microsoft.Extensions.Configuration;

namespace WhatTheDob.Infrastructure.Services.BackgroundTasks
{
    /// <summary>
    /// Implementation of IDailyMenuJob that schedules and runs daily menu related background tasks.
    /// Interface is declared in Core and implemented here in Infrastructure to allow separation of concerns.
    /// </summary>
    public class DailyMenuJob : IDailyMenuJob
    {
        private bool _initialRun = true;
        private static Timer _timer;
        private readonly IConfiguration _configuration;

        public DailyMenuJob(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task RunTaskAsync()
        {
            Console.WriteLine("Scheduled task running at: " + DateTime.Now);
            // Place real async work here, e.g., calling MenuService
            await Task.CompletedTask;
        }

        public void ScheduleMidnightTask()
        {
            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1); // tomorrow 00:00
            TimeSpan initialDelay = nextMidnight - now;
            TimeSpan interval = TimeSpan.FromDays(1);

            _timer = new Timer(async _ => await RunTaskAsync(), null, initialDelay, interval);

            Console.WriteLine("First run at: " + nextMidnight);
        }
    }
}

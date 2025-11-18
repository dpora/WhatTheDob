namespace WhatTheDob.API
{
    public class ScheduledTask
    {
        private bool _initialRun = true;
        private static Timer _timer;

        protected void ExecuteAsync(IConfiguration configuration)
        {
            var menuController = new MenuController(configuration);

            if (_initialRun)
            {
                _initialRun = false;
                // Perform initial setup or tasks here
            }
            else
            {
                // Perform scheduled tasks here
            }
            menuController.GetMenuPagesAsync().GetAwaiter().GetResult();
            ScheduleMidnightTask();
        }


        private static async Task RunTask()
        {
            // Place the code you want to run at midnight here
            Console.WriteLine("Scheduled task running at: " + DateTime.Now);
            await Task.CompletedTask; // Placeholder for async work
        }
        // Schedules the task to run at midnight every day
        protected static void ScheduleMidnightTask()
        {
            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1); // tomorrow 00:00

            TimeSpan initialDelay = nextMidnight - now;
            TimeSpan interval = TimeSpan.FromDays(1);

            // Set up the timer to trigger at midnight and then every 24 hours
            _timer = new Timer(async _ => await RunTask(), null, initialDelay, interval);

            Console.WriteLine("First run at: " + nextMidnight);
        }
    }
}

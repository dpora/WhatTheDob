using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatTheDob.Application.Interfaces.Services.BackgroundTasks
{
    public interface IDailyMenuJob
    {
        Task RunTaskAsync(int daysOffsetValue);

        void ScheduleDailyTask(int daysOffsetValue);
    }
}

using System;
namespace WhatTheDob.Application.Interfaces.Services
{
    public interface IRatingThrottleService
    {
        /// <summary>
        /// Checks whether a submission for the given sessionId is allowed and records it if allowed.
        /// Returns true when the submission is permitted, false when the rate limit has been exceeded.
        /// </summary>
        bool IsAllowedAndRecord(string sessionId);
    }
}

using System.Threading.Tasks;

namespace WhatTheDob.Application.Interfaces.Services.External
{
 /// <summary>
 /// Contract for making HTTP calls to the external menu API. Implemented in the Infrastructure project.
 /// </summary>
 public interface IMenuApiClient
 {
 Task<string> GetMenuDataAsync(string url, string menuDate, string meal, int campusId);
 }
}

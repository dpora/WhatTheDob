namespace WhatTheDob.API
{
    public class MenuController
    {
        private readonly int _daysToFetch;
        private readonly string[] _meals;
        private readonly string _menuApiUrl;
        private readonly MenuApiCaller _menuApiCaller;
        private readonly MenuParser _menuParser;

        // Constructor to initialize configuration values
        public MenuController(IConfiguration configuration)
        {
            _daysToFetch = configuration.GetValue<int>("DaysToFetch", 7);
            _meals = configuration.GetValue<string[]>("Meals", ["Breakfast", "Lunch", "Dinner"]);
            _menuApiUrl = configuration.GetValue<string>("MenuApiUrl", "https://www.absecom.psu.edu/menus/user-pages/daily-menu.cfm");
            _menuApiCaller = new MenuApiCaller();
            _menuParser = new MenuParser();
        }

        // Fetches menu pages for the next _daysToFetch days and all meals
        public async Task<entities.Menu> GetMenuPagesAsync()
        {
            // Get today's date in MM/dd/yy format
            var date = DateTime.Now.ToString("MM/dd/yy");
            // Loop through the next _daysToFetch days
            for (int i = 0; i < _daysToFetch; i++)
            {
                // Calculate the date for the current iteration
                foreach (var meal in _meals)
                {
                    // Fetch menu data from the API
                    var menuHTML = await _menuApiCaller.GetMenuDataAsync(_menuApiUrl, date, meal, 46);
                    if (!string.IsNullOrEmpty(menuHTML))
                    {
                        //Parse menuData into Menu object and return it
                        return new entities.Menu(
                            date,
                            meal,
                            _menuParser.ParseMenuItems(menuHTML),
                            46
                        );
                    }
                }
            }

            return null;
        }
    }
}

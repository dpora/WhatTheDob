using System;
using HtmlAgilityPack;
using MenuApiController = WhatTheDob.API.MenuApiCaller;
using Entities = WhatTheDob.API.entities;

namespace WhatTheDob.API
{
    public class MenuParser
    {
        private readonly int _daysToFetch;
        private readonly string[] _meals;
        private readonly string _menuApiUrl;
        private readonly MenuApiCaller _menuApiController;

        public MenuParser(IConfiguration configuration)
        {
            _daysToFetch = configuration.GetValue<int>("DaysToFetch", 7);
            _meals = configuration.GetValue<string[]>("Meals", ["Breakfast", "Lunch", "Dinner"]);
            _menuApiUrl = configuration.GetValue<string>("MenuApiUrl", "https://www.absecom.psu.edu/menus/user-pages/daily-menu.cfm");
            _menuApiController = new MenuApiCaller();
        }

        public async Task<Entities.Menu> GetMenusPageAsync()
        {
            var date = DateTime.Now.ToString("MM/dd/yy");
            for (int i = 0; i < _daysToFetch; i++)
            {
                foreach (var meal in _meals)
                {
                    var menuHTML = await _menuApiController.GetMenuDataAsync(_menuApiUrl, date, meal, "Behrend");
                    if (!string.IsNullOrEmpty(menuHTML))
                    {
                        // Parse menuData into Menu object and return it
                        return new Entities.Menu(
                            date,
                            meal,
                            ParseMenuItems(menuHTML),
                            "Behrend"
                        );
                    }
                }
            }

            return null;
        }
        public List<Entities.MenuItem> ParseMenuItems(string html)
        {
            //Load HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var menuItems = new List<Entities.MenuItem>();
            string currentCategory = "";

            //Select all category headers and menu-item divs
            var nodes = doc.DocumentNode.SelectNodes("//h2[@class='category-header'] | //div[@class='menu-items']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node.Name == "h2")
                    {
                        //Update current category
                        currentCategory = node.InnerText.Trim();
                    }
                    else if (node.Name == "div")
                    {
                        //Create a new MenuItem
                        var item = new Entities.MenuItem();
                        item.Category = currentCategory;

                        //Get <a> tag for value
                        var link = node.SelectSingleNode(".//a");
                        if (link != null)
                        {
                            item.Value = link.GetAttributeValue("aria-label", "").Trim();
                        }

                        //Get all <img> tags for tags
                        var imgNodes = node.SelectNodes(".//img");
                        if (imgNodes != null)
                        {
                            foreach (var img in imgNodes)
                            {
                                var tag = img.GetAttributeValue("aria-label", "").Trim();
                                if (!string.IsNullOrEmpty(tag))
                                    item.Tags.Add(tag);
                            }
                        }

                        menuItems.Add(item);
                    }
                }
            }
            return menuItems;
        }
    }
}

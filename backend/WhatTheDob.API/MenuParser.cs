using HtmlAgilityPack;
using Entities = WhatTheDob.API.entities;
using System.Collections.Generic;
using System.Linq;


namespace WhatTheDob.API
{
    public class MenuParser
    {
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

        // Parses campus options from the provided HTML and returns a dictionary of campus values and names
        public Dictionary<string, string> ParseCampusOptions(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var campusSelect = doc.DocumentNode.SelectSingleNode("//select[@id='selCampus']");
            var campusDict = new Dictionary<string, string>();

            if (campusSelect != null)
            {
                foreach (var option in campusSelect.SelectNodes("./option"))
                {
                    var value = option.GetAttributeValue("value", "").Trim();
                    var text = option.InnerText.Trim();

                    if (!string.IsNullOrWhiteSpace(value) && value != "0")
                    {
                        campusDict[value] = text;
                    }
                }
            }

            return campusDict;
        }

        // Parses meal options from the provided HTML and returns a list of meal names
        public List<string> ParseMealOptions(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var mealSelect = doc.DocumentNode.SelectSingleNode("//select[@id='selMeal']");
            var mealList = new List<string>();

            if (mealSelect != null)
            {
                foreach (var option in mealSelect.SelectNodes("./option"))
                {
                    var text = option.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        mealList.Add(text);
                    }
                }
            }

            return mealList;
        }
    }
}

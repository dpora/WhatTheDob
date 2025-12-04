using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WhatTheDob.Infrastructure.Interfaces.Mapping;

namespace WhatTheDob.Infrastructure.Mapping
{
    public class MenuFilterMapper : IMenuFilterMapper
    {
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

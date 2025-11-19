using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatTheDob.Core.Entities;
using WhatTheDob.Core.Mapping;
using HtmlAgilityPack;

namespace WhatTheDob.Infrastructure.Mapping
{
    public class MenuItemMapper : IMenuItemMapper
    {
        public List<MenuItem> ParseMenuItems(string html)
        {
            //Load HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var menuItems = new List<MenuItem>();
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
                        var item = new MenuItem();
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

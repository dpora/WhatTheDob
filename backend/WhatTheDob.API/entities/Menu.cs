namespace WhatTheDob.API.entities
{
    public class Menu(string date, string meal, List<MenuItem> items, string campus)
    {
        public string Date { get; set; } = date;
        public string Meal { get; set; } = meal;
        public List<MenuItem> Items { get; set; } = items;
        public string Campus { get; set; } = campus;
    }
    public class MenuItem
    {
        public string Value { get; set; }
        public List<string> Tags { get; set; }
        public string Category { get; set; }
        public MenuItem(string value, List<string> tags, string category)
        {
            Value = value;
            Tags = tags;
            Category = category;
        }

        public MenuItem()
        {
        }
    }
}

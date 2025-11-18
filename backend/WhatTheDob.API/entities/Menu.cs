namespace WhatTheDob.API.entities
{
    public class Menu
    {
        public string Date { get; set; }
        public string Meal { get; set; }
        public List<MenuItem> Items { get; set; }
        public int CampusId { get; set; }

        // Parameterized constructor
        public Menu(string date, string meal, List<MenuItem> items, int campusId)
        {
            Date = date;
            Meal = meal;
            Items = items;
            CampusId = campusId;
        }

        // Default constructor
        public Menu() : this(string.Empty, string.Empty, new List<MenuItem>(), 0)
        {
        }
    }

    public class MenuItem
    {
        public string Value { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Category { get; set; }

        // Parameterized constructor
        public MenuItem(string value, List<string> tags, string category)
        {
            Value = value;
            Tags = tags;
            Category = category;
        }

        // Default constructor
        public MenuItem() : this(string.Empty, new List<string>(), string.Empty)
        {
        }
    }
}

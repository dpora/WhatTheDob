using System.Collections.Generic;
using WhatTheDob.Domain.Entities;

namespace WhatTheDob.Domain.Entities
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

}

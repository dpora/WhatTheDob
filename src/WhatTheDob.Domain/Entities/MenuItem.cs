using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatTheDob.Domain.Entities
{
    public class MenuItem
    {
        public string Value { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Category { get; set; }
        public int TotalRating { get; set; }
        public int RatingCount { get; set; }

        public MenuItem(string value, List<string> tags, string category, int totalRating, int ratingCount)
        {
            Value = value;
            Tags = tags;
            Category = category;
            TotalRating = totalRating;
            RatingCount = ratingCount;
        }

        public MenuItem() : this(string.Empty, new List<string>(), string.Empty, 0, 0)
        {
        }
    }
}

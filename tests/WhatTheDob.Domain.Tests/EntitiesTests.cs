using System.Collections.Generic;
using FluentAssertions;
using WhatTheDob.Domain.Entities;
using Xunit;

namespace WhatTheDob.Domain.Tests;

public class EntitiesTests
{
    [Fact]
    public void Menu_default_ctor_initializes_defaults()
    {
        var menu = new Menu();

        menu.Date.Should().BeEmpty();
        menu.Meal.Should().BeEmpty();
        menu.CampusId.Should().Be(0);
        menu.Items.Should().NotBeNull();
        menu.Items.Should().BeEmpty();
    }

    [Fact]
    public void Menu_parameterized_ctor_sets_properties()
    {
        var items = new List<MenuItem> { new("Pasta", ["Vegan"], "Entree", 10, 2) };

        var menu = new Menu("01/01/25", "Lunch", items, 46);

        menu.Date.Should().Be("01/01/25");
        menu.Meal.Should().Be("Lunch");
        menu.CampusId.Should().Be(46);
        menu.Items.Should().BeSameAs(items);
    }

    [Fact]
    public void MenuItem_default_ctor_initializes_defaults()
    {
        var item = new MenuItem();

        item.Value.Should().BeEmpty();
        item.Category.Should().BeEmpty();
        item.Tags.Should().NotBeNull();
        item.Tags.Should().BeEmpty();
        item.TotalRating.Should().Be(0);
        item.RatingCount.Should().Be(0);
    }

    [Fact]
    public void MenuItem_parameterized_ctor_sets_properties()
    {
        var tags = new List<string> { "Vegan", "Gluten Free" };

        var item = new MenuItem("Tofu", tags, "Entree", 5, 1);

        item.Value.Should().Be("Tofu");
        item.Category.Should().Be("Entree");
        item.Tags.Should().BeSameAs(tags);
        item.TotalRating.Should().Be(5);
        item.RatingCount.Should().Be(1);
    }

    [Fact]
    public void Campus_and_Meal_hold_id_and_value()
    {
        var campus = new Campus { Id = 7, Value = "Main" };
        var meal = new Meal { Id = 3, Value = "Dinner" };

        campus.Id.Should().Be(7);
        campus.Value.Should().Be("Main");
        meal.Id.Should().Be(3);
        meal.Value.Should().Be("Dinner");
    }
}

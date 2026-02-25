using FluentAssertions;
using WhatTheDob.Infrastructure.Mapping;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class MenuItemMapperTests
{
    [Fact]
    public void ParseMenuItems_builds_items_with_category_and_tags()
    {
        var html = """
        <h2 class='category-header'>Entree</h2>
        <div class='menu-items'>
            <a aria-label='Pasta'></a>
            <img aria-label='Vegan' />
            <img aria-label='Gluten Free' />
        </div>
        <h2 class='category-header'>Dessert</h2>
        <div class='menu-items'>
            <a aria-label='Cake'></a>
        </div>
        """;

        var mapper = new MenuItemMapper();

        var items = mapper.ParseMenuItems(html);

        items.Should().HaveCount(2);
        items[0].Category.Should().Be("Entree");
        items[0].Value.Should().Be("Pasta");
        items[0].Tags.Should().BeEquivalentTo(new[] { "Vegan", "Gluten Free" });
        items[1].Category.Should().Be("Dessert");
        items[1].Value.Should().Be("Cake");
        items[1].Tags.Should().BeEmpty();
    }
}

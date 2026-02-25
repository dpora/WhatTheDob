using System.Linq;
using FluentAssertions;
using WhatTheDob.Infrastructure.Mapping;
using Xunit;

namespace WhatTheDob.Infrastructure.Tests;

public class MenuFilterMapperTests
{
    [Fact]
    public void ParseCampusOptions_extracts_valid_entries_and_skips_zero()
    {
        var html = """
        <select id='selCampus'>
            <option value="0">Select</option>
            <option value="46">Main Campus</option>
            <option value="99">Downtown</option>
        </select>
        """;

        var mapper = new MenuFilterMapper();

        var result = mapper.ParseCampusOptions(html);

        result.Should().HaveCount(2);
        result["46"].Should().Be("Main Campus");
        result["99"].Should().Be("Downtown");
    }

    [Fact]
    public void ParseMealOptions_returns_all_non_empty_options()
    {
        var html = """
        <select id='selMeal'>
            <option value="42" aria-label="Beaver - Brodhead Bistro" >
              Beaver - Brodhead Bistro
            </option>

            <option value="47" aria-label="Behrend - Bruno's" >
              Behrend - Bruno's
            </option>

            <option value="" aria-label="" >
            </option>
        </select>
        """;

        var mapper = new MenuFilterMapper();

        var result = mapper.ParseMealOptions(html);

        result.Should().BeEquivalentTo(new[] { "Beaver - Brodhead Bistro", "Behrend - Bruno's" });
    }
}

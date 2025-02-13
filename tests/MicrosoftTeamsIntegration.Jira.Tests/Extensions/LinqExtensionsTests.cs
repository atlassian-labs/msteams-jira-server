using MicrosoftTeamsIntegration.Jira.Extensions;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class LinqExtensionsTests
    {
        [Fact]
        public void DistinctBy_ShouldReturnDistinctElements()
        {
            // Arrange
            var items = new System.Collections.Generic.List<TestItem>
            {
                new TestItem { Id = 1, Name = "Item1" },
                new TestItem { Id = 2, Name = "Item2" },
                new TestItem { Id = 1, Name = "Item3" },
                new TestItem { Id = 3, Name = "Item4" }
            };

            // Act
            var distinctItems = System.Linq.Enumerable.ToList(items.DistinctBy(x => x.Id));

            // Assert
            Assert.Equal(3, distinctItems.Count);
            Assert.Contains(distinctItems, x => x.Name == "Item1");
            Assert.Contains(distinctItems, x => x.Name == "Item2");
            Assert.Contains(distinctItems, x => x.Name == "Item4");
        }

        private class TestItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}

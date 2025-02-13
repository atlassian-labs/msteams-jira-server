using System.Collections.Generic;
using AutoMapper;
using MicrosoftTeamsIntegration.Jira.Extensions;
using MicrosoftTeamsIntegration.Jira.Models.Interfaces;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class ResolutionContextExtensionsTests
    {
        [Fact]
        public void ExtractMappingOptions_ShouldReturnCorrectMappingOptions_WhenContextHasValidItems()
        {
            // Arrange
            var context = new TestResolutionContext
            {
                Items = new Dictionary<string, object>
                {
                    { "isQueryLinkRequest", "true" },
                    { "previewIconPath", "/path/to/icon" }
                }
            };

            // Act
            var result = context.ExtractMappingOptions();

            // Assert
            Assert.True(result.IsQueryLinkRequest);
            Assert.Equal("/path/to/icon", result.PreviewIconPath);
        }

        [Fact]
        public void ExtractMappingOptions_ShouldReturnDefaultMappingOptions_WhenContextHasNoItems()
        {
            // Arrange
            var context = new TestResolutionContext
            {
                Items = new Dictionary<string, object>()
            };

            // Act
            var result = context.ExtractMappingOptions();

            // Assert
            Assert.False(result.IsQueryLinkRequest);
            Assert.Null(result.PreviewIconPath);
        }

        [Fact]
        public void ExtractMappingOptions_ShouldReturnDefaultMappingOptions_WhenContextHasInvalidItems()
        {
            // Arrange
            var context = new TestResolutionContext
            {
                Items = new Dictionary<string, object>
                {
                    { "isQueryLinkRequest", "invalid" },
                    { "previewIconPath", string.Empty }
                }
            };

            // Act
            var result = context.ExtractMappingOptions();

            // Assert
            Assert.False(result.IsQueryLinkRequest);
            Assert.Null(result.PreviewIconPath);
        }

        private class TestResolutionContext : IResolutionContext
        {
            public object State { get; }
            public IDictionary<string, object> Items { get; set; }
            public bool TryGetItems(out Dictionary<string, object> items)
            {
                throw new System.NotImplementedException();
            }

            public IRuntimeMapper Mapper { get; }
            public Dictionary<ContextCacheKey, object> InstanceCache { get; }
        }
    }
}

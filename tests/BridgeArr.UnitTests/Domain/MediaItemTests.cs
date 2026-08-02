using BridgeArr.Domain.Entities;
using BridgeArr.Domain.Enums;

namespace BridgeArr.UnitTests.Domain;

public class MediaItemTests
{
    [Fact]
    public void MediaItem_Initializes_Collections_And_Defaults()
    {
        var mediaItem = new MediaItem
        {
            Title = "Inception",
            Type = MediaType.Movie
        };

        Assert.NotEqual(Guid.Empty, mediaItem.Id);
        Assert.NotNull(mediaItem.ExternalIds);
        Assert.NotNull(mediaItem.Tags);
        Assert.NotNull(mediaItem.Genres);
        Assert.NotNull(mediaItem.Collections);
        Assert.NotNull(mediaItem.Labels);
        Assert.NotNull(mediaItem.Artwork);
        Assert.Equal("Inception", mediaItem.Title);
        Assert.Equal(MediaType.Movie, mediaItem.Type);
    }
}

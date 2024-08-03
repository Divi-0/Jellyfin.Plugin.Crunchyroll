using System.Text.Json;
using AutoFixture.Xunit2;
using FluentAssertions;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments.Mappings;
using Xunit;

namespace JellyFin.Plugin.ExternalComments.Tests.Features.Crunchyroll.Client.Mappings;

public class CrunchyrollCommentsReponseMappingTests
{
    [Fact]
    public Task ReturnsMappedCommentsReponse_WhenMapping_GivenRealCrunchyrollResponseAsJson()
    {
        var crunchyRollResponse =
            JsonSerializer.Deserialize<CrunchyrollCommentsResponse>(Properties.Resources.CommentResponseJson,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                });

        var commentsResponse = crunchyRollResponse?.ToCommentResponse();

        commentsResponse.Should().NotBeNull();
        commentsResponse!.Comments.Should().NotBeEmpty();
        commentsResponse.Total.Should().Be(32);
        commentsResponse.Comments.First().Author.Should().Be("ku8k8k78k78");
        commentsResponse.Comments.First().Message.Should()
            .Be("Tokyo ghoul ist so ein guter anime und geile folge wie immer");
        commentsResponse.Comments.First().AvatarIconUri.Should().Be(
            """https://static.crunchyroll.com/assets/avatar/60x60/1042-jujutsu-kaisen-megumi-fushigoro.png""");
        commentsResponse.Comments.First().Likes.Should().Be(64);
        commentsResponse.Comments.First().RepliesCount.Should().Be(4);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReturnsAvatarUriNull_WhenMapping_GivenEmptyAvartarArray()
    {
        var crunchyRollResponse = new CrunchyrollCommentsResponse
        {
            Items = new List<CrunchyrollCommentsItem>()
            {
                new CrunchyrollCommentsItem
                {
                    User = new CrunchyrollCommentsItemUser()
                    {
                        Attributes = new CrunchyrollCommentsItemUserAttributes()
                        {
                            Username = "Username",
                            Avatar = new CrunchyrollCommentsItemUserAttributesAvatar()
                            {
                                Unlocked = Array.Empty<CrunchyrollCommentsItemUserAttributesAvatarIcon>()
                            }
                        }
                    },
                    Votes = new CrunchyrollCommentsItemVotes()
                    {
                        Like = 1
                    },
                    RepliesCount = 0,
                    Message = string.Empty
                }
            }
        };

        var toCommentsResponseAssertion = crunchyRollResponse?.Invoking(x => x.ToCommentResponse()).Should().NotThrow();

        toCommentsResponseAssertion.Should().NotBeNull();
        
        var commentsResponse = toCommentsResponseAssertion!.Subject;
        commentsResponse.Should().NotBeNull();
        commentsResponse!.Comments.Should().NotBeEmpty();
        commentsResponse.Comments.First().AvatarIconUri.Should().BeNull();

        return Task.CompletedTask;
    }

    [Theory]
    [AutoData]
    public Task ReturnsCompleteMappedCommentsReponse_WhenMapping_GivenRandomCrunchyrollCommentsResponse(
        CrunchyrollCommentsResponse crunchyrollCommentsResponse)
    {
        var commentsResponse = crunchyrollCommentsResponse?.ToCommentResponse();

        commentsResponse.Should().NotBeNull();
        commentsResponse!.Comments.Should().NotBeEmpty();
        commentsResponse.Total.Should().Be(crunchyrollCommentsResponse!.Items.Count);

        for (var i = 0; i < commentsResponse.Comments.Count; i++)
        {
            var commentItem = commentsResponse.Comments[i];
            commentItem.Author.Should().Be(crunchyrollCommentsResponse.Items[i].User.Attributes.Username);
            commentItem.Message.Should().Be(crunchyrollCommentsResponse.Items[i].Message);
            commentItem.AvatarIconUri.Should().Be(crunchyrollCommentsResponse.Items[i].User.Attributes.Avatar.Unlocked
                .FirstOrDefault(x => x.Type.Equals("icon_60", StringComparison.OrdinalIgnoreCase))?.Source);
            commentItem.Likes.Should().Be(crunchyrollCommentsResponse.Items[i].Votes.Like);
            commentItem.RepliesCount.Should().Be(crunchyrollCommentsResponse.Items[i].RepliesCount);
        }

        return Task.CompletedTask;
    }
}
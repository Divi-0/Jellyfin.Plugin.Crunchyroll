using Jellyfin.Plugin.Crunchyroll.Contracts.Comments;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class CommentItemFaker
{
    public static CommentItem Generate()
    {
        return new Bogus.Faker<CommentItem>()
            .RuleFor(x => x.Author, f => f.Person.UserName)
            .RuleFor(x => x.AvatarIconUri, f => f.Internet.UrlWithPath())
            .RuleFor(x => x.Message, f => f.Lorem.Sentences())
            .RuleFor(x => x.Likes, f => f.Random.Number())
            .RuleFor(x => x.RepliesCount, f => f.Random.Number())
            .Generate();
    }
}
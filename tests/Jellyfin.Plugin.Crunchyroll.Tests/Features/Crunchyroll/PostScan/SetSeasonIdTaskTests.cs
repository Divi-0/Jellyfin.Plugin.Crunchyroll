using System.Globalization;
using Bogus;
using FluentAssertions;
using FluentResults;
using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.MediaInfo;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Features.Crunchyroll.PostScan;

public class SetSeasonIdTaskTests
{
    private readonly SetSeasonIdTask _sut;

    private readonly IMediator _mediator;
    private readonly IPostSeasonIdSetTask[] _postSeasonIdSetTasks;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemRepository _itemRepository;
    private readonly IMediaSourceManager _mediaSourceManager;

    private readonly Faker _faker;

    public SetSeasonIdTaskTests()
    {
        _postSeasonIdSetTasks = Enumerable.Range(0, Random.Shared.Next(1, 10))
        .Select(_ => Substitute.For<IPostSeasonIdSetTask>())
        .ToArray();
        
        _mediator = Substitute.For<IMediator>();
        _libraryManager = MockHelper.LibraryManager;
        _itemRepository = MockHelper.ItemRepository;
        _mediaSourceManager = MockHelper.MediaSourceManager;
        var logger = Substitute.For<ILogger<SetSeasonIdTask>>();
        
        _sut = new SetSeasonIdTask(_mediator, _postSeasonIdSetTasks, logger, _libraryManager);

        _faker = new Faker();
    }
}


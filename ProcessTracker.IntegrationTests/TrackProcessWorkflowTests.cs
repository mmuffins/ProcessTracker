using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Data;
using Xunit;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class TrackProcessWorkflowTests
{
    private static readonly DateTime DefaultStart = new(2024, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

    private readonly IntegrationTestFixture _fixture;

    private SwitchableProcessProvider ProcessProvider => _fixture.GetRequiredService<SwitchableProcessProvider>();
    private MutableDateTimeProvider Clock => _fixture.GetRequiredService<MutableDateTimeProvider>();

    public TrackProcessWorkflowTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TrackProcessHandler_RecordsSessions_ForMatchingFilters()
    {
        var runningProcessName = Process.GetCurrentProcess().ProcessName;
        const string nonMatchingProcessName = "process-that-does-not-exist";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", FieldTypeEnum.Name, FilterTypeEnum.Equal, runningProcessName),
            CreateTag("Ignored", FieldTypeEnum.Name, FilterTypeEnum.Equal, nonMatchingProcessName)
        });

        ProcessProvider.UseSystemProcesses();

        var trackingResponse = await RunTrackProcessAsync();

        Assert.True(trackingResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, trackingResponse.StatusCode);

        var trackedSessions = await LoadSessionsAsync("Tracked");
        var trackedSession = Assert.Single(trackedSessions);
        Assert.True(trackedSession.StartTime <= trackedSession.LastUpdateTime);
        Assert.Null(trackedSession.EndTime);

        var ignoredSessions = await LoadSessionsAsync("Ignored");
        Assert.Empty(ignoredSessions);

        var trackedTagId = await GetTagIdAsync("Tracked");
        var ignoredTagId = await GetTagIdAsync("Ignored");

        var tagStatus = await _fixture.ExecuteScopedServiceAsync<ITagSessionRepository, List<TagSessionViewModel>>(repository => repository.GetTagsWithSessions());

        var trackedStatus = Assert.Single(tagStatus, status => status.TagId == trackedTagId);
        Assert.NotNull(trackedStatus.SessionId);

        var ignoredStatus = Assert.Single(tagStatus, status => status.TagId == ignoredTagId);
        Assert.Null(ignoredStatus.SessionId);
    }

    [Fact]
    public async Task TrackProcessHandler_UpdatesExistingSession_WhenProcessContinuesRunning()
    {
        const string processName = "focus-writer";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", FieldTypeEnum.Name, FilterTypeEnum.Equal, processName)
        });

        Clock.Set(DefaultStart);
        ProcessProvider.SetProcesses(new[]
        {
            new ProcessViewModel { Name = processName }
        });

        var firstResponse = await RunTrackProcessAsync();
        Assert.True(firstResponse.Success);

        var initialSession = await AssertSingleSessionAsync("Tracked");
        Assert.Equal(DefaultStart, initialSession.StartTime);
        Assert.Equal(DefaultStart, initialSession.LastUpdateTime);
        Assert.Null(initialSession.EndTime);

        var updatedTime = DefaultStart.AddSeconds(2);
        Clock.Set(updatedTime);
        ProcessProvider.SetProcesses(new[]
        {
            new ProcessViewModel { Name = processName }
        });

        var secondResponse = await RunTrackProcessAsync();
        Assert.True(secondResponse.Success);

        var updatedSession = await AssertSingleSessionAsync("Tracked");
        Assert.Equal(initialSession.SessionId, updatedSession.SessionId);
        Assert.Equal(DefaultStart, updatedSession.StartTime);
        Assert.Equal(updatedTime, updatedSession.LastUpdateTime);
        Assert.Null(updatedSession.EndTime);
    }

    [Fact]
    public async Task TrackProcessHandler_ClosesSession_WhenProcessStops()
    {
        const string processName = "short-lived";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", FieldTypeEnum.Name, FilterTypeEnum.Equal, processName)
        });

        Clock.Set(DefaultStart);
        ProcessProvider.SetProcesses(new[]
        {
            new ProcessViewModel { Name = processName }
        });

        await RunTrackProcessAsync();
        var startedSession = await AssertSingleSessionAsync("Tracked");

        var stopTime = DefaultStart.AddSeconds(2);
        Clock.Set(stopTime);
        ProcessProvider.ClearProcesses();

        await RunTrackProcessAsync();
        var closedSession = await AssertSingleSessionAsync("Tracked");

        Assert.Equal(startedSession.SessionId, closedSession.SessionId);
        Assert.Equal(stopTime, closedSession.LastUpdateTime);
        Assert.Equal(stopTime, closedSession.EndTime);
        Assert.Equal(TimeSpan.FromSeconds(2), closedSession.EndTime!.Value - closedSession.StartTime);
    }

    [Fact]
    public async Task TrackProcessHandler_StartsSessions_ForEachMatchingTag()
    {
        const string firstProcess = "alpha";
        const string secondProcess = "beta";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Alpha", FieldTypeEnum.Name, FilterTypeEnum.Equal, firstProcess),
            CreateTag("Beta", FieldTypeEnum.Name, FilterTypeEnum.Equal, secondProcess)
        });

        Clock.Set(DefaultStart);
        ProcessProvider.SetProcesses(new[]
        {
            new ProcessViewModel { Name = firstProcess },
            new ProcessViewModel { Name = secondProcess }
        });

        var response = await RunTrackProcessAsync();
        Assert.True(response.Success);

        var alphaSession = await AssertSingleSessionAsync("Alpha");
        var betaSession = await AssertSingleSessionAsync("Beta");

        Assert.Equal(DefaultStart, alphaSession.StartTime);
        Assert.Equal(DefaultStart, betaSession.StartTime);
    }

    public static IEnumerable<object[]> FilterMatchScenarios()
    {
        yield return new object[]
        {
            FieldTypeEnum.Name,
            FilterTypeEnum.Contain,
            "FocusWriter",
            "FOCUS"
        };

        yield return new object[]
        {
            FieldTypeEnum.Path,
            FilterTypeEnum.StartWith,
            "/usr/local/bin/dotnet",
            "/USR/LOCAL"
        };

        yield return new object[]
        {
            FieldTypeEnum.Description,
            FilterTypeEnum.EndWith,
            "Text Editor",
            "EDITOR"
        };

        yield return new object[]
        {
            FieldTypeEnum.MainWindowTitle,
            FilterTypeEnum.Equal,
            "Notes",
            "notes"
        };
    }

    [Theory]
    [MemberData(nameof(FilterMatchScenarios))]
    public async Task TrackProcessHandler_StartsSessions_ForMatchingFilterTypes(
        FieldTypeEnum fieldType,
        FilterTypeEnum filterType,
        string processValue,
        string filterValue)
    {
        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", fieldType, filterType, filterValue)
        });

        Clock.Set(DefaultStart);
        ProcessProvider.SetProcesses(new[]
        {
            BuildProcess(fieldType, processValue)
        });

        var response = await RunTrackProcessAsync();
        Assert.True(response.Success);

        var session = await AssertSingleSessionAsync("Tracked");
        Assert.Equal(DefaultStart, session.StartTime);
        Assert.Null(session.EndTime);
    }

    [Fact]
    public async Task TrackProcessHandler_SkipsTracking_WhenPaused()
    {
        const string processName = "paused-process";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", FieldTypeEnum.Name, FilterTypeEnum.Equal, processName)
        }, trackingPaused: true);

        Clock.Set(DefaultStart);
        ProcessProvider.SetProcesses(new[]
        {
            new ProcessViewModel { Name = processName }
        });

        var response = await RunTrackProcessAsync();
        Assert.True(response.Success);

        var sessions = await LoadSessionsAsync("Tracked");
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task TrackProcessHandler_DoesNotCreateSessions_WhenProcessNeverRuns()
    {
        const string processName = "ghost";

        await SeedTrackingConfigurationAsync(new[]
        {
            CreateTag("Tracked", FieldTypeEnum.Name, FilterTypeEnum.Equal, processName)
        });

        Clock.Set(DefaultStart);
        ProcessProvider.ClearProcesses();

        await RunTrackProcessAsync();
        Clock.Advance(TimeSpan.FromMinutes(1));
        await RunTrackProcessAsync();

        var sessions = await LoadSessionsAsync("Tracked");
        Assert.Empty(sessions);
    }

    private async Task SeedTrackingConfigurationAsync(IEnumerable<Tag> tags, bool trackingPaused = false)
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Settings.Add(new Setting
            {
                SettingName = SettingEnum.TrackingPaused.ToString(),
                Value = trackingPaused ? bool.TrueString : bool.FalseString
            });

            foreach (var tag in tags)
            {
                context.Tags.Add(tag);
            }

            return Task.CompletedTask;
        });
    }

    private async Task<GenericResponse> RunTrackProcessAsync()
    {
        return await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new TrackProcessRequest());
        });
    }

    private async Task<List<TagSession>> LoadSessionsAsync(string tagName)
    {
        return await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            var tagId = await context.Tags.Where(tag => tag.Name == tagName).Select(tag => tag.Id).SingleAsync();

            return await context.TagSessions.AsNoTracking()
                .Where(session => session.TagId == tagId)
                .OrderBy(session => session.SessionId)
                .ToListAsync();
        });
    }

    private async Task<TagSession> AssertSingleSessionAsync(string tagName)
    {
        var sessions = await LoadSessionsAsync(tagName);
        return Assert.Single(sessions);
    }

    private async Task<int> GetTagIdAsync(string tagName)
    {
        return await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Tags.Where(tag => tag.Name == tagName).Select(tag => tag.Id).SingleAsync();
        });
    }

    private static Tag CreateTag(string name, FieldTypeEnum fieldType, FilterTypeEnum filterType, string value)
    {
        var tag = new Tag
        {
            Name = name,
            Inactive = false
        };

        tag.Filters.Add(new Filter
        {
            FieldType = fieldType.ToString(),
            FilterType = filterType.ToString(),
            FieldValue = value,
            Inactive = false
        });

        return tag;
    }

    private static ProcessViewModel BuildProcess(FieldTypeEnum fieldType, string value)
    {
        var process = new ProcessViewModel();

        switch (fieldType)
        {
            case FieldTypeEnum.Name:
                process.Name = value;
                break;
            case FieldTypeEnum.Path:
                process.Path = value;
                break;
            case FieldTypeEnum.Description:
                process.Description = value;
                break;
            case FieldTypeEnum.MainWindowTitle:
                process.MainWindowTitle = value;
                break;
            default:
                process.Name = value;
                break;
        }

        return process;
    }
}

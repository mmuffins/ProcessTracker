using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Infrastructure.Data;
using Xunit;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class TrackProcessWorkflowTests
{
    private readonly IntegrationTestFixture _fixture;

    public TrackProcessWorkflowTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TrackProcessHandler_RecordsSessions_ForMatchingFilters()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Settings.Add(new Setting
            {
                SettingName = SettingEnum.TrackingPaused.ToString(),
                Value = bool.FalseString
            });

            return Task.CompletedTask;
        });

        var runningProcessName = Process.GetCurrentProcess().ProcessName;
        const string nonMatchingProcessName = "process-that-does-not-exist";

        await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();

            var trackedTagResponse = await mediator.Send(new CreateTagRequest { Name = "Tracked" });
            Assert.True(trackedTagResponse.Success);

            var ignoredTagResponse = await mediator.Send(new CreateTagRequest { Name = "Ignored" });
            Assert.True(ignoredTagResponse.Success);

            var trackedFilterResponse = await mediator.Send(new CreateFilterRequest
            {
                TagName = "Tracked",
                FieldType = (int)FieldTypeEnum.Name,
                FilterType = (int)FilterTypeEnum.Equal,
                Value = runningProcessName
            });
            Assert.True(trackedFilterResponse.Success);

            var ignoredFilterResponse = await mediator.Send(new CreateFilterRequest
            {
                TagName = "Ignored",
                FieldType = (int)FieldTypeEnum.Name,
                FilterType = (int)FilterTypeEnum.Equal,
                Value = nonMatchingProcessName
            });
            Assert.True(ignoredFilterResponse.Success);
        });

        var trackingResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new TrackProcessRequest());
        });

        Assert.True(trackingResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, trackingResponse.StatusCode);

        var databaseState = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();

            var trackedTag = await context.Tags.Include(tag => tag.TagSessions)
                .SingleAsync(tag => tag.Name == "Tracked");
            var ignoredTag = await context.Tags.Include(tag => tag.TagSessions)
                .SingleAsync(tag => tag.Name == "Ignored");

            return new
            {
                TrackedTagId = trackedTag.Id,
                TrackedSessions = trackedTag.TagSessions.ToList(),
                IgnoredTagId = ignoredTag.Id,
                IgnoredSessions = ignoredTag.TagSessions.ToList()
            };
        });

        var trackedSession = Assert.Single(databaseState.TrackedSessions);
        Assert.Equal(databaseState.TrackedTagId, trackedSession.TagId);
        Assert.True(trackedSession.StartTime <= trackedSession.LastUpdateTime);
        Assert.Null(trackedSession.EndTime);

        Assert.Empty(databaseState.IgnoredSessions);

        var tagStatus = await _fixture.ExecuteScopedServiceAsync<ITagSessionRepository, List<TagSessionViewModel>>(repository => repository.GetTagsWithSessions());

        var trackedStatus = Assert.Single(tagStatus, status => status.TagId == databaseState.TrackedTagId);
        Assert.NotNull(trackedStatus.SessionId);

        var ignoredStatus = Assert.Single(tagStatus, status => status.TagId == databaseState.IgnoredTagId);
        Assert.Null(ignoredStatus.SessionId);
    }
}

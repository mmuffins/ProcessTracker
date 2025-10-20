using System.Globalization;
using System.Linq;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Infrastructure.Data;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class SessionEndpointTests
{
    private readonly IntegrationTestFixture _fixture;

    public SessionEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionAddHandler_PersistsSessionsForValidRequests()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            var tag = new Tag { Name = "Focus", Inactive = false };
            tag.Filters.Add(new Filter
            {
                FilterType = FilterTypeEnum.Equal.ToString(),
                FieldType = FieldTypeEnum.Name.ToString(),
                FieldValue = "Focus"
            });

            context.Tags.Add(tag);
            return Task.CompletedTask;
        });

        var format = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var firstStart = new DateTime(2024, 9, 4, 10, 0, 0);
        var firstEnd = firstStart.AddHours(1);
        var secondStart = firstEnd;
        var secondEnd = secondStart.AddHours(1);

        var firstResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionAddRequest
            {
                TagName = "Focus",
                StartDate = firstStart.ToString(format, CultureInfo.InvariantCulture),
                EndDate = firstEnd.ToString(format, CultureInfo.InvariantCulture)
            });
        });

        Assert.True(firstResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal("Session added successfully.", firstResponse.Message);

        var secondResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionAddRequest
            {
                TagName = "Focus",
                StartDate = secondStart.ToString(format, CultureInfo.InvariantCulture),
                EndDate = secondEnd.ToString(format, CultureInfo.InvariantCulture)
            });
        });

        Assert.True(secondResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, secondResponse.StatusCode);

        var sessions = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessions.AsNoTracking().OrderBy(s => s.StartTime).ToListAsync();
        });

        Assert.Equal(2, sessions.Count);

        var firstSession = sessions[0];
        Assert.Equal(firstStart, firstSession.StartTime);
        Assert.Equal(firstEnd, firstSession.EndTime);
        Assert.Equal(firstEnd, firstSession.LastUpdateTime);

        var secondSession = sessions[1];
        Assert.Equal(secondStart, secondSession.StartTime);
        Assert.Equal(secondEnd, secondSession.EndTime);
        Assert.Equal(secondEnd, secondSession.LastUpdateTime);
    }

    [Fact]
    public async Task SessionAddHandler_TrimsWhitespaceFromTagName()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Tags.Add(new Tag { Name = "Focus", Inactive = false });
            return Task.CompletedTask;
        });

        var format = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var start = new DateTime(2024, 9, 7, 14, 0, 0);
        var end = start.AddHours(2);

        var response = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionAddRequest
            {
                TagName = "  Focus  ",
                StartDate = start.ToString(format, CultureInfo.InvariantCulture),
                EndDate = end.ToString(format, CultureInfo.InvariantCulture)
            });
        });

        Assert.True(response.Success);
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        var sessions = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessions.AsNoTracking().ToListAsync();
        });

        var session = Assert.Single(sessions);
        Assert.Equal(start, session.StartTime);
        Assert.Equal(end, session.EndTime);
    }

    [Fact]
    public async Task SessionAddHandler_ReturnsConflictForOverlappingSessions()
    {
        var existingStart = new DateTime(2024, 9, 5, 9, 0, 0);
        var existingEnd = existingStart.AddHours(2);

        await _fixture.SeedDatabaseAsync(context =>
        {
            var tag = new Tag { Name = "Focus", Inactive = false };
            tag.Filters.Add(new Filter
            {
                FilterType = FilterTypeEnum.Equal.ToString(),
                FieldType = FieldTypeEnum.Name.ToString(),
                FieldValue = "Focus"
            });

            tag.TagSessions.Add(new TagSession
            {
                StartTime = existingStart,
                EndTime = existingEnd,
                LastUpdateTime = existingEnd,
                CreationDate = existingStart
            });

            context.Tags.Add(tag);
            return Task.CompletedTask;
        });

        var format = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var overlapStart = existingStart.AddMinutes(30);
        var overlapEnd = overlapStart.AddHours(1);

        var response = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionAddRequest
            {
                TagName = "Focus",
                StartDate = overlapStart.ToString(format, CultureInfo.InvariantCulture),
                EndDate = overlapEnd.ToString(format, CultureInfo.InvariantCulture)
            });
        });

        Assert.False(response.Success);
        Assert.Equal((int)HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Session overlaps with an existing entry.", response.Message);

        var sessionCount = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessions.CountAsync();
        });

        Assert.Equal(1, sessionCount);
    }

    [Fact]
    public async Task SessionAddHandler_ReturnsBadRequestWhenEndPrecedesStart()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            var tag = new Tag { Name = "Focus", Inactive = false };
            tag.Filters.Add(new Filter
            {
                FilterType = FilterTypeEnum.Equal.ToString(),
                FieldType = FieldTypeEnum.Name.ToString(),
                FieldValue = "Focus"
            });

            context.Tags.Add(tag);
            return Task.CompletedTask;
        });

        var format = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var start = new DateTime(2024, 9, 6, 15, 0, 0);
        var end = start.AddHours(-1);

        var response = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionAddRequest
            {
                TagName = "Focus",
                StartDate = start.ToString(format, CultureInfo.InvariantCulture),
                EndDate = end.ToString(format, CultureInfo.InvariantCulture)
            });
        });

        Assert.False(response.Success);
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Start date must be earlier than end date.", response.Message);

        var sessionCount = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessions.CountAsync();
        });

        Assert.Equal(0, sessionCount);
    }

    [Fact]
    public async Task SessionRemoveHandler_RemovesSessionsOlderThanRequestedDays()
    {
        var oldStart = DateTime.Today.AddDays(-5).AddHours(8);
        var oldEnd = oldStart.AddHours(1);
        var recentStart = DateTime.Today.AddHours(9);
        var recentEnd = recentStart.AddHours(1);

        await _fixture.SeedDatabaseAsync(context =>
        {
            var tag = new Tag { Name = "Focus", Inactive = false };
            tag.Filters.Add(new Filter
            {
                FilterType = FilterTypeEnum.Equal.ToString(),
                FieldType = FieldTypeEnum.Name.ToString(),
                FieldValue = "Focus"
            });

            tag.TagSessions.Add(new TagSession
            {
                StartTime = oldStart,
                EndTime = oldEnd,
                LastUpdateTime = oldEnd,
                CreationDate = oldStart
            });

            tag.TagSessions.Add(new TagSession
            {
                StartTime = recentStart,
                EndTime = recentEnd,
                LastUpdateTime = recentEnd,
                CreationDate = recentStart
            });

            context.Tags.Add(tag);
            return Task.CompletedTask;
        });

        var response = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SessionRemoveRequest { days = 2 });
        });

        Assert.True(response.Success);
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        var remainingSessions = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessions.AsNoTracking().ToListAsync();
        });

        var remaining = Assert.Single(remainingSessions);
        Assert.Equal(recentStart, remaining.StartTime);
        Assert.Equal(recentEnd, remaining.EndTime);
    }
}

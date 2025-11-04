using System.Globalization;
using System.Linq;
using System.Net;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class ReportEndpointTests
{
    private readonly IntegrationTestFixture _fixture;

    public ReportEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReportHandler_AggregatesTotalsAndFormatsOutput()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Tags.Add(new Tag { Name = "Focus", Inactive = false });
            return Task.CompletedTask;
        });

        var dateTimeFormat = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var dateFormat = _fixture.Configuration["AppSettings:DateFormat"]!;

        var sessions = new (DateTime Start, DateTime End)[]
        {
            (new DateTime(2024, 9, 3, 9, 0, 0), new DateTime(2024, 9, 3, 10, 0, 0)),
            (new DateTime(2024, 9, 3, 11, 15, 0), new DateTime(2024, 9, 3, 11, 45, 0)),
            (new DateTime(2024, 9, 4, 9, 0, 0), new DateTime(2024, 9, 4, 10, 30, 0))
        };

        foreach (var (start, end) in sessions)
        {
            var addResponse = await _fixture.ExecuteScopeAsync(async provider =>
            {
                var mediator = provider.GetRequiredService<IMediator>();
                return await mediator.Send(new SessionAddRequest
                {
                    TagName = "Focus",
                    StartDate = start.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                    EndDate = end.ToString(dateTimeFormat, CultureInfo.InvariantCulture)
                });
            });

            Assert.True(addResponse.Success);
            Assert.Equal((int)HttpStatusCode.OK, addResponse.StatusCode);
        }

        var summarizeResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SummarizeRequest
            {
                StartDate = sessions.First().Start.Date.ToString(dateFormat, CultureInfo.InvariantCulture),
                EndDate = sessions.Last().Start.Date.ToString(dateFormat, CultureInfo.InvariantCulture)
            });
        });

        Assert.True(summarizeResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, summarizeResponse.StatusCode);

        var summaries = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessionSummary
                .AsNoTracking()
                .Include(summary => summary.Tag)
                .OrderBy(summary => summary.Day)
                .ToListAsync();
        });

        Assert.Collection(summaries,
            summary =>
            {
                Assert.Equal("Focus", summary.Tag.Name);
                Assert.Equal(sessions.First().Start.Date, summary.Day.Date);
                Assert.Equal(TimeSpan.FromHours(1.5).TotalSeconds, summary.Seconds);
            },
            summary =>
            {
                Assert.Equal("Focus", summary.Tag.Name);
                Assert.Equal(sessions.Last().Start.Date, summary.Day.Date);
                Assert.Equal(TimeSpan.FromHours(1.5).TotalSeconds, summary.Seconds);
            });

        var reportResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new ReportRequest());
        });

        Assert.True(reportResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, reportResponse.StatusCode);

        var report = Assert.IsType<List<TagsReportViewModel>>(reportResponse.Report);
        var entry = Assert.Single(report);
        Assert.Equal("Focus", entry.Name);
        Assert.Equal("3:00", entry.TotalActiveTime);
        Assert.Equal(sessions.First().Start.Date.ToString(dateFormat, CultureInfo.InvariantCulture), entry.FirstOccurence);
        Assert.Equal(sessions.Last().Start.Date.ToString(dateFormat, CultureInfo.InvariantCulture), entry.LastOccurence);
    }

    [Fact]
    public async Task ReportHandler_FiltersReportByTagName()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Tags.AddRange(
                new Tag { Name = "Focus", Inactive = false },
                new Tag { Name = "Break", Inactive = false }
            );
            return Task.CompletedTask;
        });

        var dateTimeFormat = _fixture.Configuration["AppSettings:DateTimeFormat"]!;
        var dateFormat = _fixture.Configuration["AppSettings:DateFormat"]!;

        var sessionDefinitions = new (string Tag, DateTime Start, DateTime End)[]
        {
            ("Focus", new DateTime(2024, 9, 1, 9, 0, 0), new DateTime(2024, 9, 1, 10, 0, 0)),
            ("Break", new DateTime(2024, 9, 2, 14, 0, 0), new DateTime(2024, 9, 2, 14, 45, 0))
        };

        foreach (var (tag, start, end) in sessionDefinitions)
        {
            var addResponse = await _fixture.ExecuteScopeAsync(async provider =>
            {
                var mediator = provider.GetRequiredService<IMediator>();
                return await mediator.Send(new SessionAddRequest
                {
                    TagName = tag,
                    StartDate = start.ToString(dateTimeFormat, CultureInfo.InvariantCulture),
                    EndDate = end.ToString(dateTimeFormat, CultureInfo.InvariantCulture)
                });
            });

            Assert.True(addResponse.Success);
            Assert.Equal((int)HttpStatusCode.OK, addResponse.StatusCode);
        }

        var summarizeResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new SummarizeRequest
            {
                StartDate = sessionDefinitions.Min(s => s.Start.Date).ToString(dateFormat, CultureInfo.InvariantCulture),
                EndDate = sessionDefinitions.Max(s => s.Start.Date).ToString(dateFormat, CultureInfo.InvariantCulture)
            });
        });

        Assert.True(summarizeResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, summarizeResponse.StatusCode);

        var summaryRows = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.TagSessionSummary
                .AsNoTracking()
                .Include(summary => summary.Tag)
                .OrderBy(summary => summary.Tag.Name)
                .ToListAsync();
        });

        Assert.Collection(summaryRows,
            summary =>
            {
                Assert.Equal("Break", summary.Tag.Name);
                Assert.Equal(TimeSpan.FromMinutes(45).TotalSeconds, summary.Seconds);
            },
            summary =>
            {
                Assert.Equal("Focus", summary.Tag.Name);
                Assert.Equal(TimeSpan.FromHours(1).TotalSeconds, summary.Seconds);
            });

        var reportResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new ReportRequest { TagName = "Break" });
        });

        Assert.True(reportResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, reportResponse.StatusCode);

        var report = Assert.IsType<List<TagsReportViewModel>>(reportResponse.Report);
        var entry = Assert.Single(report);
        Assert.Equal("Break", entry.Name);
        Assert.Equal("0:45", entry.TotalActiveTime);
        var expectedDate = sessionDefinitions.Single(s => s.Tag == "Break").Start.Date.ToString(dateFormat, CultureInfo.InvariantCulture);
        Assert.Equal(expectedDate, entry.FirstOccurence);
        Assert.Equal(expectedDate, entry.LastOccurence);
    }
}

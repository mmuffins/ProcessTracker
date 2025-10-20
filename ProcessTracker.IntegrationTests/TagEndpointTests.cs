using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Infrastructure.Data;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class TagEndpointTests
{
    private readonly IntegrationTestFixture _fixture;

    public TagEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TagLifecycle_CompletesThroughMediator()
    {
        await _fixture.SeedDatabaseAsync();

        var createResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new CreateTagRequest { Name = "Focus" });
        });

        Assert.True(createResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, createResponse.StatusCode);

        var tag = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Tags.AsNoTracking().SingleAsync();
        });

        Assert.Equal("Focus", tag.Name);
        Assert.False(tag.Inactive);

        var listResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetTagsRequest());
        });

        var tagList = Assert.IsType<List<TagViewModel>>(listResponse.Tags);
        var tagViewModel = Assert.Single(tagList);
        Assert.Equal(tag.Id, tagViewModel.Id);
        Assert.Equal(tag.Name, tagViewModel.Name);
        Assert.Equal(tag.Inactive, tagViewModel.Inactive);

        var singleResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetTagsRequest { Name = "Focus" });
        });

        var singleTag = Assert.IsType<TagViewModel>(singleResponse.Tag);
        Assert.Equal(tag.Id, singleTag.Id);

        var toggleResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new TagToggleRequest { Name = "Focus" });
        });

        Assert.True(toggleResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, toggleResponse.StatusCode);

        var toggledTag = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Tags.AsNoTracking().SingleAsync();
        });

        Assert.True(toggledTag.Inactive);

        var deleteResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new DeleteTagRequest { Name = "Focus" });
        });

        Assert.True(deleteResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, deleteResponse.StatusCode);

        var remainingTags = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Tags.CountAsync();
        });

        Assert.Equal(0, remainingTags);
    }
}

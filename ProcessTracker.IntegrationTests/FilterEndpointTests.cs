using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessTracker.IntegrationTests.Fixtures;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Infrastructure.Data;

namespace ProcessTracker.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class FilterEndpointTests
{
    private readonly IntegrationTestFixture _fixture;

    public FilterEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FilterLifecycle_AssociatesWithTagAndSupportsQueries()
    {
        await _fixture.SeedDatabaseAsync(context =>
        {
            context.Tags.Add(new Tag { Name = "Work", Inactive = false });
            return Task.CompletedTask;
        });

        var createResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new CreateFilterRequest
            {
                TagName = "Work",
                FieldType = (int)FieldTypeEnum.Name,
                FilterType = (int)FilterTypeEnum.Contain,
                Value = "Browser"
            });
        });

        Assert.True(createResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, createResponse.StatusCode);

        var filterEntity = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Filters.Include(f => f.Tag).AsNoTracking().SingleAsync();
        });

        Assert.Equal("Work", filterEntity.Tag.Name);
        Assert.Equal("Contain", filterEntity.FilterType);
        Assert.Equal("Name", filterEntity.FieldType);
        Assert.False(filterEntity.Inactive);

        var listResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetFilterRequest { TagName = "Work" });
        });

        var filters = Assert.IsType<List<FilterViewModel>>(listResponse.Filters);
        var filterViewModel = Assert.Single(filters);
        Assert.Equal(filterEntity.Id, filterViewModel.Id);
        Assert.False(filterViewModel.Disabled);
        Assert.Equal("Browser", filterViewModel.Value);

        var singleResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetFilterRequest { FilterID = filterEntity.Id });
        });

        var singleFilter = Assert.IsType<FilterViewModel>(singleResponse.Filter);
        Assert.Equal(filterEntity.Id, singleFilter.Id);

        var toggleResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new FilterToggleRequest { FilterID = filterEntity.Id });
        });

        Assert.True(toggleResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, toggleResponse.StatusCode);

        var toggledEntity = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Filters.Include(f => f.Tag).AsNoTracking().SingleAsync();
        });

        Assert.True(toggledEntity.Inactive);

        var activeOnlyResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new GetFilterRequest { TagName = "Work", inactive = false });
        });

        var activeFilters = Assert.IsType<List<FilterViewModel>>(activeOnlyResponse.Filters);
        Assert.Empty(activeFilters);

        var deleteResponse = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return await mediator.Send(new DeleteFilterRequest { FilterID = filterEntity.Id });
        });

        Assert.True(deleteResponse.Success);
        Assert.Equal((int)HttpStatusCode.OK, deleteResponse.StatusCode);

        var remainingFilters = await _fixture.ExecuteScopeAsync(async provider =>
        {
            var context = provider.GetRequiredService<PTServiceContext>();
            return await context.Filters.CountAsync();
        });

        Assert.Equal(0, remainingFilters);
    }
}

using MediatR;
using Microsoft.Extensions.Configuration;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Dto.Responses.ViewModels;
using ProcessTrackerService.Core.Entities;
using ProcessTrackerService.Core.Helpers;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Core.Interfaces.Repository;
using ProcessTrackerService.Core.Specifications;
using System.Diagnostics;
using System.Net;

namespace ProcessTrackerService.Core.Handlers
{
    public sealed class TrackProcessHandler : IRequestHandler<TrackProcessRequest, GenericResponse>
    {
        private readonly ITagSessionRepository _tagSessionRepository;
        private readonly IAsyncRepository<Filter> _filterRepository;
        private readonly IAsyncRepository<Setting> _settingRepository;
        private readonly IConfiguration _configuration;

        public TrackProcessHandler(ITagSessionRepository tagSessionRepository, IAsyncRepository<Filter> filterRepository, IAsyncRepository<Setting> settingRepository, IConfiguration configuration)
        {
            _tagSessionRepository = tagSessionRepository;
            _filterRepository = filterRepository;
            _settingRepository = settingRepository;
            _configuration = configuration;
        }

        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }
        public async Task<GenericResponse> Handle(TrackProcessRequest request, CancellationToken cancellationToken)
        {
            // check setting and only perform tracking when tracking is not paused.
            var trackingSetting = await _settingRepository.FirstOrDefaultAsync(new GenericSpecification<Setting>(x => x.SettingName == SettingEnum.TrackingPaused.ToString()));
            if (trackingSetting != null && !string.IsNullOrEmpty(trackingSetting.Value) && trackingSetting.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                // Get active tags from the DB
                var tags = await _tagSessionRepository.GetTagsWithSessions();
                if (!tags.Any())
                    return new GenericResponse((int)HttpStatusCode.NotFound, false, "No active tags found.");

                var tagsIDs = tags.Select(x => x.TagId);

                // Get active filters for tags from the DB
                var filters = await _filterRepository.ListAsync(new GenericSpecification<Filter>(x => !x.Inactive && tagsIDs.Contains(x.TagId)));

                // Get tag latest incomplete sessions
                IReadOnlyList<TagSession> sessions = new List<TagSession>();
                if (tags.Any(x => x.SessionId > 0))
                {
                    var sessionIDs = tags.Where(x => x.SessionId > 0).Select(x => x.SessionId.Value);
                    sessions = await _tagSessionRepository.ListAsync(new GenericSpecification<TagSession>(x => sessionIDs.Contains(x.SessionId)));
                }

                // Get Processes with their respective properties
                var processes = Process.GetProcesses();
                List<ProcessViewModel> pvmList = new List<ProcessViewModel>();
                foreach (var process in processes)
                {
                    try
                    {
                        var mainModule = process.MainModule;
                        ProcessViewModel pvm = new ProcessViewModel
                        {
                            Name = process.ProcessName,
                            MainWindowTitle = process.MainWindowTitle,
                            Description = mainModule?.FileVersionInfo.FileDescription ?? "",
                            Path = mainModule?.FileName ?? ""
                        };
                        pvmList.Add(pvm);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                List<TagSession> tagSessionsUpdate = new List<TagSession>();
                List<TagSession> tagSessionsAdd = new List<TagSession>();

                // Track Processes
                foreach (var tag in tags)
                {
                    bool found = false;
                    var tagFilters = filters.Where(x => x.TagId == tag.TagId);
                    foreach (var tagFilter in tagFilters)
                    {
                        // perform filter on fields and check if the process matches or not
                        found = FindProcessByFilter(tagFilter.FieldType, tagFilter.FilterType, tagFilter.FieldValue, pvmList);

                        // if process found then no need to check anymore filters
                        if (found)
                            break;
                    }

                    // if process is found and it does not have a incomplete session then start a new session otherwise leave it
                    if (found && tag.SessionId == null)
                        tagSessionsAdd.Add(new TagSession
                        {
                            TagId = tag.TagId,
                            StartTime = DateTime.Now,
                            LastUpdateTime = DateTime.Now
                        });
                    // if there is an incomplete session
                    else if (tag.SessionId > 0)
                    {
                        var session = sessions.FirstOrDefault(x => x.SessionId == tag.SessionId);
                        if (session != null)
                        {
                            // if the elapsed time is more than cushion time then assume that the service creashed or stopped. Update the last time as endtime for this session
                            if ((DateTime.Now - session.LastUpdateTime).TotalSeconds > AppSettings.CushionDelay)
                            {
                                session.EndTime = session.LastUpdateTime;
                                tagSessionsUpdate.Add(session);

                                // if process is still runing then start a new session after closing the previous one to last updated time
                                if (found)
                                    tagSessionsAdd.Add(new TagSession
                                    {
                                        TagId = tag.TagId,
                                        StartTime = DateTime.Now,
                                        LastUpdateTime = DateTime.Now
                                    });
                            }
                            else
                            {
                                // update the last time on every iteration if the process is running
                                if (found)
                                {
                                    session.LastUpdateTime = DateTime.Now;
                                    tagSessionsUpdate.Add(session);
                                }
                                // if process is not running then close the session.
                                else
                                {
                                    session.LastUpdateTime = DateTime.Now;
                                    session.EndTime = DateTime.Now;
                                    tagSessionsUpdate.Add(session);
                                }
                            }
                        }
                    }
                }

                // do bulk database operation of adding new sessions and updating existing incomplete sessions.
                if (tagSessionsAdd.Any())
                    await _tagSessionRepository.AddBulkAsync(tagSessionsAdd);
                if (tagSessionsUpdate.Any())
                    await _tagSessionRepository.UpdateBulkAsync(tagSessionsUpdate);
            }

            return new GenericResponse((int)HttpStatusCode.OK, true, "Success");
        }

        #region private
        private bool FindProcessByFilter(string fieldType, string filterType, string value, List<ProcessViewModel> processes)
        {
            // conditions to check filters by field type and return true if process is found 

            if (FieldTypeEnum.Name.ToString().Equals(fieldType))
            {
                if (FilterTypeEnum.Equal.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Name) && x.Name.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.Contain.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Name) && x.Name.Contains(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.EndWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Name) && x.Name.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.StartWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Name) && x.Name.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            else if (FieldTypeEnum.Path.ToString().Equals(fieldType))
            {
                if (FilterTypeEnum.Equal.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Path) && x.Path.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.Contain.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Path) && x.Path.Contains(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.EndWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Path) && x.Path.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.StartWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Path) && x.Path.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            else if (FieldTypeEnum.Description.ToString().Equals(fieldType))
            {
                if (FilterTypeEnum.Equal.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Description) && x.Description.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.Contain.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Description) && x.Description.Contains(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.EndWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Description) && x.Description.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.StartWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.Description) && x.Description.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            else if (FieldTypeEnum.MainWindowTitle.ToString().Equals(fieldType))
            {
                if (FilterTypeEnum.Equal.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.MainWindowTitle) && x.MainWindowTitle.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.Contain.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.MainWindowTitle) && x.MainWindowTitle.Contains(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.EndWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.MainWindowTitle) && x.MainWindowTitle.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
                else if (FilterTypeEnum.StartWith.ToString().Equals(filterType))
                {
                    if (processes.Any(x => !string.IsNullOrEmpty(x.MainWindowTitle) && x.MainWindowTitle.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            return false;
        }
        #endregion
    }

}

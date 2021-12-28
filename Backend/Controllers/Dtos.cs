using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TimeReport.Controllers;

public record class ProjectDto(string Id, string Name, string? Description);

public record class ActivityDto(string Id, string Name, string? Description, decimal? HourlyRate, ProjectDto Project);

public record class ExpenseDto(string Id, DateTime Date, decimal Amount, string? Description, string? Attachment, ProjectDto Project);

public record class TimeSheetDto(string Id, int Year, int Week, DateTime From, DateTime To, TimeSheetStatusDto Status, UserDto User, IEnumerable<TimeSheetActivityDto> Activities, IEnumerable<MonthInfoDto> infos);

public record class TimeSheetActivityDto(string Id, string Name, string? Description, ProjectDto Project, IEnumerable<TimeSheetEntryDto> Entries);

public record class MonthInfoDto(int Month, bool IsLocked);

public record class TimeSheetEntryDto(string Id, DateTime Date, double? Hours, string? Description, EntryStatusDto Status);

[JsonConverter(typeof(StringEnumConverter))]
public enum TimeSheetStatusDto
{
    Open,
    Closed,
    Approved,
    Disapproved
}

public record class EntryDto(string Id, ProjectDto Project, ActivityDto Activity, DateTime Date, double? Hours, string? Description, EntryStatusDto Status);

[JsonConverter(typeof(StringEnumConverter))]
public enum EntryStatusDto
{
    Unlocked,
    Locked
}

public record class CreateEntryDto(string? Id, string? ProjectId, string? ActivityId, DateTime Date, double? Hours, string? Description);

public record class UpdateEntryDto(double? Hours, string? Description);

public record class UpdateEntryDetailsDto(string? Description);

public record class UpdateTimeSheetDto(IEnumerable<UpdateEntryDto2> Entries);

public record class UpdateEntryDto2(string? Id, string? ProjectId, string? ActivityId, DateTime? Date, double? Hours, string? Description);


public record class Series(string Name, IEnumerable<decimal> Data);

public record class Data(string[] Labels, IEnumerable<Series> Series);


public record class UserDto(string Id, string FirstName, string LastName, string? DisplayName, string SSN, string Email, DateTime Created, DateTime? Deleted);


public record class ProjectMembershipDto(string Id, ProjectDto Project, UserDto User, DateTime? From, DateTime? Thru);

public record class CreateProjectMembershipDto(string UserId, DateTime? From, DateTime? Thru);

public record class UpdateProjectMembershipDto(DateTime? From, DateTime? Thru);

public record ItemsResult<T>(IEnumerable<T> Items, int TotalItems);

[JsonConverter(typeof(StringEnumConverter))]
public enum SortDirection
{
    Asc = 2,
    Desc = 1
}
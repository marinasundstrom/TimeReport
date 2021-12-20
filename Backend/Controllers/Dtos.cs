using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TimeReport.Controllers;

public record class ProjectDto(string Id, string Name, string? Description);

public record class ActivityDto(string Id, string Name, string? Description, ProjectDto Project);

public record class TimeSheetDto(string Id, int Year, int Week, TimeSheetStatusDto Status, IEnumerable<EntryDto> Entries);

[JsonConverter(typeof(StringEnumConverter))]
public enum TimeSheetStatusDto
{
    Open,
    Closed,
    Approved,
    Disapproved
}

public record class EntryDto(string Id, ProjectDto Project, ActivityDto Activity, DateTime Date, double? Hours, string? Description);

public record class CreateEntryDto(string? Id, string? ProjectId, string? ActivityId, DateTime Date, double? Hours, string? Description);

public record class UpdateEntryDto(double? Hours, string? Description);

public record class UpdateEntryDetailsDto(string? Description);

public record class UpdateTimeSheetDto(IEnumerable<UpdateEntryDto2> Entries);

public record class UpdateEntryDto2(string? Id, string? ProjectId, string? ActivityId, DateTime? Date, double? Hours, string? Description);

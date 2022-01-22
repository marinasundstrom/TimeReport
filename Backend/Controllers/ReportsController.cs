
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OfficeOpenXml;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Infrastructure;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ITimeReportContext context;

    public ReportsController(ITimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet]
    public async Task<FileStreamResult> GetReport([FromQuery] string[] projectIds, [FromQuery] string? userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        DateOnly startDate2 = DateOnly.FromDateTime(startDate);
        DateOnly endDate2 = DateOnly.FromDateTime(endDate);

        var query = context.Entries
            .Include(p => p.TimeSheet)
            .ThenInclude(p => p.User)
            .Include(p => p.Project)
            .Include(p => p.Activity)
            .Where(p => projectIds.Any(x => x == p.Project.Id))
            .Where(p => p.Date >= startDate2 && p.Date <= endDate2)
            .AsSplitQuery();

        if (userId is not null)
        {
            query = query.Where(x => x.TimeSheet.User.Id == userId);
        }

        var entries = await query.ToListAsync();

        int row = 1;

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Projects");

            var projectGroups = entries.GroupBy(x => x.Project);

            foreach (var project in projectGroups)
            {
                worksheet.Cells[row++, 1]
                      .LoadFromCollection(new[] { new { Project = project.Key.Name } });

                int headerRow = row - 1;

                var activityGroups = project.GroupBy(x => x.Activity);

                foreach (var activityGroup in activityGroups)
                {
                    var data = activityGroup
                        .OrderBy(e => e.Date)
                        .Select(e => new { e.Date, User = $" {e.TimeSheet.User.LastName}, {e.TimeSheet.User.FirstName}", Activity = e.Activity.Name, e.Hours, e.Description });

                    worksheet.Cells[row, 1]
                        .LoadFromCollection(data);

                    row += data.Count();

                    worksheet.Cells[headerRow, 4]
                        .Value = data.Sum(e => e.Hours.GetValueOrDefault());
                }

                row++;
            }

            Stream stream = new MemoryStream(package.GetAsByteArray());

            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/vnd.ms-excel", "TimeReport.xlsx");
        }
    }
}
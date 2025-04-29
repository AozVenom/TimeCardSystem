using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace TimeCardSystem.Core.Services
{
    public class ReportService : IReportService
    {
        private readonly ITimeEntryRepository _timeEntryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConverter _pdfConverter;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ITimeEntryRepository timeEntryRepository,
            IUserRepository userRepository,
            IConverter pdfConverter,
            ILogger<ReportService> logger)
        {
            _timeEntryRepository = timeEntryRepository;
            _userRepository = userRepository;
            _pdfConverter = pdfConverter;
            _logger = logger;
        }

        #region Report Generation Methods

        public async Task<ReportResult> GetIndividualReportAsync(int employeeId, DateTime startDate, DateTime endDate, bool includeDetails)
        {
            var result = new ReportResult();

            try
            {
                // Get employee
                var employee = await _userRepository.GetByEmployeeIdAsync(employeeId);
                if (employee == null)
                {
                    return result;
                }

                // Get time entries for the employee in the date range
                var timeEntries = await _timeEntryRepository.GetByEmployeeAndDateRangeAsync(employeeId, startDate, endDate);
                if (!timeEntries.Any())
                {
                    return result;
                }

                // Group by date to get summary data
                var entriesByDate = timeEntries
                    .GroupBy(e => e.Date.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                // Generate chart data
                result.ChartLabels = entriesByDate.Select(g => g.Key.ToString("MM/dd")).ToList();
                result.ChartRegularHours = entriesByDate.Select(g => g.Sum(e => e.RegularHours)).ToList();
                result.ChartOvertimeHours = entriesByDate.Select(g => g.Sum(e => e.OvertimeHours)).ToList();

                // Calculate summary metrics
                result.DaysWorked = entriesByDate.Count;
                result.RegularHours = timeEntries.Sum(e => e.RegularHours);
                result.OvertimeHours = timeEntries.Sum(e => e.OvertimeHours);
                result.TotalHours = result.RegularHours + result.OvertimeHours;

                // Generate report data
                if (includeDetails)
                {
                    // Include each individual time entry
                    foreach (var entry in timeEntries.OrderBy(e => e.Date).ThenBy(e => e.ClockIn))
                    {
                        result.Data.Add(new
                        {
                            Date = entry.Date,
                            ClockIn = entry.ClockIn,
                            ClockOut = entry.ClockOut,
                            RegularHours = entry.RegularHours,
                            OvertimeHours = entry.OvertimeHours,
                            Description = entry.Description ?? "N/A",
                            Notes = entry.Notes ?? ""
                        });
                    }
                }
                else
                {
                    // Daily summary
                    foreach (var dayGroup in entriesByDate)
                    {
                        var day = dayGroup.Key;

                        result.Data.Add(new
                        {
                            Date = day,
                            ClockIn = dayGroup.Min(e => e.ClockIn),
                            ClockOut = dayGroup.Max(e => e.ClockOut ?? DateTime.Now),
                            RegularHours = dayGroup.Sum(e => e.RegularHours),
                            OvertimeHours = dayGroup.Sum(e => e.OvertimeHours),
                            Description = "Daily Summary",
                            Notes = string.Join("; ", dayGroup.Where(e => !string.IsNullOrEmpty(e.Notes)).Select(e => e.Notes))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating individual report");
            }

            return result;
        }

        public async Task<ReportResult> GetOvertimeReportAsync(DateTime startDate, DateTime endDate)
        {
            var result = new ReportResult();

            try
            {
                // Get all employees
                var employees = await _userRepository.GetAllAsync();

                if (!employees.Any())
                {
                    return result;
                }

                var allTimeEntries = new List<TimeEntry>();

                // Get time entries for all employees in the date range
                foreach (var employee in employees)
                {
                    if (employee.EmployeeId == 0)
                        continue; // Skip users without EmployeeId

                    var entries = await _timeEntryRepository.GetByEmployeeAndDateRangeAsync(
                        employee.EmployeeId, startDate, endDate);
                    allTimeEntries.AddRange(entries);
                }

                if (!allTimeEntries.Any())
                {
                    return result;
                }

                // Calculate summary metrics
                result.DaysWorked = allTimeEntries.Select(e => e.Date.Date).Distinct().Count();
                result.RegularHours = allTimeEntries.Sum(e => e.RegularHours);
                result.OvertimeHours = allTimeEntries.Sum(e => e.OvertimeHours);
                result.TotalHours = result.RegularHours + result.OvertimeHours;

                // Group by week
                var weeklyEntries = allTimeEntries
                    .GroupBy(e => GetWeekNumber(e.Date))
                    .OrderBy(g => g.Key)
                    .ToList();

                // Generate chart data
                result.ChartLabels = weeklyEntries.Select(g => $"Week {g.Key}").ToList();
                result.ChartRegularHours = weeklyEntries.Select(g => g.Sum(e => e.RegularHours)).ToList();
                result.ChartOvertimeHours = weeklyEntries.Select(g => g.Sum(e => e.OvertimeHours)).ToList();

                // Group by employee and week
                var overtimeEntries = allTimeEntries
                    .Where(e => e.OvertimeHours > 0)
                    .GroupBy(e => new {
                        e.EmployeeId,
                        Week = GetWeekNumber(e.Date)
                    })
                    .OrderBy(g => g.Key.EmployeeId)
                    .ThenBy(g => g.Key.Week)
                    .ToList();

                // Generate report data
                foreach (var group in overtimeEntries)
                {
                    // Fix for line 271 - Add null check before dereferencing
                    var employee = employees.FirstOrDefault(e => e.EmployeeId == group.Key.EmployeeId);
                    if (employee == null)
                        continue;

                    var overtimeHours = group.Sum(e => e.OvertimeHours);
                    var hourlyRate = employee.HourlyRate ?? 0;
                    var overtimeCost = (decimal)overtimeHours * (decimal)hourlyRate * 1.5m;

                    result.Data.Add(new
                    {
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        WeekLabel = $"Week {group.Key.Week}",
                        OvertimeHours = overtimeHours,
                        OvertimeCost = overtimeCost
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overtime report");
            }

            return result;
        }

        /// <summary>
        /// Gets the ISO 8601 week number for a date
        /// </summary>
        private int GetWeekNumber(DateTime date)
        {
            var cal = CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        #endregion

        #region Export Methods

        public Task<byte[]> ExportToExcel(string reportType, List<dynamic> reportData, List<string> chartLabels,
            List<double> chartRegularHours, List<double> chartOvertimeHours, bool includeCharts, bool includeSummary)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());

                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    var sheet = new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Report"
                    };
                    sheets.AppendChild(sheet);

                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    // Add header row
                    var headerRow = new Row();

                    switch (reportType)
                    {
                        case "individual":
                            AddCell(headerRow, "Date");
                            AddCell(headerRow, "Clock In");
                            AddCell(headerRow, "Clock Out");
                            AddCell(headerRow, "Regular Hours");
                            AddCell(headerRow, "Overtime Hours");
                            AddCell(headerRow, "Description");
                            AddCell(headerRow, "Notes");
                            break;
                        case "overtime":
                            AddCell(headerRow, "Employee");
                            AddCell(headerRow, "Week");
                            AddCell(headerRow, "Overtime Hours");
                            AddCell(headerRow, "Overtime Cost");
                            break;
                    }

                    sheetData.AppendChild(headerRow);

                    // Add data rows
                    foreach (var item in reportData)
                    {
                        var dataRow = new Row();
                        IDictionary<string, object> itemDict = (IDictionary<string, object>)item;

                        switch (reportType)
                        {
                            case "individual":
                                AddCell(dataRow, itemDict["Date"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["ClockIn"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["ClockOut"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["RegularHours"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["OvertimeHours"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["Description"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["Notes"]?.ToString() ?? string.Empty);
                                break;
                            case "overtime":
                                AddCell(dataRow, itemDict["EmployeeName"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["WeekLabel"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["OvertimeHours"]?.ToString() ?? string.Empty);
                                AddCell(dataRow, itemDict["OvertimeCost"]?.ToString() ?? string.Empty);
                                break;
                        }

                        sheetData.AppendChild(dataRow);
                    }

                    workbookPart.Workbook.Save();
                }

                return Task.FromResult(stream.ToArray());
            }
        }

        /// <summary>
        /// Adds a cell to an Excel row
        /// </summary>
        private void AddCell(Row row, string? value)
        {
            // Handle null values
            var cellValue = value ?? string.Empty;

            Cell cell = new Cell();
            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue(cellValue);
            row.AppendChild(cell);
        }

        public Task<byte[]> ExportToCsv(string reportType, List<dynamic> reportData)
        {
            var sb = new StringBuilder();

            // Add header row
            switch (reportType)
            {
                case "individual":
                    sb.AppendLine("Date,Clock In,Clock Out,Regular Hours,Overtime Hours,Description,Notes");
                    break;
                case "overtime":
                    sb.AppendLine("Employee,Week,Overtime Hours,Overtime Cost");
                    break;
            }

            // Add data rows
            foreach (var item in reportData)
            {
                IDictionary<string, object> itemDict = (IDictionary<string, object>)item;

                switch (reportType)
                {
                    case "individual":
                        sb.AppendLine($"{itemDict["Date"] ?? ""}," +
                            $"{itemDict["ClockIn"] ?? ""}," +
                            $"{itemDict["ClockOut"] ?? ""}," +
                            $"{itemDict["RegularHours"] ?? ""}," +
                            $"{itemDict["OvertimeHours"] ?? ""}," +
                            $"\"{EscapeCsvValue(itemDict["Description"]?.ToString())}\"," +
                            $"\"{EscapeCsvValue(itemDict["Notes"]?.ToString())}\"");
                        break;
                    case "overtime":
                        sb.AppendLine($"\"{EscapeCsvValue(itemDict["EmployeeName"]?.ToString())}\"," +
                            $"{itemDict["WeekLabel"] ?? ""}," +
                            $"{itemDict["OvertimeHours"] ?? ""}," +
                            $"{itemDict["OvertimeCost"] ?? ""}");
                        break;
                }
            }

            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>
        /// Escapes special characters in CSV values
        /// </summary>
        private string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\"", "\"\"");
        }

        public Task<byte[]> ExportToPdf(string reportType, List<dynamic> reportData, List<string> chartLabels,
            List<double> chartRegularHours, List<double> chartOvertimeHours, bool includeCharts, bool includeSummary)
        {
            var sb = new StringBuilder();

            // Build HTML content
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<title>Time Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("h1 { color: #333; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
            sb.AppendLine(".summary { margin-bottom: 20px; padding: 15px; background-color: #f5f5f5; border-radius: 5px; }");
            sb.AppendLine(".summary-item { display: inline-block; margin-right: 20px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Title
            sb.AppendLine($"<h1>{char.ToUpper(reportType[0]) + reportType.Substring(1)} Report</h1>");

            // If including summary
            if (includeSummary)
            {
                // Calculate summary data
                double totalHours = 0;
                double regularHours = 0;
                double overtimeHours = 0;

                foreach (var item in reportData)
                {
                    IDictionary<string, object> itemDict = (IDictionary<string, object>)item;

                    switch (reportType)
                    {
                        case "individual":
                            regularHours += Convert.ToDouble(itemDict["RegularHours"] ?? 0);
                            overtimeHours += Convert.ToDouble(itemDict["OvertimeHours"] ?? 0);
                            break;
                        case "overtime":
                            overtimeHours += Convert.ToDouble(itemDict["OvertimeHours"] ?? 0);
                            break;
                    }
                }

                totalHours = regularHours + overtimeHours;

                // Add summary section
                sb.AppendLine("<div class=\"summary\">");
                sb.AppendLine("<h2>Summary</h2>");
                sb.AppendLine("<div class=\"summary-item\"><strong>Total Hours:</strong> " + totalHours.ToString("F1") + "</div>");

                if (reportType == "individual")
                {
                    sb.AppendLine("<div class=\"summary-item\"><strong>Regular Hours:</strong> " + regularHours.ToString("F1") + "</div>");
                    sb.AppendLine("<div class=\"summary-item\"><strong>Overtime Hours:</strong> " + overtimeHours.ToString("F1") + "</div>");
                }

                sb.AppendLine("</div>");
            }

            // Add table with data
            sb.AppendLine("<table>");

            // Table header
            sb.AppendLine("<tr>");
            switch (reportType)
            {
                case "individual":
                    sb.AppendLine("<th>Date</th>");
                    sb.AppendLine("<th>Clock In</th>");
                    sb.AppendLine("<th>Clock Out</th>");
                    sb.AppendLine("<th>Regular Hours</th>");
                    sb.AppendLine("<th>Overtime Hours</th>");
                    sb.AppendLine("<th>Description</th>");
                    sb.AppendLine("<th>Notes</th>");
                    break;
                case "overtime":
                    sb.AppendLine("<th>Employee</th>");
                    sb.AppendLine("<th>Week</th>");
                    sb.AppendLine("<th>Overtime Hours</th>");
                    sb.AppendLine("<th>Overtime Cost</th>");
                    break;
            }
            sb.AppendLine("</tr>");

            // Table rows
            foreach (var item in reportData)
            {
                IDictionary<string, object> itemDict = (IDictionary<string, object>)item;
                sb.AppendLine("<tr>");

                switch (reportType)
                {
                    case "individual":
                        sb.AppendLine($"<td>{itemDict["Date"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["ClockIn"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["ClockOut"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["RegularHours"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["OvertimeHours"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["Description"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["Notes"] ?? ""}</td>");
                        break;
                    case "overtime":
                        sb.AppendLine($"<td>{itemDict["EmployeeName"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["WeekLabel"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["OvertimeHours"] ?? ""}</td>");
                        sb.AppendLine($"<td>{itemDict["OvertimeCost"] ?? ""}</td>");
                        break;
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            // Create PDF
            var htmlToPdfDocument = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = sb.ToString(),
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return Task.FromResult(_pdfConverter.Convert(htmlToPdfDocument));
        }

        #endregion
    }
}
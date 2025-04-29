using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.Admin
{
    [Authorize(Roles = "Administrator,Manager")]
    public class ReportsModel : PageModel
    {
        private readonly ITimeEntryRepository _timeEntryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsModel> _logger;

        public ReportsModel(
            ITimeEntryRepository timeEntryRepository,
            IUserRepository userRepository,
            IReportService reportService,
            ILogger<ReportsModel> logger)
        {
            _timeEntryRepository = timeEntryRepository;
            _userRepository = userRepository;
            _reportService = reportService;
            _logger = logger;

            // Initialize non-nullable property
            StatusMessage = string.Empty;
        }

        [BindProperty(SupportsGet = true)]
        public string ReportType { get; set; } = "individual";

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = "thisWeek";

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string GroupBy { get; set; } = "day";

        [BindProperty(SupportsGet = true)]
        public int? EmployeeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IncludeDetails { get; set; } = false;

        public List<User> Employees { get; set; } = new List<User>();

        // Report data
        public List<dynamic> ReportData { get; set; } = new List<dynamic>();
        public bool HasData { get; private set; }
        public double TotalHours { get; private set; }
        public double RegularHours { get; private set; }
        public double OvertimeHours { get; private set; }
        public int DaysWorked { get; private set; }

        // Chart data
        public List<string> ChartLabels { get; private set; } = new List<string>();
        public List<double> ChartRegularHours { get; private set; } = new List<double>();
        public List<double> ChartOvertimeHours { get; private set; } = new List<double>();

        public string StatusMessage { get; set; }
        public bool IsError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load filter options
                await LoadFilterOptions();

                // Set date range if not already set
                SetDateRange();

                // Generate report if filters are set
                if (ShouldGenerateReport())
                {
                    await GenerateReportData();
                    HasData = ReportData.Any();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                StatusMessage = "An error occurred while loading reports.";
                IsError = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnGetExportReportAsync(string format, bool includeCharts, bool includeSummary)
        {
            try
            {
                // Set date range
                SetDateRange();

                // Generate report data
                await GenerateReportData();

                if (!ReportData.Any())
                {
                    TempData["StatusMessage"] = "No data available to export.";
                    TempData["IsError"] = true;
                    return RedirectToPage();
                }

                // Export based on format
                string fileName = $"{ReportType}_report_{DateTime.Now:yyyy-MM-dd}";
                byte[] fileContent;

                switch (format.ToLower())
                {
                    case "excel":
                        fileContent = await _reportService.ExportToExcel(ReportType, ReportData, ChartLabels,
                            ChartRegularHours, ChartOvertimeHours, includeCharts, includeSummary);
                        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx");

                    case "csv":
                        fileContent = await _reportService.ExportToCsv(ReportType, ReportData);
                        return File(fileContent, "text/csv", $"{fileName}.csv");

                    case "pdf":
                        fileContent = await _reportService.ExportToPdf(ReportType, ReportData, ChartLabels,
                            ChartRegularHours, ChartOvertimeHours, includeCharts, includeSummary);
                        return File(fileContent, "application/pdf", $"{fileName}.pdf");

                    default:
                        TempData["StatusMessage"] = "Invalid export format.";
                        TempData["IsError"] = true;
                        return RedirectToPage();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                TempData["StatusMessage"] = "An error occurred while exporting the report.";
                TempData["IsError"] = true;
                return RedirectToPage();
            }
        }

        private async Task LoadFilterOptions()
        {
            // Load employees for dropdown
            Employees = (await _userRepository.GetAllAsync()).ToList();
        }

        private void SetDateRange()
        {
            if (DateRange != "custom" || StartDate == null && EndDate == null)
            {
                // Calculate date range based on selection
                var today = DateTime.Today;

                switch (DateRange)
                {
                    case "thisWeek":
                        var firstDayOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        StartDate = firstDayOfWeek;
                        EndDate = firstDayOfWeek.AddDays(6);
                        break;

                    case "lastWeek":
                        var firstDayOfLastWeek = today.AddDays(-(int)today.DayOfWeek - 7);
                        StartDate = firstDayOfLastWeek;
                        EndDate = firstDayOfLastWeek.AddDays(6);
                        break;

                    case "thisMonth":
                        StartDate = new DateTime(today.Year, today.Month, 1);
                        EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
                        break;

                    case "lastMonth":
                        var firstDayOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                        StartDate = firstDayOfLastMonth;
                        EndDate = firstDayOfLastMonth.AddMonths(1).AddDays(-1);
                        break;

                    default:
                        // Default to this week
                        var defaultFirstDay = today.AddDays(-(int)today.DayOfWeek);
                        StartDate = defaultFirstDay;
                        EndDate = defaultFirstDay.AddDays(6);
                        break;
                }
            }
        }

        private bool ShouldGenerateReport()
        {
            // Check if required filters are set
            if (!StartDate.HasValue || !EndDate.HasValue)
                return false;

            if (ReportType == "individual" && !EmployeeId.HasValue)
                return false;

            return true;
        }

        private async Task GenerateReportData()
        {
            // Call appropriate report service method based on report type
            switch (ReportType)
            {
                case "individual":
                    if (EmployeeId.HasValue)
                    {
                        var reportResult = await _reportService.GetIndividualReportAsync(
                            EmployeeId.Value,
                            StartDate!.Value, 
                            EndDate!.Value,
                            IncludeDetails);

                        ReportData = reportResult.Data;
                        TotalHours = reportResult.TotalHours;
                        RegularHours = reportResult.RegularHours;
                        OvertimeHours = reportResult.OvertimeHours;
                        DaysWorked = reportResult.DaysWorked;
                        ChartLabels = reportResult.ChartLabels;
                        ChartRegularHours = reportResult.ChartRegularHours;
                        ChartOvertimeHours = reportResult.ChartOvertimeHours;
                    }
                    break;

                case "overtime":
                    var overtimeReport = await _reportService.GetOvertimeReportAsync(
                        StartDate!.Value, // Using null-forgiving operator because ShouldGenerateReport ensures this is not null
                        EndDate!.Value);

                    ReportData = overtimeReport.Data;
                    TotalHours = overtimeReport.TotalHours;
                    RegularHours = overtimeReport.RegularHours;
                    OvertimeHours = overtimeReport.OvertimeHours;
                    DaysWorked = overtimeReport.DaysWorked;
                    ChartLabels = overtimeReport.ChartLabels;
                    ChartRegularHours = overtimeReport.ChartRegularHours;
                    ChartOvertimeHours = overtimeReport.ChartOvertimeHours;
                    break;

                default:
                    // Default to individual report if type is not recognized
                    if (EmployeeId.HasValue)
                    {
                        var defaultReport = await _reportService.GetIndividualReportAsync(
                            EmployeeId.Value,
                            StartDate!.Value, 
                            EndDate!.Value,
                            IncludeDetails);

                        ReportData = defaultReport.Data;
                        TotalHours = defaultReport.TotalHours;
                        RegularHours = defaultReport.RegularHours;
                        OvertimeHours = defaultReport.OvertimeHours;
                        DaysWorked = defaultReport.DaysWorked;
                        ChartLabels = defaultReport.ChartLabels;
                        ChartRegularHours = defaultReport.ChartRegularHours;
                        ChartOvertimeHours = defaultReport.ChartOvertimeHours;
                    }
                    break;
            }
        }
    }
}
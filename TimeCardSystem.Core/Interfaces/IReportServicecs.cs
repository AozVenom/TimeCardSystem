using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimeCardSystem.Core.Interfaces
{
    /// <summary>
    /// Interface for report generation services
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Generates an individual employee report
        /// </summary>
        /// <param name="employeeId">The employee ID</param>
        /// <param name="startDate">Start date of the report period</param>
        /// <param name="endDate">End date of the report period</param>
        /// <param name="includeDetails">Whether to include daily time entry details</param>
        /// <returns>A report result containing the report data and summary information</returns>
        Task<ReportResult> GetIndividualReportAsync(int employeeId, DateTime startDate, DateTime endDate, bool includeDetails);

        /// <summary>
        /// Generates an overtime report
        /// </summary>
        /// <param name="startDate">Start date of the report period</param>
        /// <param name="endDate">End date of the report period</param>
        /// <returns>A report result containing the report data and summary information</returns>
        Task<ReportResult> GetOvertimeReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Exports report data to Excel format
        /// </summary>
        /// <param name="reportType">The type of report being exported</param>
        /// <param name="reportData">The report data to export</param>
        /// <param name="chartLabels">Labels for the chart</param>
        /// <param name="chartRegularHours">Regular hours data for the chart</param>
        /// <param name="chartOvertimeHours">Overtime hours data for the chart</param>
        /// <param name="includeCharts">Whether to include charts in the export</param>
        /// <param name="includeSummary">Whether to include summary information in the export</param>
        /// <returns>The Excel file as a byte array</returns>
        Task<byte[]> ExportToExcel(string reportType, List<dynamic> reportData, List<string> chartLabels,
            List<double> chartRegularHours, List<double> chartOvertimeHours, bool includeCharts, bool includeSummary);

        /// <summary>
        /// Exports report data to CSV format
        /// </summary>
        /// <param name="reportType">The type of report being exported</param>
        /// <param name="reportData">The report data to export</param>
        /// <returns>The CSV file as a byte array</returns>
        Task<byte[]> ExportToCsv(string reportType, List<dynamic> reportData);

        /// <summary>
        /// Exports report data to PDF format
        /// </summary>
        /// <param name="reportType">The type of report being exported</param>
        /// <param name="reportData">The report data to export</param>
        /// <param name="chartLabels">Labels for the chart</param>
        /// <param name="chartRegularHours">Regular hours data for the chart</param>
        /// <param name="chartOvertimeHours">Overtime hours data for the chart</param>
        /// <param name="includeCharts">Whether to include charts in the export</param>
        /// <param name="includeSummary">Whether to include summary information in the export</param>
        /// <returns>The PDF file as a byte array</returns>
        Task<byte[]> ExportToPdf(string reportType, List<dynamic> reportData, List<string> chartLabels,
            List<double> chartRegularHours, List<double> chartOvertimeHours, bool includeCharts, bool includeSummary);
    }

    /// <summary>
    /// Contains report data and summary information
    /// </summary>
    public class ReportResult
    {
        /// <summary>
        /// The report data rows
        /// </summary>
        public List<dynamic> Data { get; set; } = new List<dynamic>();

        /// <summary>
        /// Total hours worked
        /// </summary>
        public double TotalHours { get; set; }

        /// <summary>
        /// Regular hours worked
        /// </summary>
        public double RegularHours { get; set; }

        /// <summary>
        /// Overtime hours worked
        /// </summary>
        public double OvertimeHours { get; set; }

        /// <summary>
        /// Number of days worked
        /// </summary>
        public int DaysWorked { get; set; }

        /// <summary>
        /// Labels for the chart
        /// </summary>
        public List<string> ChartLabels { get; set; } = new List<string>();

        /// <summary>
        /// Regular hours data for the chart
        /// </summary>
        public List<double> ChartRegularHours { get; set; } = new List<double>();

        /// <summary>
        /// Overtime hours data for the chart
        /// </summary>
        public List<double> ChartOvertimeHours { get; set; } = new List<double>();
    }
}

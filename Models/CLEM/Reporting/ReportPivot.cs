﻿using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.Storage;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Provides utility to quickly summarise data from a report
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.CLEMView")]
    [PresenterName("UserInterface.Presenters.ReportPivotPresenter")]
    [ValidParent(ParentType = typeof(Report))]
    [Description("Generates a pivot table from a Report")]
    [Version(1, 0, 0, "")]
    public class ReportPivot : Model, ICLEMUI
    {
        [Link]
        private IDataStore datastore = null;

        /// <summary>
        /// The query generated by the pivot
        /// </summary>
        public string SQL { get; private set; }

        /// <summary>
        /// Tracks the active selection in the value box
        /// </summary>
        [Description("Values column")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetValueNames))]
        public string Value { get; set; }

        /// <summary>
        /// Populates the value filter
        /// </summary>
        public string[] GetValueNames() => GetColumnPivotOptions(true);

        /// <summary>
        /// Tracks the active selection in the row box
        /// </summary>
        [Description("Rows column")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetRowNames))]
        public string Row { get; set; }

        /// <summary>
        /// Populates the row filter
        /// </summary>
        public string[] GetRowNames() => GetColumnPivotOptions(false);

        /// <summary>
        /// Tracks the active selection in the column box
        /// </summary>
        [Description("Columns columns")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetColumnNames))]
        public string Column { get; set; }

        /// <summary>
        /// Populates the column filter
        /// </summary>
        public string[] GetColumnNames() => GetColumnPivotOptions(false);

        /// <summary>
        /// Tracks the active selection in the time box
        /// </summary>
        [Description("Time filter")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetTimes))]
        public string Time { get; set; }

        /// <summary>
        /// Populates the time filter
        /// </summary>
        public string[] GetTimes() => new string[] { "Day", "Month", "Year" };

        /// <summary>
        /// Tracks the active selection in the time box
        /// </summary>
        [Description("Aggregation method")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetAggregators))]
        public string Aggregator { get; set; }

        /// <summary>
        /// Populates the aggregate filter
        /// </summary>
        public string[] GetAggregators() => new string[] { "SUM", "AVG", "MAX", "MIN", "COUNT" };

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Searches the columns of the parent report for the pivot options
        /// </summary>
        /// <param name="value"><see langword="true"/> if we are searching for value options</param>
        public string[] GetColumnPivotOptions(bool value)
        {
            // Find the data from the parent report
            var storage = FindInScope<IDataStore>();
            var report = storage.Reader.GetData(Parent.Name);

            if (report is null)
                return new string[] { "No available data" };

            // Find the columns that meet our criteria
            var columns = report.Columns.Cast<DataColumn>();
            var result = columns.Where(c => !value ^ HasDataValues(c))
                .Select(c => c.ColumnName)
                .ToArray();

            return result;
        }

        /// <summary>
        /// Test if a column contains data values
        /// </summary>
        /// <param name="col">The column being tested</param>
        /// <returns>
        /// <see langword="true"/> if the column contains data values,
        /// <see langword="false"/> otherwise 
        /// </returns>
        private bool HasDataValues(DataColumn col)
        {
            // Assume no value types are represented in string form
            if (col.DataType.Name == "String")
                return false;

            // We are looking for data values, not IDs
            if (col.ColumnName.EndsWith("ID"))
                return false;

            // DateTime is handled separately from other value types
            return col.DataType != typeof(DateTime);
        }

        /// <summary>
        /// Generates the pivot table
        /// </summary>
        public DataTable GenerateTable()
        {
            if (Name.Any(c => c == ' '))
                throw new Exception($"Invalid name: {Name}\nNames cannot contain spaces.");

            // Find the data
            var storage = FindInScope<IDataStore>() ?? datastore;
            var report = storage.Reader.GetData(Parent.Name);

            // Check sensibility
            if (report is null || Row is null || Column is null || Value is null || Aggregator is null)
                return null;

            // Set up date handling
            bool isDate = report.Columns[Column].DataType == typeof(DateTime);
            var cs_format = Time == "Year" ? "yyyy-01-01" : Time == "Month" ? "yyyy-MM-01" : "yyyy-MM-dd";
            var sql_format = Time == "Year" ? "%Y-01-01" : Time == "Month" ? "%Y-%m-01" : "%Y-%m-%d";

            string test = isDate 
                ? Time == "Day" ? $"[{Column}]" : $"datetime(strftime('{sql_format}', [{Column}]))" 
                : $"[{Column}]";            

            // Set up the columns in the pivot
            var cols = report
                .AsEnumerable()
                .Select(r => isDate ? ((DateTime)r[Column]).ToString(cs_format) : r[Column])
                .Distinct()
                .Select(o => $"{Aggregator}(CASE WHEN {test} == '{o}' THEN {Value} ELSE NULL END) AS '{o} {Value}'");

            // Set up the rows in the pivot
            var rows = $"[{Row}]";
            if (report.Columns[Row].DataType == typeof(DateTime))
                rows = Time == "Day" ? $"[{Row}]" : $"datetime(strftime('{sql_format}', [{Row}]))";

            // Construct the SQL statement
            var builder = new StringBuilder();
            builder.AppendLine($"SELECT {rows} AS {Row},");
            builder.AppendLine(string.Join(",\n", cols));
            builder.AppendLine($"FROM [{Parent.Name}] GROUP BY {rows}");

            // Execute the query
            SQL = builder.ToString();
            storage.Reader.ExecuteSql(SQL);
            storage.AddView($"{Name}", SQL);
            return storage.Reader.GetDataUsingSql(SQL);
        }

        /// <summary>
        /// Saves the view post-simulation
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e) => GenerateTable();
    }
}

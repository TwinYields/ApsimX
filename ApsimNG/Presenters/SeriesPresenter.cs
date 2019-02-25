﻿// -----------------------------------------------------------------------
// <copyright file="SeriesPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using EventArguments;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Models.Graph;
    using Views;
    using Commands;
    /// <summary>
    /// A presenter class for graph series.
    /// </summary>
    public class SeriesPresenter : IPresenter
    {
        /// <summary>
        /// The storage
        /// </summary>
        [Link]
        private IStorageReader storage = null;

        /// <summary>The graph model to work with.</summary>
        private Series series;

        /// <summary>The series view to work with.</summary>
        private ISeriesView seriesView;

        /// <summary>The parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The graph presenter</summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Attach the model and view to this presenter.</summary>
        /// <param name="model">The graph model to work with</param>
        /// <param name="view">The series view to work with</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.series = model as Series;
            this.seriesView = view as SeriesView;
            this.explorerPresenter = explorerPresenter;
            intellisense = new IntellisensePresenter(seriesView as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            Graph parentGraph = Apsim.Parent(series, typeof(Graph)) as Graph;
            if (parentGraph != null)
            {
                graphPresenter = new GraphPresenter();
                explorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                graphPresenter.Attach(parentGraph, seriesView.GraphView, explorerPresenter);
            }

            try
            {
                PopulateView();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }

            ConnectViewEvents();
        }

        /// <summary>Detach the model and view from this presenter.</summary>
        public void Detach()
        {
            seriesView.EndEdit();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            if (graphPresenter != null)
            {
                graphPresenter.Detach();
            }

            DisconnectViewEvents();
        }

        /// <summary>Connect all view events.</summary>
        private void ConnectViewEvents()
        {
            seriesView.Checkpoint.Changed += OnCheckpointChanged;
            seriesView.DataSource.Changed += OnDataSourceChanged;
            seriesView.SeriesType.Changed += OnSeriesTypeChanged;
            seriesView.LineType.Changed += OnLineTypeChanged;
            seriesView.MarkerType.Changed += OnMarkerTypeChanged;
            seriesView.LineThickness.Changed += OnLineThicknessChanged;
            seriesView.MarkerSize.Changed += OnMarkerSizeChanged;
            seriesView.Colour.Changed += OnColourChanged;
            seriesView.XOnTop.Changed += OnXOnTopChanged;
            seriesView.YOnRight.Changed += OnYOnRightChanged;
            seriesView.X.Changed += OnXChanged;
            seriesView.Y.Changed += OnYChanged;
            seriesView.X2.Changed += OnX2Changed;
            seriesView.Y2.Changed += OnY2Changed;
            seriesView.ShowInLegend.Changed += OnShowInLegendChanged;
            seriesView.IncludeSeriesNameInLegend.Changed += OnIncludeSeriesNameInLegendChanged;
            seriesView.YCumulative.Changed += OnCumulativeYChanged;
            seriesView.XCumulative.Changed += OnCumulativeXChanged;
            seriesView.Filter.Changed += OnFilterChanged;
            seriesView.Filter.IntellisenseItemsNeeded += OnIntellisenseItemsNeeded;
        }

        /// <summary>Disconnect all view events.</summary>
        private void DisconnectViewEvents()
        {
            seriesView.Checkpoint.Changed -= OnCheckpointChanged;
            seriesView.DataSource.Changed -= OnDataSourceChanged;
            seriesView.SeriesType.Changed -= OnSeriesTypeChanged;
            seriesView.LineType.Changed -= OnLineTypeChanged;
            seriesView.MarkerType.Changed -= OnMarkerTypeChanged;
            seriesView.LineThickness.Changed += OnLineThicknessChanged;
            seriesView.MarkerSize.Changed += OnMarkerSizeChanged;
            seriesView.Colour.Changed -= OnColourChanged;
            seriesView.XOnTop.Changed -= OnXOnTopChanged;
            seriesView.YOnRight.Changed -= OnYOnRightChanged;
            seriesView.X.Changed -= OnXChanged;
            seriesView.Y.Changed -= OnYChanged;
            seriesView.X2.Changed -= OnX2Changed;
            seriesView.Y2.Changed -= OnY2Changed;
            seriesView.ShowInLegend.Changed -= OnShowInLegendChanged;
            seriesView.IncludeSeriesNameInLegend.Changed -= OnIncludeSeriesNameInLegendChanged;
            seriesView.YCumulative.Changed -= OnCumulativeYChanged;
            seriesView.XCumulative.Changed -= OnCumulativeXChanged;
            seriesView.Filter.Changed -= OnFilterChanged;
            seriesView.Filter.IntellisenseItemsNeeded += OnIntellisenseItemsNeeded;
        }

        /// <summary>Set the value of the graph models property</summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelProperty(string name, object value)
        {
            try
            {
                ChangeProperty command = new ChangeProperty(series, name, value);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense window.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                seriesView.Filter.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        #region Events from the view

        /// <summary>Series type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesTypeChanged(object sender, EventArgs e)
        {
            SeriesType seriesType = (SeriesType)Enum.Parse(typeof(SeriesType), this.seriesView.SeriesType.SelectedValue);
            this.SetModelProperty("Type", seriesType);
            
            // This doesn't quite work yet. If the previous series was a scatter plot, there is no x2, y2 to work with
            // and things go a bit awry.
            // this.seriesView.ShowX2Y2(series.Type == SeriesType.Area);
        }

        /// <summary>Series line type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnLineTypeChanged(object sender, EventArgs e)
        {
            LineType lineType;
            if (Enum.TryParse<LineType>(this.seriesView.LineType.SelectedValue, out lineType))
            {
                this.SetModelProperty("Line", lineType);
                this.SetModelProperty("FactorToVaryLines", null);
            }
            else
                this.SetModelProperty("FactorToVaryLines", this.seriesView.LineType.SelectedValue.Replace("Vary by ", ""));
        }
        
        /// <summary>Series marker type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMarkerTypeChanged(object sender, EventArgs e)
        {
            MarkerType markerType;
            if (Enum.TryParse<MarkerType>(this.seriesView.MarkerType.SelectedValue, out markerType))
            {
                this.SetModelProperty("Marker", markerType);
                this.SetModelProperty("FactorToVaryMarkers", null);
            }
            else
                this.SetModelProperty("FactorToVaryMarkers", this.seriesView.MarkerType.SelectedValue.Replace("Vary by ", ""));
        }

        /// <summary>Series line thickness has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnLineThicknessChanged(object sender, EventArgs e)
        {
            LineThicknessType lineThickness;
            if (Enum.TryParse<LineThicknessType>(this.seriesView.LineThickness.SelectedValue, out lineThickness))
            {
                this.SetModelProperty("LineThickness", lineThickness);
            }
        }

        /// <summary>Series marker size has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMarkerSizeChanged(object sender, EventArgs e)
        {
            MarkerSizeType markerSize;
            if (Enum.TryParse<MarkerSizeType>(this.seriesView.MarkerSize.SelectedValue, out markerSize))
            {
                this.SetModelProperty("MarkerSize", markerSize);
            }
        }

        /// <summary>Series color has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnColourChanged(object sender, EventArgs e)
        {
            object obj = seriesView.Colour.SelectedValue;
            if (obj is Color)
            {
                this.SetModelProperty("Colour", obj);
                this.SetModelProperty("FactorToVaryColours", null);
            }
            else
                this.SetModelProperty("FactorToVaryColours", obj.ToString().Replace("Vary by ", ""));
        }

        /// <summary>X on top has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXOnTopChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Bottom;
            if (this.seriesView.XOnTop.IsChecked)
            {
                axisType = Axis.AxisType.Top;
            }

            this.SetModelProperty("XAxis", axisType);
        }

        /// <summary>Y on right has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYOnRightChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Left;
            if (this.seriesView.YOnRight.IsChecked)
            {
                axisType = Axis.AxisType.Right;
            }

            this.SetModelProperty("YAxis", axisType);
        }

        /// <summary>X has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("XFieldName", seriesView.X.SelectedValue);
        }

        /// <summary>Y has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("YFieldName", seriesView.Y.SelectedValue);
        }

        /// <summary>Cumulative check box has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Cumulative", this.seriesView.YCumulative.IsChecked);
        }

        /// <summary>Cumulative X check box has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("CumulativeX", this.seriesView.XCumulative.IsChecked);
        }

        /// <summary>X2 has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnX2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("X2FieldName", seriesView.X2.SelectedValue);
        }

        /// <summary>Y2 has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnY2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("Y2FieldName", seriesView.Y2.SelectedValue);
        }

        /// <summary>User has changed the data source.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDataSourceChanged(object sender, EventArgs e)
        {
            if (series.TableName != this.seriesView.DataSource.SelectedValue)
            {
                this.SetModelProperty("TableName", this.seriesView.DataSource.SelectedValue);
                List<string> warnings = PopulateFieldNames();
                if (warnings != null && warnings.Count > 0)
                {
                    explorerPresenter.MainPresenter.ClearStatusPanel();
                    explorerPresenter.MainPresenter.ShowMessage(warnings, Simulation.MessageType.Warning);
                }
            }
        }

        /// <summary>User has changed the checkpoint.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckpointChanged(object sender, EventArgs e)
        {
            if (series.Checkpoint != this.seriesView.Checkpoint.SelectedValue)
                this.SetModelProperty("Checkpoint", this.seriesView.Checkpoint.SelectedValue);
        }

        /// <summary>User has changed the show in legend</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShowInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("ShowInLegend", this.seriesView.ShowInLegend.IsChecked);
        }

        /// <summary>User has changed the include series name in legend</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnIncludeSeriesNameInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("IncludeSeriesNameInLegend", this.seriesView.IncludeSeriesNameInLegend.IsChecked);
        }

        /// <summary>User has changed the filter</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Filter", this.seriesView.Filter.Value);
        }

        /// <summary>
        /// Invoked when the user is asking for items for the intellisense.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemsNeeded(object sender, NeedContextItemsArgs args)
        {
            try
            {
                if (intellisense.GenerateSeriesCompletions(args.Code, args.Offset, seriesView.DataSource.SelectedValue, storage))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        #endregion

        /// <summary>Populate the views series editor with the current selected series.</summary>
        private void PopulateView()
        {
            List<string> warnings = new List<string>();

            warnings.AddRange(PopulateMarkerDropDown());
            warnings.AddRange(PopulateLineDropDown());
            warnings.AddRange(PopulateColourDropDown());

            // Populate the checkpoint drop down.
            List<string> checkpoints = storage.Checkpoints().ToList();
            if (!checkpoints.Contains(series.Checkpoint) && !string.IsNullOrEmpty(series.Checkpoint))
            {
                checkpoints.Add(series.Checkpoint);
                warnings.Add(string.Format("WARNING: {0}: Selected Checkpoint '{1}' is invalid. Have the simulations been run?", Apsim.FullPath(series), series.Checkpoint));
            }
            seriesView.Checkpoint.Values = checkpoints.ToArray();
            seriesView.Checkpoint.SelectedValue = series.Checkpoint;

            // Populate line thickness drop down.
            List<string> thicknesses = new List<string>(Enum.GetNames(typeof(LineThicknessType)));
            if (!thicknesses.Contains(series.LineThickness.ToString()) && !string.IsNullOrEmpty(series.LineThickness.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                thicknesses.Add(series.LineThickness.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected line thickness '{1}' is invalid. This could be a relic from an older version of APSIM.", Apsim.FullPath(series), series.LineThickness.ToString()));
            }
            this.seriesView.LineThickness.Values = thicknesses.ToArray();
            this.seriesView.LineThickness.SelectedValue = series.LineThickness.ToString();

            // Populate marker size drop down.
            List<string> sizes = new List<string>(Enum.GetNames(typeof(MarkerSizeType)));
            if (!sizes.Contains(series.MarkerSize.ToString()) && !string.IsNullOrEmpty(series.MarkerSize.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                sizes.Add(series.MarkerSize.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected marker size '{1}' is invalid. This could be a relic from an older version of APSIM.", Apsim.FullPath(series), series.MarkerSize));
            }
            this.seriesView.MarkerSize.Values = sizes.ToArray();
            this.seriesView.MarkerSize.SelectedValue = series.MarkerSize.ToString();

            // Populate series type drop down.
            List<string> seriesTypes = new List<string>(Enum.GetNames(typeof(SeriesType)));
            if (!seriesTypes.Contains(series.Type.ToString()) && !string.IsNullOrEmpty(series.Type.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                seriesTypes.Add(series.Type.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected series type '{1}' is invalid. This could be a relic from an older version of APSIM.", Apsim.FullPath(series), series.Type));
            }
            this.seriesView.SeriesType.Values = seriesTypes.ToArray();
            this.seriesView.SeriesType.SelectedValue = series.Type.ToString();

            // Populate checkboxes.
            this.seriesView.XOnTop.IsChecked = series.XAxis == Axis.AxisType.Top;
            this.seriesView.YOnRight.IsChecked = series.YAxis == Axis.AxisType.Right;
            this.seriesView.ShowInLegend.IsChecked = series.ShowInLegend;
            this.seriesView.IncludeSeriesNameInLegend.IsChecked = series.IncludeSeriesNameInLegend;
            this.seriesView.XCumulative.IsChecked = series.CumulativeX;
            this.seriesView.YCumulative.IsChecked = series.Cumulative;

            // Populate data source drop down.
            List<string> dataSources = storage.TableNames.ToList();
            if (!dataSources.Contains(series.TableName))
            {
                dataSources.Add(series.TableName);
                warnings.Add(string.Format("WARNING: {0}: Selected Data Source '{1}' does not exist in the datastore. Have the simulations been run?", Apsim.FullPath(series), series.TableName));
            }
            dataSources.Sort();
            this.seriesView.DataSource.Values = dataSources.ToArray();
            this.seriesView.DataSource.SelectedValue = series.TableName;

            // Populate field name drop downs.
            warnings.AddRange(PopulateFieldNames());

            // Populate filter textbox.
            this.seriesView.Filter.Value = series.Filter;

            this.seriesView.ShowX2Y2(series.Type == SeriesType.Area);

            explorerPresenter.MainPresenter.ClearStatusPanel();
            if (warnings != null && warnings.Count > 0)
            {
                explorerPresenter.MainPresenter.ShowMessage(warnings, Simulation.MessageType.Warning);
            }
        }

        /// <summary>Populate the line drop down.</summary>
        private List<string> PopulateLineDropDown()
        {
            List<string> warnings = new List<string>();

            List<string> values = new List<string>(Enum.GetNames(typeof(LineType)));
            if (series.FactorNamesForVarying != null)
                values.AddRange(series.FactorNamesForVarying.Select(factorName => "Vary by " + factorName));

            string selectedValue;
            if (series.FactorToVaryLines == null)
                selectedValue = series.Line.ToString();
            else
                selectedValue = "Vary by " + series.FactorToVaryLines;

            if (!values.Contains(selectedValue) && !string.IsNullOrEmpty(selectedValue))
            {
                values.Add(selectedValue);
                warnings.Add(string.Format("WARNING: {0}: Selected line type '{1}' is invalid.", Apsim.FullPath(series), selectedValue));
            }
            this.seriesView.LineType.Values = values.ToArray();
            this.seriesView.LineType.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Populate the marker drop down.</summary>
        private List<string> PopulateMarkerDropDown()
        {
            List<string> warnings = new List<string>();

            List<string> values = new List<string>(Enum.GetNames(typeof(MarkerType)));
            if (series.FactorNamesForVarying != null)
                values.AddRange(series.FactorNamesForVarying.Select(factorName => "Vary by " + factorName));

            string selectedValue;
            if (series.FactorToVaryMarkers == null)
                selectedValue = series.Marker.ToString();
            else
                selectedValue = "Vary by " + series.FactorToVaryMarkers;

            if (!values.Contains(selectedValue) && !string.IsNullOrEmpty(selectedValue))
            {
                values.Add(selectedValue);
                warnings.Add(string.Format("WARNING: {0}: Selected marker type '{1}' is invalid.", Apsim.FullPath(series), selectedValue));
            }

            this.seriesView.MarkerType.Values = values.ToArray();
            this.seriesView.MarkerType.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Populate the colour drop down in the view.</summary>
        private List<string> PopulateColourDropDown()
        {
            List<string> warnings = new List<string>();
            List<object> colourOptions = new List<object>();
            foreach (Color colour in ColourUtilities.Colours)
                colourOptions.Add(colour);

            // Send colour options to view.
            if (series.FactorNamesForVarying != null)
                colourOptions.AddRange(series.FactorNamesForVarying.Select(factorName => "Vary by " + factorName));

            object selectedValue;
            if (series.FactorToVaryColours == null)
                selectedValue = series.Colour;
            else
                selectedValue = "Vary by " + series.FactorToVaryColours;

            if (!colourOptions.Contains(selectedValue) && selectedValue != null)
            {
                colourOptions.Add(selectedValue);
                // If selectedValue is not a string, then it is probably a custom colour.
                // In such a scenario, we don't show a warning, as we can display it with no problems.
                if (selectedValue is string)
                    warnings.Add(string.Format("WARNING: {0}: Selected colour '{1}' is invalid.", Apsim.FullPath(series), selectedValue));
            }

            this.seriesView.Colour.Values = colourOptions.ToArray();
            this.seriesView.Colour.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Gets a list of valid field names for the view.</summary>
        private List<string> GetFieldNames()
        {
            List<string> fieldNames = new List<string>();

            if (this.seriesView.DataSource != null && !string.IsNullOrEmpty(this.seriesView.DataSource.SelectedValue))
            {
                fieldNames.Add("SimulationName");
                fieldNames.AddRange(storage.ColumnNames(seriesView.DataSource.SelectedValue));
                fieldNames.Sort();
            }
            return fieldNames;
        }

        /// <summary>
        /// Populates the field names in the view, and returns a list of warnings.
        /// </summary>
        /// <returns>List of warning messages.</returns>
        private List<string> PopulateFieldNames()
        {
            List<string> fieldNames = GetFieldNames();
            List<string> warnings = new List<string>();
            this.seriesView.X.Values = fieldNames.ToArray();
            this.seriesView.Y.Values = fieldNames.ToArray();
            this.seriesView.X2.Values = fieldNames.ToArray();
            this.seriesView.Y2.Values = fieldNames.ToArray();

            if (!this.seriesView.X.Values.Contains(series.XFieldName) && !string.IsNullOrEmpty(series.XFieldName))
            {
                this.seriesView.X.Values = this.seriesView.X.Values.Concat(new string[] { series.XFieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected X field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", Apsim.FullPath(series), series.XFieldName, series.TableName));
            }
            this.seriesView.X.SelectedValue = series.XFieldName;

            if (!this.seriesView.Y.Values.Contains(series.YFieldName) && !string.IsNullOrEmpty(series.YFieldName))
            {
                this.seriesView.Y.Values = this.seriesView.Y.Values.Concat(new string[] { series.YFieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected Y field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", Apsim.FullPath(series), series.YFieldName, series.TableName));
            }
            this.seriesView.Y.SelectedValue = series.YFieldName;

            if (!this.seriesView.X2.Values.Contains(series.X2FieldName) && !string.IsNullOrEmpty(series.X2FieldName))
            {
                this.seriesView.X2.Values = this.seriesView.X2.Values.Concat(new string[] { series.X2FieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected X2 field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", Apsim.FullPath(series), series.X2FieldName, series.TableName));
            }
            this.seriesView.X2.SelectedValue = series.X2FieldName;

            if (!this.seriesView.Y2.Values.Contains(series.Y2FieldName) && !string.IsNullOrEmpty(series.Y2FieldName))
            {
                this.seriesView.Y2.Values = this.seriesView.Y2.Values.Concat(new string[] { series.Y2FieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected Y2 field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", Apsim.FullPath(series), series.Y2FieldName, series.TableName));
            }
            this.seriesView.Y2.SelectedValue = series.Y2FieldName;

            return warnings;
        }
    }
}

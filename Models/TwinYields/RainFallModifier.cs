using System;
using Models.Core;
using Models.Climate;
using Models.Interfaces;
using Models.PMF;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>This is a simple rainfall modifier model.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]

    public class RainfallModifier : Model
    {
        [Link]
        Clock clock = null;

        [Link]
        Weather weather = null;

        /// <summary>Start date for modifying rainfall</summary>
        [Description("Start modifying rainfall from date:")]
        public DateTime StartDate { get; set; }

        /// <summary>Rainfall muliplier</summary>
        [Description("Rainfall multiplier: ")]
        public double RainfallMultiplier { get; set; }

        /// <summary>Rainfall addition</summary>
        [Description("Rainfall addition (mm): ")]
        public double RainfallAddition { get; set; }

        /// <summary>An output variable.</summary>
        public double OriginalRain { get; private set; }

        /// <summary>Handler for event invoked by weather model to allow modification of weather variables.</summary>
        [EventSubscribe("PreparingNewWeatherData")]
        private void ModifyWeatherData(object sender, EventArgs e)
        {
            OriginalRain = weather.Rain;
            Console.WriteLine(OriginalRain.ToString());
            if (clock.Today >= StartDate)
                weather.Rain = RainfallMultiplier * weather.Rain + RainfallAddition;
        }
    }
}
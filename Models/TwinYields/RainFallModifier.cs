using System;
using Models.Core;
using Models.Climate;
using Models.Interfaces;
using Models.PMF;
using APSIM.Shared.Utilities;
using APSIM.Shared.APSoil;

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

        [Link]
        ISoilWater swat = null;

        [Link]
        private Simulation simulation = null;

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
            if (clock.Today >= StartDate)
                Console.WriteLine(OriginalRain.ToString());
            //    weather.Rain = RainfallMultiplier * weather.Rain + RainfallAddition;

        }


        [EventSubscribe("DoSoilWaterMovement")]
        private void SoilWaterUpdate(object sender, EventArgs e) {
            //double[] sw = soilWater
            //sw[1] = 0.5;
            //soilWater.SW = sw;
            var Orig = (double[])swat.SW.Clone();
            swat.SW[0] = 0.0;
            swat.SW[1] = 0.1;
            Console.WriteLine(swat.SW[0]);
            var sim = simulation;
            //Console.WriteLine(simulation);
            //Console.WriteLine(soilWater);
            //Console.WriteLine(swat);
            Console.Write(clock.Today + "," + swat.SW[1] + "," + Orig[1] + ",");
            //Console.WriteLine("end");
        }

        [EventSubscribe("DoPhenology")]
        private void changePhenology(object sender, EventArgs e)
        {
            var SW = swat.SW;
            Console.WriteLine(SW[1]);
            //Console.WriteLine(swat);
            //Console.WriteLine("end");
        }
    }
}
namespace Models
{
    using Models.Core;
    using Models.Climate;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Models.Soils;
    using Models.PMF;
    using APSIM.Shared.Utilities;
  

    /// <summary>
    /// Monitor and influence simulation using ZMQ sockets
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class SendReceive : Model
    {
        [Link]
        Clock clock = null;

        //[Link]
        //Weather weather = null;

        //[Link]
        //Soil soil = null;

        [Link]
        private Simulation simulation = null;

        /// <summary>HostName</summary>
        [Description("ZeroMQ host url: ")]
        public String HostURL { get; set; }
        //private String HostURL = "";

        [EventSubscribe("DoManagement")]
        private void DoDailyCalculations(object sender, EventArgs e)
        {
            Console.WriteLine(clock.ToString());
            var sim2 = simulation;
            Console.Write(HostURL);
        }

    }
}

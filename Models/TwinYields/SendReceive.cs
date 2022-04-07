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
    using NetMQ;
    using NetMQ.Sockets;
      
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

        [Link]
        Soil soil = null;

        [Link]
        private Simulation simulation = null;

        [NonSerialized]
        private RequestSocket client;

        /// <summary>HostName</summary>
        [Description("ZeroMQ host url: ")]
        public String HostURL { get; set; }
        //private String HostURL = "";

        [EventSubscribe("StartOfSimulation")]
        private void Connect(object sender, EventArgs e)
        {
            client = new RequestSocket();
            client.Connect("tcp://localhost:5555");
            client.SendFrame("Connected!");
            var message = client.ReceiveFrameString();
            var sim = simulation;
            var s = soil;
            Console.Write(HostURL);
        }


        [EventSubscribe("DoManagement")]
        private void DoDailyCalculations(object sender, EventArgs e)
        {
            //Console.WriteLine(clock.ToString());
            //Console.Write(HostURL);
            client.SendFrame(clock.Today.ToString());
            //client.SendFrame("Daily!");
            var message = client.ReceiveFrameString();
            //Console.WriteLine("Received {0}", message);
        }

    }
}

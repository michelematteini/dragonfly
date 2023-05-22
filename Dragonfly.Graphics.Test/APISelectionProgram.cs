using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.Test
{
    public class APISelectionProgram : IConsoleProgram
    {
        public string ProgramName
        {
            get { return "API Selection"; }
        }

        public void RunProgram()
        {
            Console.WriteLine("Select an API from the list (Currently used API is {0}).", GraphicsAPIs.GetDefault().Description);
            List<IGraphicsAPI> allAPIs = GraphicsAPIs.GetList();
            for (int i = 0; i < allAPIs.Count; i++)
            {
                Console.WriteLine("{0} - {1}", i, allAPIs[i].Description);
            }

            Console.WriteLine();
            Console.WriteLine("Insert the selected API number:");

            string selectedString = Console.ReadLine();
            int selectedID = -1;
            if(int.TryParse(selectedString, out selectedID) && selectedID > 0 && selectedID < allAPIs.Count)
            {
                GraphicsAPIs.SetDefault(allAPIs[selectedID]);
            }
        }
    }
}

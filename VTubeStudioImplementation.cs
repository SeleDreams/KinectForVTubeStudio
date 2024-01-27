using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTS.Core;

namespace LumiosNoctis
{
    internal class VTubeStudioLogger : IVTSLogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string error)
        {
            Log("ERROR : " + error);
        }

        public void LogError(Exception error)
        {
            Log("EXCEPTION : " + error.ToString() + " " + error.Message);
        }

        public void LogWarning(string warning)
        {
            Log("WARNING : " + warning);
        }
    }

    internal class VTubeStudioImplementation : CoreVTSPlugin
    {
        public VTubeStudioImplementation(VTubeStudioLogger logger, int updateIntervalMs, string pluginName, string pluginAuthor, string pluginIcon) : base(logger, updateIntervalMs, pluginName, pluginAuthor, pluginIcon)
        {
        }
    }
}

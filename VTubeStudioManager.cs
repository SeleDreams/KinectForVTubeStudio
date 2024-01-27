using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTS;
using VTS.Core;

namespace LumiosNoctis
{

    public enum VTubeStudioParameters
    {
        FacePositionX,
        FacePositionY,
        FacePositionZ,
        FaceAngleX,
        FaceAngleY,
        FaceAngleZ,
        MouthOpen,
        MouthSmile,
        Brows,
        CheekPuff,
        EyeOpenLeft,
        EyeOpenRight
    }

    public class VTubeStudioManager
    {
        VTubeStudioImplementation vTubeStudio;
        VTubeStudioLogger logger;
        Dictionary<VTubeStudioParameters, float> vtubeStudioValues;

        public VTubeStudioManager()
        {
            logger = new VTubeStudioLogger();
            vTubeStudio = new VTubeStudioImplementation(logger, 1, "Kinect for VTube Studio", "Lumios Noctis", "");
            vtubeStudioValues = new Dictionary<VTubeStudioParameters, float>();
        }

        public void SetVtubeStudioParam(VTubeStudioParameters param,float value)
        {
            vtubeStudioValues[param] = value;
        }

        public async Task Initialize()
        {
            try
            {
                await vTubeStudio.InitializeAsync(new WebSocketImpl(logger), new NewtonsoftJsonUtilityImpl(), new TokenStorageImpl(""), () => logger.LogWarning("Disconnected !"));
                logger.Log("Connected !");
                var apiState = await vTubeStudio.GetAPIState();
                logger.Log($"VTube Studio version : {apiState.data.vTubeStudioVersion}");
                var currentModel = await vTubeStudio.GetCurrentModel();
                logger.Log($"Model name is : {currentModel.data.modelName}");
                var inputParameterList = await vTubeStudio.GetInputParameterList();
                VTSParameter[] parameters = inputParameterList.data.defaultParameters;
                foreach (var parameter in parameters)
                {
                    logger.Log(parameter.name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        public async Task SendTrackingParameter()
        {
            List<VTSParameterInjectionValue> values = new List<VTSParameterInjectionValue>();
            
            foreach (var trackingParam in vtubeStudioValues)
            {
                VTSParameterInjectionValue param = new VTSParameterInjectionValue();
                param.id = trackingParam.Key.ToString();
                param.value = trackingParam.Value;
                values.Add(param);
            }
            if (values.Count == 0)
            {
                return;
            }
            await vTubeStudio.InjectParameterValues(values.ToArray(),VTSInjectParameterMode.ADD,true);
        }
    }
}

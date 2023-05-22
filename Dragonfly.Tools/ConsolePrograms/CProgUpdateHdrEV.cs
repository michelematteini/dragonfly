using Dragonfly.BaseModule;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Tools
{
    public class CProgUpdateHdrEV : IConsoleProgram
    {
        private const int SAMPLE_COUNT = 256;

        public string ProgramName => "Change *.hdr file EV";

        public void RunProgram()
        {
            // retrieve input-output file locations
            string inHdrFilePath, outHdrFilePath;
            if (!ConsoleUtils.AskInOutPath(".hdr", ".hdr", out inHdrFilePath, out outHdrFilePath))
                return;

            // load hdr file
            HdrFile image = new HdrFile(inHdrFilePath);
            float[] rgbData = new float[image.Header.Width * image.Header.Height * 3];
            image.CopyHdrDataTo(rgbData);

            // calculate the average ev of the image
            float imageEv = 0;
            {
                Float3 avgColor = Float3.Zero;
                int w = image.Header.Width, h = image.Header.Height;
                int step = rgbData.Length / (9 * SAMPLE_COUNT) * 3;
                for (int i = 0; i < rgbData.Length; i+= step)
                {
                    avgColor += new Float3(rgbData[i], rgbData[i + 1], rgbData[i + 2]);
                }
                avgColor /= SAMPLE_COUNT;
                imageEv = ExposureHelper.LuxToEV(Color.GetLuminanceFromRGB(avgColor));
            }

            if (!ConsoleUtils.AskYesNo($"The image exposure is at about EV {imageEv}, do you want to change it?"))
                return;

            float newEV = ConsoleUtils.AskFloat($"Insert the wanted EV:", imageEv);

            // update the data exposure
            float linearMul = ExposureHelper.EVToLux(newEV - imageEv);
            for (int i = 0; i < rgbData.Length; i++)
                rgbData[i] *= linearMul;

            // create a new image with the new data and save it
            HdrFile newImage = new HdrFile(image.Header.Width, image.Header.Height);
            newImage.SetHdrData(rgbData);
            newImage.Save(outHdrFilePath);
        }
    }
}

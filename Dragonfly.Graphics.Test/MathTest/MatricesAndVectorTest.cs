using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Graphics.Test
{
    public class MatricesAndVectorTest : IConsoleProgram
    {
        public string ProgramName => "Matrices and vectors test.";

        public void RunProgram()
        {
            Float4x4 scale = Float4x4.Scale(1, 2, 3);
            PrintMatrix("scale", scale);

            Float4x4 rotation = Float4x4.RotationY(FMath.PI);
            PrintMatrix("rotation Y 180 deg.", rotation);

            Float4x4 translation = Float4x4.Translation(100, 200, 300);
            PrintMatrix("translate to (100, 200, 300)", translation);

            Float4x4 model = scale * rotation * translation;
            PrintMatrix("model = scale * rotation * translation", model);

            Float4x4 lookAt = Float4x4.LookAt(new Float3(10, 20, 30), new Float3(0, 0, 0), Float3.UnitY);
            PrintMatrix("look from (10, 20, 30)  to origin", lookAt);

            Float4x4 ortho = Float4x4.Orthographic(640, 480, 1, 100);
            PrintMatrix("orthographic 640x480 from z=1 to 100", ortho);

            Int2 screenRes = new Int2(640, 480);
            Console.WriteLine("Expected pixelSize on 640x480 res is (1, 1)");
            Console.WriteLine("Pixel size:" + ortho.PixelSizeAt(screenRes));

            Float4x4 transfOrtho = lookAt * ortho;
            PrintMatrix("lookAt * ortho", transfOrtho);
            Console.WriteLine("Pixel size:" + transfOrtho.PixelSizeAt(screenRes));

            Float4x4 persp = Float4x4.Perspective(FMath.PI_OVER_2, 1, 1, 100);
            PrintMatrix("perspective fovy of 90 deg, from z=1 to 100", persp);
            Console.WriteLine("Pixel size:" + persp.PixelSizeAt(screenRes));

            Float4x4 transfPersp = lookAt * persp;
            PrintMatrix("lookAt * perspective", transfPersp);
            Console.WriteLine("Pixel size:" + transfPersp.PixelSizeAt(screenRes));
        }

        private void PrintMatrix(string name, Float4x4 m)
        {
            Console.WriteLine(string.Format("matrix \"{0}\" = ", name));
            Console.WriteLine(m.GetRow(0));
            Console.WriteLine(m.GetRow(1));
            Console.WriteLine(m.GetRow(2));
            Console.WriteLine(m.GetRow(3));
            Console.WriteLine();
        }
    }

}

using Dragonfly.Graphics.Test.ResourceAllocTest;
using Dragonfly.Utils;

namespace Dragonfly.Graphics.Test
{
    class Program
    {
        private static void Main(string[] args)
        {
            ConsoleSelectionLoop selectionLoop = new ConsoleSelectionLoop("Dragonfly.Graphics tests.");
            selectionLoop.AddProgram(new APISelectionProgram());
            selectionLoop.AddProgram(new FrmClearBlueTest());
            selectionLoop.AddProgram(new FrmTriangleTest() { ApplyTexture = false });
            selectionLoop.AddProgram(new FrmTriangleTest() { ApplyTexture = true });
            selectionLoop.AddProgram(new FrmAllocationTest());
            selectionLoop.AddProgram(new FrmInstancingTest());
            selectionLoop.AddProgram(new MatricesAndVectorTest());

            selectionLoop.Start();
        }

    }
}

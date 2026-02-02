using Engine3D;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Editor3D
{
    public class Editor
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("Editor3D: start");
            Vector2i windowSize = new Vector2i(1280, 768);

            Console.WriteLine("Editor3D: creating Engine");
            Engine engine = new Engine(windowSize.X, windowSize.Y);
            Console.WriteLine("Editor3D: engine created");
            ImGuiController imGuiController = new ImGuiController(windowSize.X, windowSize.Y, ref engine);
            Console.WriteLine("Editor3D: ImGuiController created");
            engine.AddOnLoadMethod(imGuiController.OnLoad);
            Console.WriteLine("Editor3D: running engine");
            engine.Run();
        }

        
    }
}

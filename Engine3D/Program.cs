﻿namespace Engine3D
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using(Engine engine = new Engine(1280,768))
            {
                engine.Run();
            }
        }
    }
}
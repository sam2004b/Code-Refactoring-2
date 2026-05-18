using System;
using System.Linq;
using System.Threading;

namespace LifeSim;

public static class Program
{
    private const int Width = 50;
    private const int Height = 22;
    private const int InitialHerbivores = 28;
    private const int InitialPredators = 10;
    private const int DelayMs = 120;

    public static void Main()
    {
        ConfigureConsole();

        var world = CreateWorld();

        RunSimulation(world);
    }

    private static void ConfigureConsole()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
    }

    private static World CreateWorld()
    {
        var world = new World(Width, Height);

        var initialPlants = (int)(Width * Height * 0.22);

        world.Seed<Plant>(initialPlants);
        world.Seed<Herbivore>(InitialHerbivores);
        world.Seed<Predator>(InitialPredators);

        return world;
    }

    private static void RunSimulation(World world)
    {
        var paused = false;

        while (true)
        {
            HandleInput(ref paused);

            if (!paused)
            {
                world.Step();
                RenderWorld(world);
            }

            Thread.Sleep(DelayMs);
        }
    }

    private static void HandleInput(ref bool paused)
    {
        while (!Console.IsInputRedirected && Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
            {
                ExitApplication();
            }

            if (key == ConsoleKey.Spacebar || key == ConsoleKey.P)
            {
                paused = !paused;
            }
        }
    }

    private static void ExitApplication()
    {
        Console.ResetColor();
        Console.CursorVisible = true;
        Environment.Exit(0);
    }

    private static void RenderWorld(World world)
    {
        Console.SetCursorPosition(0, 0);

        var plants = world.All.OfType<Plant>().Count();
        var herbs = world.All.OfType<Herbivore>().Count();
        var preds = world.All.OfType<Predator>().Count();

        Console.ResetColor();

        Console.WriteLine(
            $"Tick: {world.Tick,-8}  Plants: {plants,-5}  Herbivores: {herbs,-5}  Predators: {preds,-5}   [Space/P] pause, [Q/Esc] quit");

        var snapshot = world.GridSnapshot();

        for (var y = 0; y < world.Height; y++)
        {
            for (var x = 0; x < world.Width; x++)
            {
                if (snapshot.TryGetValue(new Point2(x, y), out var organism))
                {
                    organism.ApplyColor();
                    Console.Write(organism.Glyph);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(' ');
                }
            }

            Console.WriteLine();
        }
    }
}
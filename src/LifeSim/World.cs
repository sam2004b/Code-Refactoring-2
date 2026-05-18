using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeSim;

public class World
{
    private readonly Dictionary<Point2, Organism> _grid = new();
    private readonly List<Organism> _organisms = new();

    public World(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public int Tick { get; private set; }

    public IEnumerable<Organism> All => _organisms.Where(o => o.IsAlive);

    public void Add(Organism org)
    {
        if (_grid.ContainsKey(org.Pos))
        {
            return;
        }

        _organisms.Add(org);
        _grid[org.Pos] = org;
    }

    public void Remove(Organism org)
    {
        if (!org.IsAlive)
        {
            return;
        }

        org.IsAlive = false;
        _grid.Remove(org.Pos);
    }

    public void MoveTo(Organism org, Point2 newPos)
    {
        if (!org.IsAlive)
        {
            return;
        }

        var wrappedPos = Wrap(newPos);

        if (_grid.ContainsKey(wrappedPos))
        {
            return;
        }

        _grid.Remove(org.Pos);
        org.Pos = wrappedPos;
        _grid[wrappedPos] = org;
    }

    public bool IsEmpty(Point2 p) => !_grid.ContainsKey(Wrap(p));

    public Point2 Wrap(Point2 p)
    {
        var x = ((p.X % Width) + Width) % Width;
        var y = ((p.Y % Height) + Height) % Height;
        return new Point2(x, y);
    }

    public void Step()
    {
        Tick++;

        var snapshot = All
        .OrderBy(_ => Rand.Next(0, int.MaxValue))
        .ToList();

        foreach (var organism in snapshot)
        {
            if (organism.IsAlive)
            {
                organism.Tick();
            }
        }

        _organisms.RemoveAll(o => !o.IsAlive);
    }

    public IEnumerable<Point2> Neighbors8(Point2 p)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx != 0 || dy != 0)
                {
                    yield return Wrap(new Point2(p.X + dx, p.Y + dy));
                }
            }
        }
    }

    public IEnumerable<Point2> EmptyNeighbors8(Point2 p)
    {
        foreach (var neighbour in Neighbors8(p))
        {
            if (IsEmpty(neighbour))
            {
                yield return neighbour;
            }
        }
    }

    public void Seed<T>(int count)
        where T : Organism
    {
        for (var i = 0; i < count; i++)
        {
            var emptyCell = RandomEmptyCell();
            
            if (emptyCell == null)
            {
                break;
            }

            var organism = CreateOrganism<T>(emptyCell.Value);
            Add(organism);
        }
    }

      private Organism CreateOrganism<T>(Point2 position)
        where T : Organism
    {
       return typeof(T).Name switch
       {
             nameof(Plant) => new Plant(this, position),
            nameof(Herbivore) => new Herbivore(this, position),
            nameof(Predator) => new Predator(this, position),
            _ => throw new NotSupportedException($"Unknown organism type: {typeof(T).Name}")
        };
    }
         public Point2? RandomEmptyCell()
    {
        var randomCell = TryFindRandomEmptyCell();

        if (randomCell != null)
        {
            return randomCell;
        }

        return FindEmptyCellFromGrid();
    }

    private Point2? TryFindRandomEmptyCell()
    {
        for (var i = 0; i < 500; i++)
        {
            var point = new Point2(
                Rand.Next(0, Width),
                Rand.Next(0, Height));

            if (IsEmpty(point))
            {
                return point;
            }
        }

        return null;
    }

    private Point2? FindEmptyCellFromGrid()
    {
        var emptyCells = new List<Point2>();

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var point = new Point2(x, y);

                if (IsEmpty(point))
                {
                    emptyCells.Add(point);
                }
            }
        }

        return emptyCells.Count == 0
            ? null
            : emptyCells.Pick();
    }

    public Organism? FindNearest<T>(Point2 from, int visionRange)
        where T : Organism
    {
        Organism? nearestOrganism = null;
        var shortestDistance = int.MaxValue;

        foreach (var organism in All)
        {
            if (organism is not T)
            {
                continue;
            }

            var distance = CalculateDistance(from, organism.Pos);

            if (distance <= visionRange && distance < shortestDistance)
            {
                nearestOrganism = organism;
                shortestDistance = distance;
            }
        }

        return nearestOrganism;
    }

    private int CalculateDistance(Point2 from, Point2 to)
    {
        var dx = ToroidalDistance(from.X, to.X, Width);
        var dy = ToroidalDistance(from.Y, to.Y, Height);

        return dx + dy;
    }

    public string SerializeWorldSnapshot()
    {
        var items = All.Select(o => $"{o.GetType().Name}@{o.Pos.X},{o.Pos.Y}");

        return $"Tick={Tick} | {string.Join(";", items)}";
    }

    public IReadOnlyDictionary<Point2, Organism> GridSnapshot() =>
        new Dictionary<Point2, Organism>(_grid);

    private static int ToroidalDistance(int a, int b, int size)
    {
        var diff = Math.Abs(a - b);

        return Math.Min(diff, size - diff);
    }
}
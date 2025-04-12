using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Runtime.CompilerServices;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool _isAliveNext;
        public bool IsAliveNext => _isAliveNext;
        public int X;
        public int Y;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                _isAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                _isAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = _isAliveNext;
        }
    }
    public class Board
    {
        public Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }
        public int Generation { get; private set; }
        private readonly Queue<int> LiveCountHistory = new();
        private int StabilityPeriod = 10;

        public Board(int width, int height, int cellSize, double liveDensity = .1, bool isLoad = false)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell { X = x, Y = y };



            ConnectNeighbors();
            if (!isLoad) Randomize(liveDensity);

        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
            UpdateStabilityTracking();
            Generation++;
        }
        public void SaveBoard(string filePath)
        {
            var projectdir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            filePath = Path.Combine(projectdir, filePath);
            var lines = new List<string>();
            for (int y = 0; y < Rows; y++)
            {
                var sb = new StringBuilder();
                for (int x = 0; x < Columns; x++)
                    sb.Append(Cells[y, x].IsAlive ? '1' : '0');
                lines.Add(sb.ToString());
            }
            File.WriteAllLines(filePath, lines);
        }

        public void LoadBoard(string filePath)
        {
            var projectdir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            filePath = Path.Combine(projectdir, filePath);
            var lines = File.ReadAllLines(filePath);
            for (int y = 0; y < Rows && y < lines.Length; y++)
            {
                for (int x = 0; x < Columns && x < lines[y].Length; x++)
                {
                    Cells[y, x].IsAlive = lines[y][x] == '1';
                }

            }
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public List<List<Cell>> FindComponents()
        {
            var visited = new bool[Columns, Rows];
            var components = new List<List<Cell>>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (!visited[x, y] && Cells[x, y].IsAlive)
                    {
                        var component = new List<Cell>();
                        FloodFill(x, y, visited, component);
                        components.Add(component);
                    }
                }
            }
            return components;
        }

        private void FloodFill(int x, int y, bool[,] visited, List<Cell> component)
        {
            var stack = new Stack<(int, int)>();
            stack.Push((x, y));

            while (stack.Count > 0)
            {
                var (cx, cy) = stack.Pop();
                if (cx < 0 || cx >= Columns || cy < 0 || cy >= Rows) continue;
                if (visited[cx, cy] || !Cells[cx, cy].IsAlive) continue;

                visited[cx, cy] = true;
                component.Add(Cells[cx, cy]);

                foreach (var neighbor in Cells[cx, cy].neighbors)
                    stack.Push((neighbor.X, neighbor.Y));
            }
        }
        private void UpdateStabilityTracking()
        {
            int liveCount = Cells.Cast<Cell>().Count(c => c.IsAlive);
            LiveCountHistory.Enqueue(liveCount);
            if (LiveCountHistory.Count > StabilityPeriod)
                LiveCountHistory.Dequeue();
        }

        public bool IsStable()
        {
            if (LiveCountHistory.Count < StabilityPeriod)
                return false;
            bool isStatic = LiveCountHistory.All(c => c == LiveCountHistory.First());
            return isStatic;
        }

        public void ResetStabilityTracking()
        {
            LiveCountHistory.Clear();
            Generation = 0;
        }
    }
    public static class PatternRecognizer
    {
        private static readonly Dictionary<string, HashSet<HashSet<(int, int)>>> _patterns = new();

        public static void LoadPatterns(string directory)
        {
            var projectdir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            directory = Path.Combine(projectdir, directory);
            foreach (var file in Directory.GetFiles(directory, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var pattern = ReadPattern(file);
                AddPatternVariants(name, pattern);
            }
        }

        private static HashSet<(int, int)> ReadPattern(string path)
        {
            var lines = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            var cells = new HashSet<(int, int)>();
            for (int y = 0; y < lines.Length; y++)
                for (int x = 0; x < lines[y].Length; x++)
                    if (lines[y][x] == '1') cells.Add((x, y));

            return cells;
        }

        private static void AddPatternVariants(string name, HashSet<(int, int)> pattern)
        {
            var variants = new HashSet<HashSet<(int, int)>>();
            var current = pattern;

            for (int i = 0; i < 4; i++)
            {
                variants.Add(Normalize(current));
                variants.Add(Normalize(Reflect(current)));
                current = Rotate90(current);
            }

            _patterns[name] = variants;
        }

        private static HashSet<(int, int)> Rotate90(HashSet<(int, int)> pattern) =>
            new(pattern.Select(p => (p.Item2, -p.Item1)));

        private static HashSet<(int, int)> Reflect(HashSet<(int, int)> pattern) =>
            new(pattern.Select(p => (-p.Item1, p.Item2)));

        private static HashSet<(int, int)> Normalize(HashSet<(int, int)> pattern)
        {
            var minX = pattern.Min(p => p.Item1);
            var minY = pattern.Min(p => p.Item2);
            return new(pattern.Select(p => (p.Item1 - minX, p.Item2 - minY)));
        }

        public static string Classify(List<Cell> component)
        {
            var coords = component.Select(c => (c.X, c.Y)).ToHashSet();
            var normalized = Normalize(coords);

            foreach (var (name, variants) in _patterns)
                if (variants.Any(v => v.SetEquals(normalized)))
                    return name;

            return "Unknown";
        }
    }

    [System.Serializable]
    public class Settings
    {

        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }


        public Settings(Settings settings)
        {
            Width = settings.Width;
            Height = settings.Height;
            CellSize = settings.CellSize;
            LiveDensity = settings.LiveDensity;
        }
        [JsonConstructor]
        public Settings(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            LiveDensity = liveDensity;
        }


    }

    class Program
    {
        static Board board;
        static private void Reset(bool isLoad, string filePath = "")
        {
            Settings settings = new Settings(LoadSettings());
            board = new Board(
                settings.Width,
                settings.Height,
                settings.CellSize,
                settings.LiveDensity,
                isLoad);
            if (isLoad) board.LoadBoard(filePath);
        }
        static void FigureFinder()
        {
            PatternRecognizer.LoadPatterns("figures");
            var components = board.FindComponents();
            foreach (var component in components)
            {
                var type = PatternRecognizer.Classify(component);
                var firstCell = component.First();
                Console.WriteLine($"Тип: {type,-10} | Размер: {component.Count,-3} | Позиция: ({firstCell.X},{firstCell.Y})");
            }
        }
        static void Render()
        {
            Console.WriteLine($"Поколение: {board.Generation}");
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static private Settings LoadSettings(string path = "system_parameters.json")
        {
            var projectdir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            path = Path.Combine(projectdir, path);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            return new Settings(50, 20, 1, 0.5);
        }
        static public void StartGame(bool isLoad, string filePath = "start_field.txt")
        {
            Reset(isLoad, filePath);
            while (true)
            {
                Console.Clear();
                if (board.IsStable())
                {
                    Console.WriteLine($"Система стабилизировалась на поколении {board.Generation}");
                    Render();
                    return;
                }
                Render();
                board.Advance();
                Thread.Sleep(1000);
            }
        }
        static public void StablilityAnalysis()
        {
            string filePath = "data_for_graph.txt";
            var projectdir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            filePath = Path.Combine(projectdir, filePath);
            string content = "";
            var allGener = 0;
            for (decimal k = 0.1m; k < 1.0m; k += 0.05m)
            {
                Settings settings = new Settings(LoadSettings("parameters_for_graph.json"));
                board = new Board(
                    settings.Width,
                    settings.Height,
                    settings.CellSize,
                    (double)k,
                    false);
                for (int i = 0; i < 10000; i++)
                {
                    if (board.IsStable())
                    {
                        content += k.ToString() + " " + board.Generation.ToString() + "\n";
                        allGener += board.Generation;
                        break;
                    }
                    board.Advance();
                }

            }
            File.WriteAllText(filePath, content);
            Console.WriteLine((double)allGener / 10);
        }
        static void Main(string[] args)
        {
            StartGame(true);
            StablilityAnalysis();


        }
    }
}
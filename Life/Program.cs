using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading;
using ScottPlot;


namespace cli_life
{
    public class SimulationSettings
    {
        public int Width { get; set; } = 80;
        public int Height { get; set; } = 40;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.3;
        public int DelayMs { get; set; } = 200;
    }

    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;

        public Board(int width, int height, int cellSize, double liveDensity = .1, bool randomize = true)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            if (randomize) Randomize(liveDensity);
        }

        private readonly Random rand = new Random();
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

        public int CountAlive()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public void SaveToFile(string filename)
        {
            using var writer = new StreamWriter(filename);
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                    writer.Write(Cells[x, y].IsAlive ? '1' : '0');
                writer.WriteLine();
            }
        }

        public static Board LoadFromFile(string filename, int cellSize, int width = -1, int height = -1)
        {
            var lines = File.ReadAllLines(filename);
            int h = lines.Length;
            int w = lines[0].Length;
            if (width == -1) width = w;
            if (height == -1) height = h;
            var board = new Board(width, height, cellSize, randomize: false);
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    board.Cells[x, y].IsAlive = false;
            for (int y = 0; y < h && y < board.Rows; y++)
                for (int x = 0; x < w && x < board.Columns; x++)
                    board.Cells[x, y].IsAlive = lines[y][x] == '1';
            return board;
        }

        public void PlaceFigure(int left, int top, string[] pattern)
        {
            for (int row = 0; row < pattern.Length; row++)
                for (int col = 0; col < pattern[row].Length; col++)
                {
                    int x = left + col;
                    int y = top + row;
                    if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                        Cells[x, y].IsAlive = pattern[row][col] == '1';
                }
        }
    }

    public class Program
    {
        static Board board;
        static SimulationSettings settings;

        static SimulationSettings LoadSettings(string path = "settings.json")
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SimulationSettings>(json) ?? new SimulationSettings();
            }
            return new SimulationSettings();
        }

        static void Reset()
        {
            board = new Board(
                width: settings.Width,
                height: settings.Height,
                cellSize: settings.CellSize,
                liveDensity: settings.LiveDensity);
        }

        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    Console.Write(board.Cells[col, row].IsAlive ? '*' : ' ');
                }
                Console.WriteLine();
            }
        }

        static void LoadFigure(string figureName)
        {
            string path = $"Figures/{figureName}.txt";
            if (!File.Exists(path))
            {
                Console.WriteLine($"Figure {figureName} not found.");
                Reset();
                return;
            }
            var pattern = File.ReadAllLines(path);
            board = new Board(settings.Width, settings.Height, settings.CellSize, randomize: false);
            int left = (settings.Width / settings.CellSize - pattern[0].Length) / 2;
            int top = (settings.Height / settings.CellSize - pattern.Length) / 2;
            board.PlaceFigure(left, top, pattern);
        }

        // ===== Методы для исследования и тестов =====
        public static List<List<(int x, int y)>> GetConnectedComponents(Board board)
        {
            var visited = new bool[board.Columns, board.Rows];
            var components = new List<List<(int, int)>>();
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                {
                    if (board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var comp = new List<(int, int)>();
                        var queue = new Queue<(int, int)>();
                        queue.Enqueue((x, y));
                        visited[x, y] = true;
                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();
                            comp.Add((cx, cy));
                            for (int dx = -1; dx <= 1; dx++)
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = cx + dx;
                                    int ny = cy + dy;
                                    if (nx >= 0 && nx < board.Columns && ny >= 0 && ny < board.Rows &&
                                        board.Cells[nx, ny].IsAlive && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                        }
                        components.Add(comp);
                    }
                }
            return components;
        }

        public static string ClassifyComponent(List<(int x, int y)> cells, Board board)
        {
            if (cells.Count == 0) return "Empty";
            int minX = cells.Min(c => c.x);
            int minY = cells.Min(c => c.y);
            var normalized = cells.Select(c => (c.x - minX, c.y - minY)).OrderBy(c => c.Item1).ThenBy(c => c.Item2).ToList();
        
            foreach (var pattern in _patterns)
            {
                if (normalized.Count != pattern.Value.Count) continue;
                bool match = true;
                foreach (var p in pattern.Value)
                    if (!normalized.Contains(p)) { match = false; break; }
                if (match) return pattern.Key;
            }
            return "Other";
        }

        private static readonly Dictionary<string, List<(int, int)>> _patterns = new()
        {
            ["Block"] = new List<(int, int)> { (0,0), (1,0), (0,1), (1,1) },
            ["Beehive"] = new List<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (2,2) },
            ["Loaf"] = new List<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (3,2), (2,3) },
            ["Boat"] = new List<(int, int)> { (0,0), (1,0), (2,1), (0,1), (1,2) },
            ["Tub"] = new List<(int, int)> { (1,0), (0,1), (2,1), (1,2) }
        };

        public static int GetStableGenerations(Board startBoard, int maxGenerations = 10000, int stableWindow = 5)
        {
            var copy = new Board(startBoard.Columns, startBoard.Rows, startBoard.CellSize, randomize: false);
            for (int x = 0; x < startBoard.Columns; x++)
                for (int y = 0; y < startBoard.Rows; y++)
                    copy.Cells[x, y].IsAlive = startBoard.Cells[x, y].IsAlive;

            int lastAlive = copy.CountAlive();
            int stableCount = 0;
            for (int gen = 0; gen < maxGenerations; gen++)
            {
                copy.Advance();
                int alive = copy.CountAlive();
                if (alive == lastAlive)
                    stableCount++;
                else
                    stableCount = 0;
                lastAlive = alive;
                if (stableCount >= stableWindow)
                    return gen + 1 - stableWindow;
            }
            return maxGenerations;
        }

        public static void RunExperiments()
        {
            double[] densities = Enumerable.Range(5, 19).Select(i => i / 100.0).ToArray();
            int attempts = 10;
            var results = new List<(double density, double avgGen)>();

            foreach (var density in densities)
            {
                int totalGen = 0;
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    var board = new Board(80, 40, 1, density);
                    int stableGen = GetStableGenerations(board);
                    totalGen += stableGen;
                }
                double avg = totalGen / (double)attempts;
                results.Add((density, avg));
                Console.WriteLine($"Density {density:F2}: average stable generation = {avg:F2}");
            }

            Directory.CreateDirectory("Data");
            using (var writer = new StreamWriter("Data/data.txt"))
            {
                writer.WriteLine("Density;AverageGenerationsToStable");
                foreach (var r in results)
                    writer.WriteLine($"{r.density:F2};{r.avgGen:F2}");
            }

            var plt = new ScottPlot.Plot(800, 600);
            double[] xs = results.Select(r => r.density).ToArray();
            double[] ys = results.Select(r => r.avgGen).ToArray();
            plt.AddScatter(xs, ys);
            plt.Title("Stabilization time vs initial density");
            plt.XLabel("Initial live density");
            plt.YLabel("Generations to stable phase");
            plt.SaveFig("Data/plot.png");
            Console.WriteLine("Graph saved to Data/plot.png");
        }

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--experiment")
            {
                RunExperiments();
                return;
            }

            settings = LoadSettings();

            if (args.Length >= 2 && args[0] == "--load")
            {
                string filename = args[1];
                if (File.Exists(filename))
                    board = Board.LoadFromFile(filename, settings.CellSize, settings.Width, settings.Height);
                else
                {
                    Console.WriteLine($"File {filename} not found. Starting with random board.");
                    Reset();
                }
            }
            else if (args.Length >= 2 && args[0] == "--figure")
            {
                LoadFigure(args[1]);
            }
            else
            {
                Reset();
            }

            while (true)
            {
                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(settings.DelayMs);
            }
        }
    }
}

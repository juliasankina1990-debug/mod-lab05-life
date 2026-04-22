using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System.IO;
using System.Linq;

namespace Life.Tests
{
    [TestClass]
    public class GameOfLifeTests
    {
        [TestMethod]
        public void Test_NewCell_IsDeadByDefault()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Test_Advance_AliveCellWith2Neighbors_StaysAlive()
        {
            var board = new Board(3, 3, 1, randomize: false);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void Test_Advance_AliveCellWith1Neighbor_Dies()
        {
            var board = new Board(3, 3, 1, randomize: false);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void Test_Advance_DeadCellWith3Neighbors_BecomesAlive()
        {
            var board = new Board(3, 3, 1, randomize: false);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod]
        public void Test_Block_IsStable()
        {
            var board = new Board(4, 4, 1, randomize: false);
            string[] block = { "11", "11" };
            board.PlaceFigure(1, 1, block);
            int aliveBefore = board.CountAlive();
            board.Advance();
            int aliveAfter = board.CountAlive();
            Assert.AreEqual(aliveBefore, aliveAfter);
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
            Assert.IsTrue(board.Cells[1, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Test_Blinker_ChangesEveryGeneration()
        {
            var board = new Board(10, 10, 1, randomize: false);
            board.Cells[4, 4].IsAlive = true;
            board.Cells[5, 4].IsAlive = true;
            board.Cells[6, 4].IsAlive = true;
            Assert.AreEqual(3, board.CountAlive());
            board.Advance();
            Assert.AreEqual(3, board.CountAlive());
        }


		[TestMethod]
		public void Test_ClassifyComponent_IdentifiesBeehive()
		{
		    var board = new Board(10, 10, 1, randomize: false);
		    string[] beehive = { "0110", "1001", "0110" };
		    board.PlaceFigure(3, 3, beehive);
		    var components = Program.GetConnectedComponents(board);
		    var classification = Program.ClassifyComponent(components[0], board);
		    Assert.AreEqual("Beehive", classification);
		}


        [TestMethod]
        public void Test_Glider_Moves()
        {
            var board = new Board(10, 10, 1, randomize: false);
            string[] glider = { "010", "001", "111" };
            board.PlaceFigure(1, 1, glider);
            int[] aliveCounts = new int[5];
            for (int i = 0; i < 5; i++)
            {
                aliveCounts[i] = board.CountAlive();
                board.Advance();
            }
            Assert.AreEqual(5, aliveCounts[0]);
            Assert.AreEqual(5, aliveCounts[1]);
            Assert.AreEqual(5, aliveCounts[4]);
        }

        [TestMethod]
        public void Test_SaveAndLoad()
        {
            var board = new Board(10, 10, 1, randomize: false);
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            string tempFile = Path.GetTempFileName();
            board.SaveToFile(tempFile);
            var loaded = Board.LoadFromFile(tempFile, 1, 10, 10);
            Assert.IsTrue(loaded.Cells[2, 2].IsAlive);
            Assert.IsTrue(loaded.Cells[2, 3].IsAlive);
            File.Delete(tempFile);
        }

        [TestMethod]
        public void Test_CountAlive_ReturnsCorrectNumber()
        {
            var board = new Board(5, 5, 1, randomize: false);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Cells[4, 4].IsAlive = true;
            Assert.AreEqual(3, board.CountAlive());
        }

        [TestMethod]
        public void Test_PlaceFigure_CorrectlyPlaces()
        {
            var board = new Board(10, 10, 1, randomize: false);
            string[] pattern = { "101", "010" };
            board.PlaceFigure(2, 3, pattern);
            Assert.IsTrue(board.Cells[2, 3].IsAlive);
            Assert.IsFalse(board.Cells[3, 3].IsAlive);
            Assert.IsTrue(board.Cells[4, 3].IsAlive);
            Assert.IsFalse(board.Cells[2, 4].IsAlive);
            Assert.IsTrue(board.Cells[3, 4].IsAlive);
            Assert.IsFalse(board.Cells[4, 4].IsAlive);
        }

        [TestMethod]
        public void Test_GetConnectedComponents_ReturnsCorrectCount()
        {
            var board = new Board(5, 5, 1, randomize: false);
            board.PlaceFigure(0, 0, new[] { "11", "11" });
            board.PlaceFigure(3, 3, new[] { "11", "11" });
            var components = Program.GetConnectedComponents(board);
            Assert.AreEqual(2, components.Count);
        }

        [TestMethod]
        public void Test_StableGenerations_ForBlock_ReturnsZero()
        {
            var board = new Board(5, 5, 1, randomize: false);
            board.PlaceFigure(1, 1, new[] { "11", "11" });
            int stableGen = Program.GetStableGenerations(board);
            Assert.AreEqual(0, stableGen);
        }

        [TestMethod]
        public void Test_ClassifyComponent_IdentifiesBlock()
        {
            var board = new Board(5, 5, 1, randomize: false);
            board.PlaceFigure(1, 1, new[] { "11", "11" });
            var components = Program.GetConnectedComponents(board);
            var classification = Program.ClassifyComponent(components[0], board);
            Assert.AreEqual("Block", classification);
        }

        [TestMethod]
        public void Test_Randomize_Density()
        {
            var board = new Board(100, 100, 1, 0.5);
            int alive = board.CountAlive();
            double actualDensity = alive / 10000.0;
            Assert.IsTrue(actualDensity > 0.45 && actualDensity < 0.55);
        }

        [TestMethod]
        public void Test_BoardConstructor_WithRandomizeFalse_AllDead()
        {
            var board = new Board(10, 10, 1, randomize: false);
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    Assert.IsFalse(board.Cells[x, y].IsAlive);
        }

        [TestMethod]
        public void Test_LoadFromFile_WithDifferentSize()
        {
            File.WriteAllText("test3x3.txt", "101\n010\n101");
            var board = Board.LoadFromFile("test3x3.txt", 1, 5, 5);
            Assert.IsTrue(board.Cells[0, 0].IsAlive);
            Assert.IsFalse(board.Cells[1, 0].IsAlive);
            Assert.IsTrue(board.Cells[2, 0].IsAlive);
            Assert.IsFalse(board.Cells[3, 0].IsAlive);
            File.Delete("test3x3.txt");
        }
    }
}

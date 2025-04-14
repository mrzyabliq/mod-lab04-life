using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cli_life.Tests
{
    [TestClass]
    public class AllGameOfLifeTests
    {
        private string _testDataPath;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Setup()
        {
            _testDataPath = "TestData";
        }

        // =====================
        // Тесты для класса Cell
        // =====================
        [TestMethod]
        public void T01_Cell_InitialState_IsDead()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void T02_Cell_AliveWith0Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.DetermineNextLiveState();
            Assert.IsFalse(cell.IsAliveNext);
        }

        [TestMethod]
        public void T03_Cell_AliveWith2Neighbors_Survives()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(CreateAliveCells(2));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        [TestMethod]
        public void T04_Cell_DeadWith3Neighbors_BecomesAlive()
        {
            var cell = new Cell();
            cell.neighbors.AddRange(CreateAliveCells(3));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAliveNext);
        }

        // ======================
        // Тесты для класса Board
        // ======================
        [TestMethod]
        public void T05_Board_CorrectDimensions()
        {
            var board = new Board(50, 20, 1, 0.1, false);
            Assert.AreEqual(50, board.Columns);
            Assert.AreEqual(20, board.Rows);
        }

        [TestMethod]
        public void T06_Board_AllCellsHave8Neighbors()
        {
            var board = new Board(10, 10, 1, 0.1, false);
            foreach (var cell in board.Cells)
                Assert.AreEqual(8, cell.neighbors.Count);
        }

        [TestMethod]
        public void T07_Board_LoadState_Correctly()
        {
            var board = new Board(3, 3, 1, 0, false);
            board.LoadBoard(Path.Combine(_testDataPath, "glider.txt"));
            
            var expected = new[,] {
                {false, true, false},
                {false, false, true},
                {true, true, true}
            };

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    Assert.AreEqual(expected[x,y], board.Cells[x,y].IsAlive);
        }

        [TestMethod]
        public void T08_Board_SaveAndReload_Consistent()
        {
            var tempFile = Path.Combine(_testDataPath, "temp.txt");
            var board1 = new Board(10, 10, 1, 0.1, false);
            board1.SaveBoard(tempFile);
            
            var board2 = new Board(10, 10, 1, 0.1, false);
            board2.LoadBoard(tempFile);

            CollectionAssert.AreEqual(
                board1.Cells.Cast<Cell>().Select(c => c.IsAlive).ToList(),
                board2.Cells.Cast<Cell>().Select(c => c.IsAlive).ToList()
            );
        }

        // =============================
        // Тесты для распознавания фигур
        // =============================
        [TestMethod]
        public void T09_Recognize_Block()
        {
            var board = new Board(2, 2, 1, 0.1, false);
            board.LoadBoard(Path.Combine(_testDataPath, "block.txt"));
            var components = board.FindComponents();
            PatternRecognizer.LoadPatterns("TestData");
            Assert.AreEqual("block", PatternRecognizer.Classify(components[0]));
        }
        [TestMethod]
        public void T10_Recognize_Hive()
        {
            var board = new Board(5, 5, 1, 0.0, false);
            board.LoadBoard(Path.Combine(_testDataPath, "hive.txt"));
            var components = board.FindComponents();
            PatternRecognizer.LoadPatterns("TestData");
            Assert.AreEqual("hive", PatternRecognizer.Classify(components[0]));
        }
        [TestMethod]
        public void T11_Recognize_Glider()
        {
            var board = new Board(5, 5, 1, 0.0, false);
            board.LoadBoard(Path.Combine(_testDataPath, "glider.txt"));
            var components = board.FindComponents();
            PatternRecognizer.LoadPatterns("TestData");
            Assert.AreEqual("glider", PatternRecognizer.Classify(components[0]));
        }
        [TestMethod]
        public void T12_Recognize_Toad()
        {
            var board = new Board(5, 5, 1, 0.0, false);
            board.LoadBoard(Path.Combine(_testDataPath, "toad.txt"));
            var components = board.FindComponents();
            PatternRecognizer.LoadPatterns("TestData");
            Assert.AreEqual("toad", PatternRecognizer.Classify(components[0]));
        }
        [TestMethod]
        public void T13_Stability_StaticBlock()
        {
            var board = new Board(4, 4, 1, 0.0, false);
            board.LoadBoard(Path.Combine(_testDataPath, "block.txt"));
            while (!board.IsStable())
            {
                board.Advance();
            }
            Assert.AreEqual(board.Generation, 10);
        }

        [TestMethod]
        public void T18_EmptyBoard_StaysEmpty()
        {
            var board = new Board(10, 10, 1, 0.0, false);
            board.Advance();
            Assert.AreEqual(0, board.Cells.Cast<Cell>().Count(c => c.IsAlive));
        }

        [TestMethod]
        public void T19_RandomBoard_ValidDensity()
        {
            var board = new Board(100, 100, 1, 0.1, false);
            int aliveCount = board.Cells.Cast<Cell>().Count(c => c.IsAlive);
            double density = (double)aliveCount / (100 * 100);
            Assert.IsTrue(density > 0.05 && density < 0.15);
        }
        private List<Cell> CreateAliveCells(int count)
        {
            return Enumerable.Range(0, count)
                .Select(_ => new Cell { IsAlive = true })
                .ToList();
        }
    }
}
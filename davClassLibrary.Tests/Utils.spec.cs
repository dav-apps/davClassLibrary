using NUnit.Framework;
using System.Collections.Generic;

namespace davClassLibrary.Tests
{
    [TestFixture][SingleThreaded]
    class UtilsTest
    {
        #region Setup
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            Utils.GlobalSetup();
        }
        #endregion

        #region SortTableIds
        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreNoParallelTableIds()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    
                    pages:               2, 2, 2, 2

                Output:
                    [1, 1, 2, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int>();
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 1, 2, 2, 3, 3, 4, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereIsOneParallelTableId()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:       2
                    pages:               2, 2, 2, 2

                Output:
                    [1, 1, 2, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 1, 2, 2, 3, 3, 4, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenTheParallelTableIdsAreSideBySide()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:       2, 3
                    pages:               2, 2, 2, 2

                Output:
                    [1, 1, 2, 3, 2, 3, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 2, 3 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 1, 2, 3, 2, 3, 4, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenTheParallelTableIdsAreNotSideBySide()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1,       4
                    pages:               2, 2, 2, 2

                Output:
                    [1, 2, 2, 3, 3, 4, 1, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 4 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 2, 3, 3, 4, 1, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreDifferentPagesAndTheParallelTableIdsAreNotSideBySide()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1,       4
                    pages:               3, 1, 2, 4

                Output:
                    [1, 2, 3, 3, 4, 1, 4, 1, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 4 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 3,
                [2] = 1,
                [3] = 2,
                [4] = 4
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 3, 3, 4, 1, 4, 1, 4, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreDifferentPagesAndTheParallelTableIdsAreSideBySide()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               2, 4, 3, 2

                Output:
                    [1, 2, 1, 2, 2, 2, 3, 3, 3, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 4,
                [3] = 3,
                [4] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 1, 2, 2, 2, 3, 3, 3, 4, 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreNoPages()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               0, 0, 0, 0

                Output:
                    []
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 0,
                [2] = 0,
                [3] = 0,
                [4] = 0
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int>(), sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereIsOnePage()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               0, 0, 0, 1

                Output:
                    [4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 0,
                [2] = 0,
                [3] = 0,
                [4] = 1
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 4 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreLotsOfPagesForOneTable()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               6, 0, 0, 0

                Output:
                    [1, 1, 1, 1, 1, 1]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 6,
                [2] = 0,
                [3] = 0,
                [4] = 0
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 1, 1, 1, 1, 1 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreDifferentCountsOfPagesForParallelTables()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               6, 8, 1, 0

                Output:
                    [1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 2, 2, 3]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 6,
                [2] = 8,
                [3] = 1,
                [4] = 0
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 2, 2, 3 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreMultipleParallelTables()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4, 5
                    parallelTableIds:    1, 2, 3,    5
                    pages:               2, 2, 2, 4, 2

                Output:
                    [1, 2, 3, 4, 4, 4, 4, 5, 1, 2, 3, 5]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4, 5 };
            List<int> parallelTableIds = new List<int> { 1, 2, 3, 5 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 4,
                [5] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 3, 4, 4, 4, 4, 5, 1, 2, 3, 5 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereAreMultipleParallelTablesWithDifferentCountsOfPages()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4, 5
                    parallelTableIds:    1, 2, 3,    5
                    pages:               3, 6, 4, 3, 2

                Output:
                    [1, 2, 3, 4, 4, 4, 5, 1, 2, 3, 5, 1, 2, 3, 2, 3, 2, 2]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4, 5 };
            List<int> parallelTableIds = new List<int> { 1, 2, 3, 5 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 3,
                [2] = 6,
                [3] = 4,
                [4] = 3,
                [5] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 3, 4, 4, 4, 5, 1, 2, 3, 5, 1, 2, 3, 2, 3, 2, 2 }, sortedTableIds);
        }

        [Test]
        public void SortTableIdsShouldReturnTheCorrectArrayWhenThereArePagesForNonExistentTables()
        {
            /*
                Input:
                    tableIds:            1, 2, 3, 4
                    parallelTableIds:    1, 2
                    pages:               2, 2, 2, 2, 2

                Output:
                    [1, 2, 1, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<int> tableIds = new List<int> { 1, 2, 3, 4 };
            List<int> parallelTableIds = new List<int> { 1, 2 };
            Dictionary<int, int> tableIdPages = new Dictionary<int, int>
            {
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2,
                [5] = 2
            };

            // Act
            List<int> sortedTableIds = davClassLibrary.Utils.SortTableIds(tableIds, parallelTableIds, tableIdPages);

            // Assert
            Assert.AreEqual(new List<int> { 1, 2, 1, 2, 3, 3, 4, 4 }, sortedTableIds);
        }
        #endregion
    }
}

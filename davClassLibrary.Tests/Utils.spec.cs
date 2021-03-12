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
        #endregion
    }
}

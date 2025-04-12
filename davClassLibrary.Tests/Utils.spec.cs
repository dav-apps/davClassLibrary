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

        #region SortTableNames
        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreNoParallelTableNames()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    
                    pages:                 2, 2, 2, 2

                Output:
                    [1, 1, 2, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string>();
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "1", "2", "2", "3", "3", "4", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereIsOneParallelTableName()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:       2
                    pages:                 2, 2, 2, 2

                Output:
                    [1, 1, 2, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "1", "2", "2", "3", "3", "4", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenTheParallelTableNamesAreSideBySide()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:       2, 3
                    pages:                 2, 2, 2, 2

                Output:
                    [1, 1, 2, 3, 2, 3, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "2", "3" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "1", "2", "3", "2", "3", "4", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenTheParallelTableNamesAreNotSideBySide()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1,       4
                    pages:                 2, 2, 2, 2

                Output:
                    [1, 2, 2, 3, 3, 4, 1, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "4" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "2", "3", "3", "4", "1", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreDifferentPagesAndTheParallelTableNamesAreNotSideBySide()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1,       4
                    pages:                 3, 1, 2, 4

                Output:
                    [1, 2, 3, 3, 4, 1, 4, 1, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "4" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 3,
                ["2"] = 1,
                ["3"] = 2,
                ["4"] = 4
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "3", "3", "4", "1", "4", "1", "4", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreDifferentPagesAndTheParallelTableNamesAreSideBySide()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 2, 4, 3, 2

                Output:
                    [1, 2, 1, 2, 2, 2, 3, 3, 3, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 4,
                ["3"] = 3,
                ["4"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "1", "2", "2", "2", "3", "3", "3", "4", "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreNoPages()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 0, 0, 0, 0

                Output:
                    []
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 0,
                ["2"] = 0,
                ["3"] = 0,
                ["4"] = 0
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string>(), sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereIsOnePage()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 0, 0, 0, 1

                Output:
                    [4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 0,
                ["2"] = 0,
                ["3"] = 0,
                ["4"] = 1
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "4" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreLotsOfPagesForOneTable()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 6, 0, 0, 0

                Output:
                    [1, 1, 1, 1, 1, 1]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 6,
                ["2"] = 0,
                ["3"] = 0,
                ["4"] = 0
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "1", "1", "1", "1", "1" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreDifferentCountsOfPagesForParallelTables()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 6, 8, 1, 0

                Output:
                    [1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 2, 2, 3]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 6,
                ["2"] = 8,
                ["3"] = 1,
                ["4"] = 0
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "1", "2", "1", "2", "1", "2", "1", "2", "1", "2", "2", "2", "3" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreMultipleParallelTables()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4, 5
                    parallelTableNames:    1, 2, 3,    5
                    pages:                 2, 2, 2, 4, 2

                Output:
                    [1, 2, 3, 4, 4, 4, 4, 5, 1, 2, 3, 5]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4", "5" };
            List<string> parallelTableNames = new List<string> { "1", "2", "3", "5" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 4,
                ["5"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "3", "4", "4", "4", "4", "5", "1", "2", "3", "5" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereAreMultipleParallelTablesWithDifferentCountsOfPages()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4, 5
                    parallelTableNames:    1, 2, 3,    5
                    pages:                 3, 6, 4, 3, 2

                Output:
                    [1, 2, 3, 4, 4, 4, 5, 1, 2, 3, 5, 1, 2, 3, 2, 3, 2, 2]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4", "5" };
            List<string> parallelTableNames = new List<string> { "1", "2", "3", "5" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 3,
                ["2"] = 6,
                ["3"] = 4,
                ["4"] = 3,
                ["5"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "3", "4", "4", "4", "5", "1", "2", "3", "5", "1", "2", "3", "2", "3", "2", "2" }, sortedTableNames);
        }

        [Test]
        public void SortTableNamesShouldReturnTheCorrectArrayWhenThereArePagesForNonExistentTables()
        {
            /*
                Input:
                    tableNames:            1, 2, 3, 4
                    parallelTableNames:    1, 2
                    pages:                 2, 2, 2, 2, 2

                Output:
                    [1, 2, 1, 2, 3, 3, 4, 4]
            */
            // Arrange
            List<string> tableNames = new List<string> { "1", "2", "3", "4" };
            List<string> parallelTableNames = new List<string> { "1", "2" };
            Dictionary<string, int> tableNamePages = new Dictionary<string, int>
            {
                ["1"] = 2,
                ["2"] = 2,
                ["3"] = 2,
                ["4"] = 2,
                ["5"] = 2
            };

            // Act
            List<string> sortedTableNames = davClassLibrary.Utils.SortTableNames(tableNames, parallelTableNames, tableNamePages);

            // Assert
            Assert.AreEqual(new List<string> { "1", "2", "1", "2", "3", "3", "4", "4" }, sortedTableNames);
        }
        #endregion
    }
}

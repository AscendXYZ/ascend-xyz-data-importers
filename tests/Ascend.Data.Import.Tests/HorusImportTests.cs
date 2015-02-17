using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ascend.Data.Import.Horus;
using System.IO;
using System.Threading.Tasks;
using Ascend.Data.Import.Tests.Mocks;
using Ascend.Data.Import;
using Ascend.Data.Import.CesiumExtensions;
namespace Ascend.Data.Import.Tests
{
    /// <summary>
    /// Summary description for HorusImportTests
    /// </summary>
    [TestClass]
    public class HorusImportTests
    {
        public HorusImportTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public async Task TestMethod1()
        {
            var importer = new HorusMetadataParser<string>(new DefaultDataReader<string>
            {
                 OpenStreamProvider = (path) => Task.FromResult( (Stream)File.OpenRead(path))
            });
            var data = new Dictionary<string, string>
            {
                {"../../../../data/MetaData_0-7664.csv","../../../../data/MetaData_0-7664.csv"}
            };
            var canImport = await importer.CanImportAsync(data, new FolderAccessFacade());

            Assert.IsTrue(canImport);
            var store = new AscendDataSetImporter();

            await importer.ImportDataSetsAsync(store, data, data);

            using (var ms = new FileStream("../../../../data/MetaData_0-7664.czml",FileMode.Create))
            {
                store.WriteCzmlDocument(ms, new CesiumDocumentOptions {  PrettyFormating = true, WriteFrames=true});
                ms.Flush();
            }

        }
    }
}

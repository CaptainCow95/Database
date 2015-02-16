using NUnit.Framework;
using DataOp = Database.Common.DataOperation;

namespace UnitTests
{
    [TestFixture]
    public class Document
    {
        [Test]
        public void AllJsonTypes()
        {
            const string json = "{\"int\":10,\"string\":\"test\",\"bool\":true,\"float\":5.345,\"array\":[10,\"test\",{\"key\":\"value\"}],\"document\":{\"key\":\"value\"}}";
            DataOp.Document doc = new DataOp.Document(json);
            Assert.AreEqual(true, doc.Valid, "The document is not valid.");
            Assert.AreEqual(6, doc.Count, "The document does not contain the right number of items.");
            Assert.AreEqual(false, doc.CheckForSubkeys(), "The document had sub-keys.");
            Assert.AreEqual(json, doc.ToJson(), "The json is incorrect.");

            Assert.AreEqual(true, doc.ContainsKey("int"), "The document did not contain the key \"int\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Integer, doc["int"].ValueType, "The value at key \"int\" was not an integer.");
            Assert.AreEqual(10, doc["int"].ValueAsInteger, "The value at key \"int\" was not 10.");

            Assert.AreEqual(true, doc.ContainsKey("string"), "The document did not contain the key \"string\".");
            Assert.AreEqual(DataOp.DocumentEntryType.String, doc["string"].ValueType, "The value at key \"string\" was not a string.");
            Assert.AreEqual("test", doc["string"].ValueAsString, "The value at key \"string\" was not \"test\".");

            Assert.AreEqual(true, doc.ContainsKey("bool"), "The document did not contain the key \"bool\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Boolean, doc["bool"].ValueType, "The value at key \"bool\" was not a bool.");
            Assert.AreEqual(true, doc["bool"].ValueAsBoolean, "The value at key \"bool\" was not true.");

            Assert.AreEqual(true, doc.ContainsKey("float"), "The document did not contain the key \"float\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Float, doc["float"].ValueType, "The value at key \"float\" was not a float.");
            Assert.That(doc["float"].ValueAsFloat, Is.EqualTo(5.345).Within(0.00001), "The value at key \"float\" was not 5.345.");

            Assert.AreEqual(true, doc.ContainsKey("array"), "The document did not contain the key \"array\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Array, doc["array"].ValueType, "The value at key \"array\" was not an array.");
            var array = doc["array"].ValueAsArray;
            Assert.AreEqual(3, array.Count, "The array does not contain the right number of items.");
            Assert.AreEqual(DataOp.DocumentEntryType.Integer, array[0].ValueType, "The first value of the array is not an integer.");
            Assert.AreEqual(10, array[0].ValueAsInteger, "The first value of the array is not 10.");
            Assert.AreEqual(DataOp.DocumentEntryType.String, array[1].ValueType, "The second value of the array is not a string.");
            Assert.AreEqual("test", array[1].ValueAsString, "The second value of the array is not \"test\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Document, array[2].ValueType, "The third value of the array is not a document.");
            Assert.True(array[2].ValueAsDocument.Equals(new DataOp.Document("{\"key\":\"value\"}")), "The third value of the array is not \"{\"key\":\"value\"}\".");

            Assert.AreEqual(true, doc.ContainsKey("document"), "The document did not contain the key \"document\".");
            Assert.AreEqual(DataOp.DocumentEntryType.Document, doc["document"].ValueType, "The value at key \"document\" was not a document.");
            Assert.True(doc["document"].ValueAsDocument.Equals(new DataOp.Document("{\"key\":\"value\"}")), "The value at key \"document\" was not \"{\"key\":\"value\"}\".");
            Assert.AreEqual(1, doc["document"].ValueAsDocument.Count, "The number of items in the document at key \"document\" was not 1.");
            Assert.AreEqual(DataOp.DocumentEntryType.String, doc["document"].ValueAsDocument["key"].ValueType, "The value at key \"key\" in the document at key \"document\" was not a string.");
            Assert.AreEqual("value", doc["document"].ValueAsDocument["key"].ValueAsString, "The value at key \"key\" in the document at key \"document\" was not \"value\".");
        }

        [Test]
        public void EmptyConstructor()
        {
            DataOp.Document doc = new DataOp.Document();
            Assert.AreEqual(true, doc.Valid, "The document is not valid.");
            Assert.AreEqual(0, doc.Count, "The number of items in the document was not 0.");
            Assert.AreEqual(false, doc.CheckForSubkeys(), "The document had sub-keys.");
            Assert.AreEqual("{}", doc.ToJson(), "The json is incorrect.");
        }

        [Test]
        public void EmptyJson()
        {
            DataOp.Document doc = new DataOp.Document("{}");
            Assert.AreEqual(true, doc.Valid, "The document is not valid.");
            Assert.AreEqual(0, doc.Count, "The number of items in the document was not 0.");
            Assert.AreEqual(false, doc.CheckForSubkeys(), "The document had sub-keys.");
            Assert.AreEqual("{}", doc.ToJson(), "The json is incorrect.");
        }
    }
}
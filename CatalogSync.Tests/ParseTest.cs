using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CatalogSync.Parsers;
using System.Reflection;
using System.IO;

namespace CatalogSync.Tests
{
	[TestClass]
	public class ParseTest
	{
		[TestMethod]
		public void BasicParseTest()
		{
			IParser<bool> p = new TestParser(); // The test parser simply returns true if it finds the word 'test'. 
			Assert.IsTrue(p.ParseHtml("this is a test"), "Test parser failed.");
			Assert.IsTrue(RequestParse<TestParser, bool>("this is another test"), "Generic parse method failed.");
			Assert.IsFalse(RequestParse<TestParser, bool>("this isn't an exam"), "Generic parse false positive.");
		}

		[TestMethod]
		public void ParseSectionListTest()
		{
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			string path = "CatalogSync.Tests.Test_Data";
			var reader = new StreamReader(thisAssembly.GetManifestResourceStream(path + ".TestSectionList.txt"));
			var content = reader.ReadToEnd();

			var p = new SectionListParser();
			var results = p.ParseHtml(content);

			// Check integrity on some random CRNs
			Assert.IsTrue(results["68877"].SubjectCode.Equals("CS"), "Incorrect result on subject code parse.");
			Assert.IsTrue(results["13189"].Number.Equals("23500"), "Incorrect result on course number.");
			Assert.IsTrue(results["45909"].Title.Equals("Programming In C"), "Incorrect result on course title.");
			Assert.IsTrue(results["65457"].Meetings.Count > 0, "Error in parsing sections with multiple meetings.");
		}

		[TestMethod]
		public void ParseSectionDetailsTest()
		{
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			string path = "CatalogSync.Tests.Test_Data";
			var reader = new StreamReader(thisAssembly.GetManifestResourceStream(path + ".TestSectionDetails.txt"));
			var content = reader.ReadToEnd();

			var p = new SectionDetailsParser();
			var results = p.ParseHtml(content);

			// Check integrity on some random CRNs
			Assert.IsTrue(results["13209"].SubjectCode.Equals("CS"), "Incorrect result on subject code parse.");
			Assert.IsTrue(results["53032"].Number.Equals("25100"), "Incorrect result on course number");
			Assert.IsTrue(results["62132"].Title.Equals("Operating Systems"), "Incorrect result on course title.");
			Assert.IsTrue(results["65474"].Meetings.Count > 1, "Error in parsing sections with multiple meetings.");
		}

		private U RequestParse<T, U>(string content) where T : IParser<U>, new()
		{
			var parser = new T();
			U parseResults = parser.ParseHtml(content);
			return parseResults;
		}
	}
}

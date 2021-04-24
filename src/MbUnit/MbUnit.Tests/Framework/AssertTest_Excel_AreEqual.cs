using MbUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace MbUnit.Tests.Framework
{
  [TestsOn(typeof(Assert))]
  public class AssertTest_Excel_AreEqual : BaseAssertTest
  {
    [Test]
    [Row("..\\Framework\\AssertTest_Excel_AreEqual.xls", "..\\Framework\\AssertTest_Excel_AreEqual_copy.xls")]
    public void AreEqual_passes(string expectedFileName, string actualFileName)
    {
      string expected = Path.Combine(Path.GetDirectoryName(typeof(AssertTest_Excel_AreEqual).Assembly.Location), expectedFileName);
      string actual = Path.Combine(Path.GetDirectoryName(typeof(AssertTest_Excel_AreEqual).Assembly.Location), actualFileName);
      Assert.Excel.AreEqual(expected, actual);
    }

    [Test]
    [Row("..\\Framework\\AssertTest_Excel_AreEqual.xls", "..\\Framework\\AssertTest_Excel_AreNotEqual.xls")]
    [Row("..\\Framework\\AssertTest_Excel_AreEqual.xls", "..\\Framework\\AssertTest_Excel_AreNotEqual_missing_image.xls")]
    public void AreNotEqual_passes(string expectedFileName, string actualFileName)
    {
      string expected = Path.Combine(Path.GetDirectoryName(typeof(AssertTest_Excel_AreEqual).Assembly.Location), expectedFileName);
      string actual = Path.Combine(Path.GetDirectoryName(typeof(AssertTest_Excel_AreEqual).Assembly.Location), actualFileName);
      Assert.Excel.AreNotEqual(expected, actual);
    }
  }
}

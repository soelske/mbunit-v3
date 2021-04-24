using Gallio.Framework.Assertions;
using System;

namespace MbUnit.Framework
{
  public abstract partial class Assert
  {
    /// <summary>
    /// Assertions for Excel data.
    /// </summary>
    public abstract class Excel
    {
      private static byte[] FileToByteArray(string fileName)
      {
        byte[] fileContent = null;
        System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fs);
        long byteLength = new System.IO.FileInfo(fileName).Length;
        fileContent = binaryReader.ReadBytes((Int32)byteLength);
        fs.Close();
        fs.Dispose();
        binaryReader.Close();
        return fileContent;
      }

      private static bool ByteArrayCompare(byte[] a1, byte[] a2)
      {
        if (a1.Length != a2.Length)
          return false;

        for (int i = 0; i < a1.Length; i++)
          if (a1[i] != a2[i])
            return false;

        return true;
      }

      #region Fragment Equality

      /// <summary>
      /// Asserts that two Excels have the same content.
      /// </summary>
      /// <param name="expectedExcel">The expected Excel.</param>
      /// <param name="actualExcel">The actual Excel.</param>
      /// <exception cref="AssertionException">Thrown if the verification failed unless the current <see cref="AssertionContext.AssertionFailureBehavior" /> indicates otherwise.</exception>
      /// <exception cref="ArgumentNullException">Thrown if <paramref name="expectedExcel"/>, <paramref name="actualExcel"/> is null.</exception>
      public static void AreEqual(string expectedExcel, string actualExcel)
      {
        if (expectedExcel == null)
          throw new ArgumentNullException("expectedExcel");
        if (actualExcel == null)
          throw new ArgumentNullException("actualExcel");

        byte[] expectedBytes = FileToByteArray(expectedExcel);
        byte[] actualBytes = FileToByteArray(actualExcel);

        IsTrue(ByteArrayCompare(expectedBytes, actualBytes));
      }

      /// <summary>
      /// Asserts that two Excels do not have the same content.
      /// </summary>
      /// <param name="expectedExcel">The expected XML fragment.</param>
      /// <param name="actualExcel">The actual XML fragment.</param>
      /// <exception cref="AssertionException">Thrown if the verification failed unless the current <see cref="AssertionContext.AssertionFailureBehavior" /> indicates otherwise.</exception>
      /// <exception cref="ArgumentNullException">Thrown if <paramref name="expectedExcel"/>, <paramref name="actualExcel"/> is null.</exception>
      public static void AreNotEqual(string expectedExcel, string actualExcel)
      {
        if (expectedExcel == null)
          throw new ArgumentNullException("expectedExcel");
        if (actualExcel == null)
          throw new ArgumentNullException("actualExcel");

        byte[] expectedBytes = FileToByteArray(expectedExcel);
        byte[] actualBytes = FileToByteArray(actualExcel);

        IsFalse(ByteArrayCompare(expectedBytes, actualBytes));
      }
      #endregion
    }
  }
}

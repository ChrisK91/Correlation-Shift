using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Correlation_Shift.Shifter;
using System.IO;

namespace ShiftTest
{
    [TestClass]
    public class ImageShifterTest
    {
        [TestMethod]
        public void TestImageLoadingOne()
        {
            string path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "5by5_one.tif");
            var result = Shifter.LoadImage8Bit(path);

            var expected = new byte[,] {
                {0,0,0,0,0},
                {0,1,0,0,0},
                {0,0,0,0,0},
                {0,0,0,0,0},
                {0,0,0,0,0}
            };

            CollectionAssert.AreEqual(result, expected);
        }

        [TestMethod]
        public void TestImageLoadingTwo()
        {
            string path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "5by5_two.tif");
            var result = Shifter.LoadImage8Bit(path);

            var expected = new byte[,] {
                  {0,0,0,0,0},
                  {0,0,0,0,0},
                  {0,0,0,0,0},
                  {0,0,0,1,0},
                  {0,0,0,0,0}
            };

            CollectionAssert.AreEqual(result, expected);

        }

        [TestMethod]
        public void TestImageLoadingRamp()
        {
            string path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "4by4ramp.tif");
            var result = Shifter.LoadImage8Bit(path);

            var expected = new byte[,] {
                  {0,64,128,192},
                  {0,64,128,192},
                  {0,64,128,192},
                  {0,64,128,192}
            };

            CollectionAssert.AreEqual(result, expected);

        }

        [TestMethod]
        public void TestImageLoading16bitRamp()
        {
            string path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "4by1ramp16bit.tif");
            var result = Shifter.LoadImage16Bit(path);

            var expected = new ushort[,] {
               {0,16384,32768,49152}
            };


            CollectionAssert.AreEqual(result, expected);

            path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "16ramp4by3.tif");
            result = Shifter.LoadImage16Bit(path);

            expected = new ushort[,] {
               {0,16384,32768,49152},
               {0,16384,32768,49152},
               {0,16384,32768,49152}
            };


            CollectionAssert.AreEqual(result, expected);

        }

        [TestMethod]
        public void TestInvariantLoading()
        {
            string path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "4by1ramp16bit.tif");

            var result = Shifter.LoadPictureInvariant(path);

            Assert.AreEqual(result.Item1, ImageType.UShort);

            var expected = new ushort[,] {
                {0,16384,32768,49152}
            };

            CollectionAssert.AreEqual(((ushort[,])result.Item2), expected);



            path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Images", "4by4ramp.tif");

            result = Shifter.LoadPictureInvariant(path);

            Assert.AreEqual(result.Item1, ImageType.Byte);

            var expected_byte = new byte[,] {
                  {0,64,128,192},
                  {0,64,128,192},
                  {0,64,128,192},
                  {0,64,128,192}
            };


            CollectionAssert.AreEqual(((byte[,])result.Item2), expected_byte);

        }

        [TestMethod]
        public void testSimpleOffset()
        {
            string path = Directory.GetCurrentDirectory();

            Shifter s = new Shifter();
            s.MinX = -5;
            s.MaxX = 5;
            s.MinY = -5;
            s.MaxX = 5;

            var result = s.DetermineBestShift(Path.Combine(path, "Images", "5by5_one.tif"), Path.Combine(path, "Images", "5by5_two.tif"));
            Assert.AreEqual(new Tuple<int, int>(2, 2), result);

            result = s.DetermineBestShift(Path.Combine(path, "Images", "5by5_two.tif"), Path.Combine(path, "Images", "5by5_one.tif"));
            Assert.AreEqual(new Tuple<int, int>(-2, -2), result);
        }

        [TestMethod]
        public void testLargeOffset()
        {
            string path = Directory.GetCurrentDirectory();

            Shifter s = new Shifter();
            s.MinX = -320;
            s.MaxX = -280;
            s.MinY = -5;
            s.MaxY = 5;

            var result = s.DetermineBestShift(Path.Combine(path, "Images", "Channel1_offset300x.tif"), Path.Combine(path, "Images", "Channel1.tif"));
            Assert.AreEqual(new Tuple<int, int>(-300, 0), result);
        }
    }
}

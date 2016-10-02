using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Correlation_Shift.Shifter
{
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }

    public enum ImageType
    {
        Byte,
        UShort
    };

    public class Shifter
    {
        /// <summary>
        /// Determines the leftmost shift along the horizontal (x) axis
        /// </summary>
        public int MinX = -20;
        /// <summary>
        /// Determines the rightmost shift along the horizontal (x) axis
        /// </summary>
        public int MaxX = 20;

        /// <summary>
        /// Determines the topmost shift along the vertical (y) axis
        /// </summary>
        public int MinY = -20;

        /// <summary>
        /// Determines the bottommost shift along the vertical (y) axis
        /// </summary>
        public int MaxY = 20;

        /// <summary>
        /// Determines the best correlative shift for two images
        /// </summary>
        /// <param name="path_one">The first image (tif)</param>
        /// <param name="path_two">The second image (tif)</param>
        /// <param name="output_directory">The location to save the image to</param>
        /// <returns></returns>
        public Tuple<int, int> DetermineBestShift(string path_one, string path_two)
        {
            var information_one = LoadPictureInvariant(path_one);
            var information_two = LoadPictureInvariant(path_two);

            double[,] image_one = unpackImageData(information_one);
            double[,] image_two = unpackImageData(information_two);

            double[][] jagged_one = convertToJaggedArray(image_one);
            double[][] jagged_two = convertToJaggedArray(image_two);

            double best_correlation = -1;
            int best_x = int.MinValue;
            int best_y = int.MinValue;

            int start_x, end_x, start_y, end_y; // these define the area of the second image we want to compar
            int reference_start_x, reference_end_x, reference_start_y, reference_end_y; // This is the area of the reference picture used to compare, as we dont wont to compare non-overlapping areas

            List<double> image_one_values = new List<double>();
            List<double> image_two_values = new List<double>();

            for (int x_offset = MinX; x_offset <= MaxX; x_offset++)
            {
                for (int y_offset = MinY; y_offset <= MaxY; y_offset++)
                {
                    // If coordinates are negative, we will start at the abs of the coordinate and go to the border
                    // (= shift to left or top)

                    if (x_offset < 0)
                    {
                        start_x = Math.Abs(x_offset);
                        end_x = jagged_one[0].Length;

                        reference_start_x = 0;
                        reference_end_x = jagged_two[0].Length + x_offset; //Attn: offset is negative!
                    }
                    else
                    {
                        start_x = 0;
                        end_x = jagged_one[0].Length - x_offset;

                        reference_start_x = x_offset;
                        reference_end_x = jagged_two[0].Length;
                    }

                    if (y_offset < 0)
                    {
                        start_y = Math.Abs(y_offset);
                        end_y = jagged_one.Length;

                        reference_start_y = 0;
                        reference_end_y = jagged_two.Length + y_offset; //Negative value is added!
                    }
                    else
                    {
                        start_y = 0;
                        end_y = jagged_one.Length - y_offset;

                        reference_start_y = y_offset;
                        reference_end_y = jagged_two.Length;
                    }

                    if (start_x != end_x &&
                        start_y != end_y &&
                        reference_start_x != reference_end_x &&
                        reference_start_y != reference_end_y &&
                        end_x - start_x > 0 &&
                        reference_end_x - reference_start_x > 0 &&
                        reference_start_y >= 0 &&
                        start_y >= 0
                        )
                    {
                        image_one_values.Clear();
                        for (int y_steps = start_y; y_steps < end_y; y_steps++)
                        {
                            image_one_values.AddRange(jagged_one[y_steps].SubArray(start_x, end_x - start_x));
                        }

                        image_two_values.Clear();
                        for (int y_steps = reference_start_y; y_steps < reference_end_y; y_steps++)
                        {
                            image_two_values.AddRange(jagged_two[y_steps].SubArray(reference_start_x, reference_end_x - reference_start_x));
                        }

                        double correlation = MathNet.Numerics.Statistics.Correlation.Pearson(image_one_values, image_two_values);

# if DEBUG
                        Console.WriteLine("{0}, {1}: {2}", x_offset, y_offset, correlation);
#endif

                        if (correlation > best_correlation)
                        {
                            best_correlation = correlation;
                            best_x = x_offset;
                            best_y = y_offset;
                        }
                    }
#if DEBUG
                    else
                    {
                        Console.Write("Skipped {0}, {1}", x_offset, y_offset);
                    }
#endif
                }
            }

            return new Tuple<int, int>(best_x, best_y);
        }

        /// <summary>
        /// Transforms an 2d array into a jagged array
        /// </summary>
        /// <param name="multiArray"></param>
        /// <returns></returns>
        static T[][] convertToJaggedArray<T>(T[,] multiArray)
        {
            int firstElement = multiArray.GetLength(0);
            int secondElement = multiArray.GetLength(1);

            T[][] jaggedArray = new T[firstElement][];

            for (int c = 0; c < firstElement; c++)
            {
                jaggedArray[c] = new T[secondElement];
                for (int r = 0; r < secondElement; r++)
                {
                    jaggedArray[c][r] = multiArray[c, r];
                }
            }
            return jaggedArray;
        }

        /// <summary>
        /// Extracts the image into a double array
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private double[,] unpackImageData(Tuple<ImageType, object> item)
        {
            switch (item.Item1)
            {
                case ImageType.Byte:
                    byte[,] tmp = (byte[,])item.Item2;
                    double[,] image = new double[tmp.GetLength(0), tmp.GetLength(1)];

                    for (int x = 0; x < tmp.GetLength(1); x++)
                    {
                        for (int y = 0; y < tmp.GetLength(0); y++)
                        {
                            image[y, x] = Convert.ToDouble(tmp[y, x]);
                        }
                    }
                    return image;
                case ImageType.UShort:
                    ushort[,] tmp_ushort = (ushort[,])item.Item2;
                    image = new double[tmp_ushort.GetLength(0), tmp_ushort.GetLength(1)];

                    for (int x = 0; x < tmp_ushort.GetLength(1); x++)
                    {
                        for (int y = 0; y < tmp_ushort.GetLength(0); y++)
                        {
                            image[y, x] = Convert.ToDouble(tmp_ushort[y, x]);
                        }
                    }
                    return image;
                default:
                    throw new ArgumentException("Invalid Imagetype supplied");
            }
        }

        /// <summary>
        /// Load an tif file, supports 8bit and 16bit
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Tuple<ImageType, object> LoadPictureInvariant(string path)
        {
            Tiff t = Tiff.Open(path, "r");
            int size = t.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

            switch (size)
            {
                case 8:
                    return new Tuple<ImageType, object>(ImageType.Byte, LoadTif8Bit(t)); // closes handle
                case 16:
                    return new Tuple<ImageType, object>(ImageType.UShort, convert8BitTo16Bit(LoadTif8Bit(t)));
                default:
                    throw new InvalidOperationException("The image type is not supported");
            }
        }

        /// <summary>
        /// Load a 16bit tif
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ushort[,] LoadImage16Bit(string path)
        {
            byte[,] image = LoadImage8Bit(path);
            return convert8BitTo16Bit(image);
        }

        /// <summary>
        /// Reinterpret a byte array as ushort array
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static ushort[,] convert8BitTo16Bit(byte[,] image)
        {
            ushort[,] ret = new ushort[image.GetLength(0), image.GetLength(1) / 2];

            var jagged_image = convertToJaggedArray(image);

            for (int i = 0; i < ret.GetLength(0); i++)
            {
                for (int j = 0; j < image.GetLength(1); j = j + 2)
                {
                    ret[i, j / 2] = BitConverter.ToUInt16(jagged_image[i], j);
                }
            }

            return ret;
        }

        /// <summary>
        /// Load an 8bit tiff
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[,] LoadImage8Bit(string path)
        {
            Tiff t = Tiff.Open(path, "r");
            return LoadTif8Bit(t);
        }

        public static byte[,] LoadTif8Bit(Tiff t)
        {
            int length = t.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            byte[,] image = new byte[length, t.ScanlineSize()];
            try
            {
                for (int i = 0; i < length; i++)
                {
                    byte[] buffer = new byte[t.ScanlineSize()];
                    t.ReadScanline(buffer, i);

                    for (int j = 0; j < image.GetLength(1); j++)
                    {
                        image[i, j] = buffer[j];
                    }
                }
            }
            finally
            {
                if (t != null)
                    t.Close();
            }

            return image;
        }

        public static void LogShift(string original, string newfolder, Tuple<int, int> offset)
        {
            using (StreamWriter sw = File.AppendText(Path.Combine(newfolder, "ShiftOffset.csv")))
            {
                //sw.Write("OriginalName,OffsetX,OffsetY,Timestamp");
                sw.Write("{0},{1},{2},{3}\n", Path.GetFileName(original), offset.Item1, offset.Item2, DateTime.Now.Ticks);
            }
        }

        public static void PerformShiftWithImageJ(Dictionary<string, Tuple<int, int>> FilesWithOffset, string output, string imageJPath)
        {
            StringBuilder sb = new StringBuilder();
            string newPath = "";
            string macro = Path.Combine(output, "macro.ijm");

            foreach (var item in FilesWithOffset)
            {
                newPath = Path.Combine(output, Path.GetFileName(item.Key));
                File.Copy(item.Key, newPath, true);
                sb.AppendLine(string.Format("processFile(\"{0}\", {1}, {2});", newPath.Replace(@"\",@"\\"), item.Value.Item1, item.Value.Item2));
            }

            using (TextWriter sw = File.CreateText(macro))
            {
                sw.Write(Correlation_Shift.Properties.Resources.MacroTemplate.Replace("%COMMANDS%", sb.ToString()));
            }

            ProcessStartInfo p = new ProcessStartInfo(imageJPath);
            p.Arguments = String.Format("-macro \"{0}\"", macro);
            p.WorkingDirectory = output;
            p.UseShellExecute = true;
            Process.Start(p);
        }
    }
}

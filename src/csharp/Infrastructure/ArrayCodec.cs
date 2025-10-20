using System;
using System.Buffers.Binary;
using System.Text;

namespace REWApi.Infrastructure
{
    /// <summary>
    /// Handles REW's Base64 big-endian float array encoding/decoding
    /// REW API returns frequency response and impulse response data as Base64-encoded 
    /// big-endian 32-bit float arrays
    /// </summary>
    public class ArrayCodec
    {
        /// <summary>
        /// Decode REW Base64 float array (big-endian format)
        /// </summary>
        /// <param name="base64Data">Base64 encoded string from REW API</param>
        /// <returns>Decoded float array</returns>
        /// <exception cref="ArgumentException">Thrown when base64Data is invalid</exception>
        public float[] DecodeFloatArray(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return Array.Empty<float>();

            try
            {
                byte[] bytes = Convert.FromBase64String(base64Data);
                
                if (bytes.Length % 4 != 0)
                    throw new ArgumentException("Base64 data length must be multiple of 4 bytes for float array");

                float[] result = new float[bytes.Length / 4];
                
                // REW uses big-endian format, convert to system endianness
                for (int i = 0; i < result.Length; i++)
                {
                    int byteIndex = i * 4;
                    uint intValue = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(byteIndex, 4));
                    result[i] = BitConverter.UInt32BitsToSingle(intValue);
                }
                
                return result;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format", nameof(base64Data), ex);
            }
        }

        /// <summary>
        /// Encode float array to REW Base64 format (big-endian)
        /// </summary>
        /// <param name="data">Float array to encode</param>
        /// <returns>Base64 encoded string in REW format</returns>
        public string EncodeFloatArray(float[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            byte[] bytes = new byte[data.Length * 4];
            
            // Convert to big-endian format for REW
            for (int i = 0; i < data.Length; i++)
            {
                uint intValue = BitConverter.SingleToUInt32Bits(data[i]);
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(i * 4, 4), intValue);
            }
            
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decode Base64 double array (big-endian format)
        /// Used for some REW API endpoints that return double precision data
        /// </summary>
        /// <param name="base64Data">Base64 encoded string from REW API</param>
        /// <returns>Decoded double array</returns>
        public double[] DecodeDoubleArray(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return Array.Empty<double>();

            try
            {
                byte[] bytes = Convert.FromBase64String(base64Data);
                
                if (bytes.Length % 8 != 0)
                    throw new ArgumentException("Base64 data length must be multiple of 8 bytes for double array");

                double[] result = new double[bytes.Length / 8];
                
                // REW uses big-endian format, convert to system endianness
                for (int i = 0; i < result.Length; i++)
                {
                    int byteIndex = i * 8;
                    ulong longValue = BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(byteIndex, 8));
                    result[i] = BitConverter.UInt64BitsToDouble(longValue);
                }
                
                return result;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format", nameof(base64Data), ex);
            }
        }

        /// <summary>
        /// Encode double array to REW Base64 format (big-endian)
        /// </summary>
        /// <param name="data">Double array to encode</param>
        /// <returns>Base64 encoded string in REW format</returns>
        public string EncodeDoubleArray(double[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            byte[] bytes = new byte[data.Length * 8];
            
            // Convert to big-endian format for REW
            for (int i = 0; i < data.Length; i++)
            {
                ulong longValue = BitConverter.DoubleToUInt64Bits(data[i]);
                BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(i * 8, 8), longValue);
            }
            
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Validate that Base64 string represents a valid float array
        /// </summary>
        /// <param name="base64Data">Base64 string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidFloatArray(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return true; // Empty is valid

            try
            {
                byte[] bytes = Convert.FromBase64String(base64Data);
                return bytes.Length % 4 == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get the number of float values in a Base64 encoded array
        /// </summary>
        /// <param name="base64Data">Base64 encoded string</param>
        /// <returns>Number of float values</returns>
        public int GetFloatArrayLength(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return 0;

            try
            {
                byte[] bytes = Convert.FromBase64String(base64Data);
                return bytes.Length / 4;
            }
            catch (FormatException)
            {
                return 0;
            }
        }
    }
}
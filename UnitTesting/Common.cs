using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnitTesting
{
    public static class Common
    {
        public static bool DoSerialisableObjectsHaveMatchingContent(object x, object y)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");

            var dataX = serialise(x);
            var dataY = serialise(y);
            if (dataX.Length != dataY.Length)
                return false;

            for (var index = 0; index < dataX.Length; index++)
            {
                if (dataX[index] != dataY[index])
                    return false;
            }
            return true;
        }
        
        private static byte[] serialise(object src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            using (var stream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stream, src);
                return stream.ToArray();
            }
        }
    }
}

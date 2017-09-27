using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPort
{
    class Parser
    {
        private const byte Flag = 0x7E;
        private const int DataBites = 7;
        private static readonly byte[] Header = { Flag, 0xFF, 03, 0x00 };
        bool[] values = { false, true, true, true, true, true, false } ;
        bool[] decodeValues = { false, true, true, true, true, true, true, false };
        bool[] controlValues = { true, false, false, false, false, false, false, false };

        public byte[] Encode(byte[] buffer)
        {
            BitArray bitArray = new BitArray(buffer);
                       
            List<object> temp = new List<object>();
            foreach(var elem in bitArray)
                temp.Add(elem);
            while (temp.Count % 7 != 0)
                temp.Add(true);
            var packages = temp.ChunkBy(7);

            BitArray template = new BitArray(values);
            List<byte> result = new List<byte>();

            foreach (var item in packages)
            {
                if (item.Equals(template))
                    item.Insert(6, true);
                var controlBit = false;
                foreach(var bit in item)
                {
                    if (bit.Equals(true))
                        controlBit = !controlBit;
                }
                item.Add(controlBit);

                result.AddRange(Header);
                if (item.Count == 8)
                {
                    result.Add(BitArrayToByte(item.ToArray()));
                }
                else if (item.Count > 8)
                {
                    result.Add(BitArrayToByte(item.GetRange(0, 8).ToArray()));
                    result.Add(BitArrayToByte(item.GetRange(8, item.Count).ToArray()));
                }
                result.Add(Flag);              
            }
            return result.ToArray();
        }

        public byte[] Decode(byte[] buffer)
        {
            var bufferList = buffer.ToList();
            var decodeBits = new List<bool>();
            var result = new List<byte>();
            for (int j = 0; bufferList.Count > 3; )
            {
                if (bufferList[j] == Flag)
                {
                    bufferList.Remove(Flag);
                    var package = bufferList.GetRange(0, bufferList.IndexOf(Flag) + 1);                  
                    if (package.ElementAt(0) != Header[1]
                        || package.ElementAt(1) != Header[2]
                        || package.ElementAt(2) != Header[3])
                    {
                        return null;
                    }
                    package.RemoveRange(0, 3);

                    var bits = new BitArray(new byte[] { package.First() });
                    if (bits.Equals(decodeValues))
                    {
                        if (new BitArray(new byte[] { package.ElementAt(1) }).Equals(controlValues))
                        {
                            return null;
                        }
                        foreach (var elem in values)
                            decodeBits.Add(elem.Equals(true));
                        package.RemoveRange(0,2);
                    }
                    else
                    {
                        //--------------------------------------------------------------------
                        var controlBit = false;
                        for (int i = 0; i < 7; i++)
                        {
                            if (bits[i].Equals(true))
                                controlBit = !controlBit;
                        }
                        if (bits[7] != controlBit)
                        {
                            return null;
                        }

                        //--------------------------------------------------------------------
                        for (int i = 0; i < 7; i++)
                            decodeBits.Add(bits[i]);
                    }                   
                    package.RemoveAt(0);
                }
                bufferList.RemoveRange(0, bufferList.IndexOf(Flag) + 1);
            }
            while (decodeBits.Count % 8 != 0)
                decodeBits.RemoveAt(decodeBits.Count-1);
            foreach(var item in decodeBits.ChunkBy(8))           
                result.Add(BitArrayToByte(item.ToArray()));                    
            return result.ToArray();
        }

        private static byte BitArrayToByte(object[] bitArray)
        {
            byte result = 0;
            for (byte index = 0, m = 1; index < bitArray.Count(); index++, m *= 2)
                result += bitArray[index].Equals(true) ? m : (byte)0;
            return result;
        }

        private static byte BitArrayToByte(bool[] bitArray)
        {
            byte result = 0;
            for (byte index = 0, m = 1; index < bitArray.Count(); index++, m *= 2)
                result += bitArray[index].Equals(true) ? m : (byte)0;
            return result;
        }
    }
    public static class ListExtensions
    {       
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) => source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
    }
    
    
}

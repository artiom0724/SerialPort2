using System.IO.Ports;
using System.Threading;

namespace SerialPort
{
    public class Serial : System.IO.Ports.SerialPort
    {
        public byte[] LostBytes { get; set; }

        public Serial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : base(portName,
            baudRate, parity, dataBits, stopBits)
        {
        }

        public byte[] ReadBytes()
        {
            var data = new byte[BytesToRead];
            Read(data, 0, data.Length);
            Parser parser = new Parser();
            return parser.Decode(data);
        }

        public void WriteData(byte[] dataBytes)
        {
            Parser parser = new Parser();
            while (true)
            {
                if (BytesToRead == 0)
                {
                    RtsEnable = true;
                    var sendingData = parser.Encode(dataBytes);
                    Write(sendingData, 0, sendingData.Length);

                    Thread.Sleep(100);
                    RtsEnable = false;
                }
                else
                {
                    LostBytes = ReadBytes();
                    continue;
                }
                break;
            }
        }
    }
}
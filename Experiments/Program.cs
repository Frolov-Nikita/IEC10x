using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IEC10x;

namespace Experiments
{

    class Program
    {
        static void Main(string[] args)
        {
            SerialPort port = new SerialPort("COM4", 9600, Parity.Even, 7, StopBits.One);
            CE303 ce303 = new CE303(port);

            ce303.OnCommunicate += Ce303_OnCommunicate;

            var data = ce303.ReadMessuredData();

            foreach (var d in data)
                Console.WriteLine($"{d.Address}:{d.Value}");

            ce303.CmdB();

            Console.ReadKey();
        }

        private static void Ce303_OnCommunicate(IEC107PortCommDirection direction, byte[] buffer)
        {
            var str = DateTime.Now.ToString("HH:mm:ss.ffff") + " "; // [14]
            if (direction == IEC107PortCommDirection.Read)
                str += "<<"; // [16]
            if (direction == IEC107PortCommDirection.Write)
                str += ">>";
            str += " :"; // [18]
            str += BitConverter.ToString(buffer).Replace('-',' ');
            Console.WriteLine(str);
            str = new string(' ', 18);
            str += GetASCIIStr(buffer);
            Console.WriteLine(str);
        }

        public static string GetASCIIchar(byte b)
        {
            if (b == 0x7F)
                return "<DEL>";
            if (b > 0x1F)
                return "" + (char)b;
            else
                return $"<{((AsciiControlSymbols)b).ToString()}>";
        }

        public static string GetASCIIStr(byte[] buffer)
        {
            string str = "";
            foreach (var b in buffer)
                str += GetASCIIchar(b);
            return str;
        }

    }
}

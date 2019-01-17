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

            var data = ce303.ReadVoltage();

            foreach (var d in data)
                Console.WriteLine($"{d.Address}:{d.Value}");

            Console.ReadKey();
            ce303.Bye();

            port.Close();
        }

    }
}

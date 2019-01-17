using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC10x
{
    public class IEC107ModC : IEC107
    {
        public IEC107ModC(SerialPort port, string deviceAddress = "") 
            : base(port, deviceAddress)
        {}

        protected void InitSession() {

        }


    }
}

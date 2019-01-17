using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC10x
{
    public class CE303 : IEC107
    {
        public CE303(SerialPort port)
            :base(port, "")
        {            
        }

        public List<DataSet> ReadVoltage() {
            return CmdR(new DataSet {Address ="VOLTA" });
        }
    }
}

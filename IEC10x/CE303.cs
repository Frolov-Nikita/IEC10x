using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC10x
{
    public class CE303 : IEC107ModC
    {
        public CE303(SerialPort port)
            :base(port, CE303PasswordService.Instance)
        {
            
        }

        public List<DataSet> ReadVoltage() {
            return CmdR(new DataSet {Address ="VOLTA" });
        }
    }
}

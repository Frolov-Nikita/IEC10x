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
        IPasswordService passwordService;

        public IEC107ModC(SerialPort port, IPasswordService passwordService, string deviceAddress = "") 
            : base(port, deviceAddress)
        {
            this.passwordService = passwordService;
        }

        /// <summary>
        /// Initializes new session.
        /// </summary>
        /// <param name="deviceAddress"></param>
        protected override void Init(string deviceAddress = "")
        {
            StartSession(deviceAddress);
        }

        public DataBlock ReadMessuredData(int nakRetries = 1)
        {
            if (SessState == SessionState.Program)
                CmdB();

            if (SessState == SessionState.Disconnected)
                Init();

            int len = MakeAck(ref buffer, false, '0', BaudRateInIdentifier);
            port.Write(ref buffer, 0, len);
            port.SetBaudRate(BaudRateInIdentifier);
            var result = ReadInfoMessage();

            if ((result.ControlSymbol == AsciiControlSymbols.NAK) && (nakRetries-- > 0))
                return ReadMessuredData(nakRetries);

            return result;
        }

        protected override void ToProgram()
        {
            if (SessState == SessionState.Disconnected)
                Init();
            int len = MakeAck(ref buffer, false, '1', BaudRateInIdentifier);
            port.Write(ref buffer, 0, len);
            port.SetBaudRate(BaudRateInIdentifier);

            var cmdP0 = ReadCommand();
            SessState = SessionState.Program;

            var passKey = cmdP0.DataSet.Value;
            string password = passwordService.GetPassword(passKey);

            var r = CmdP(password);

            if (r == AsciiControlSymbols.ACK)
                SessState = SessionState.Program;
            else
                throw new Exception("Не удалось переключиться в режим программирования.");
        }
    }
}

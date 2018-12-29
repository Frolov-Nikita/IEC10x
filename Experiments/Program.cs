﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] qStartSess = { 0x2F, 0x3F, 0x21, 0x0D, 0x0A }; // /?!<cr><lf>
            byte[] qGetId = { 0x06, 0x30, 0x35, 0x31, 0x0D, 0x0A };// /<ACK>051<cr><lf> //перевод в режим программирования
            byte[] qEnterPass = { 0x01, 0x50, 0x31, 0x02, 0x28, 0x37, 0x37, 0x37, 0x37, 0x37, 0x37, 0x29, 0x03, 0x21 }; // ввод пароля
            byte[] qU = { 0x01, 0x52, 0x31, 0x02, 0x56, 0x4F, 0x4C, 0x54, 0x41, 0x28, 0x29, 0x03, 0x5F }; // <SOH>R1<STX>VOLTA()<ETX>_
            byte[] qI = { 0x01, 0x52, 0x31, 0x02, 0x43, 0x55, 0x52, 0x52, 0x45, 0x28, 0x29, 0x03, 0x5A }; // <SOH>R1<STX>CURRE()<ETX>Z
            byte[] qBye = { 0x01, 0x42, 0x30, 0x03, 0x75 }; // <SOH>B0<ETX>u

            byte[] response = new byte[1024];

            SerialPort sp = new SerialPort("COM4", 9600, Parity.Even, 7, StopBits.One);
            sp.Open();
            int len = 0;
            int sleepTime1 = 000;
            int sleepTime2 = 400;

            sp.Write(qStartSess, 0, qStartSess.Length);
            Thread.Sleep(sleepTime2);
            len = sp.BytesToRead;
            sp.Read(response, 0, len);
            Console.WriteLine($">[{len}]: {GetStr(response)}");

            Thread.Sleep(sleepTime1);
            sp.BaseStream.Flush();
            sp.Write(qGetId, 0, qGetId.Length);
            Thread.Sleep(sleepTime2);
            len = sp.BytesToRead;
            sp.Read(response, 0, len);
            Console.WriteLine($">[{len}]: {GetStr(response)}");

            /*
            // пароль
            Thread.Sleep(sleepTime1);
            sp.BaseStream.Flush();
            sp.Write(qEnterPass, 0, qEnterPass.Length);
            Thread.Sleep(sleepTime2);
            len = sp.BytesToRead;
            sp.Read(response, 0, len);
            Console.WriteLine($">[{len}]: {GetStr(response)}");
            */

            Thread.Sleep(sleepTime1);
            sp.BaseStream.Flush();
            sp.Write(qU, 0, qU.Length);
            Thread.Sleep(sleepTime2);
            len = sp.BytesToRead;
            sp.Read(response, 0, len);
            Console.WriteLine($">[{len}]: {GetStr(response)}");
            /*
                        Thread.Sleep(sleepTime1);
                        sp.BaseStream.Flush();
                        sp.Write(qI, 0, qI.Length);
                        Thread.Sleep(sleepTime2);
                        len = sp.BytesToRead;
                        sp.Read(response, 0, len);
                        Console.WriteLine($">[{len}]: {GetStr(response)}");
                        */
            Console.ReadKey();
        }

        static string GetStr(byte[] buff)
        {
            string ret = "";
            foreach (var b in buff)
            {
                if ((b == 0x00) ||
                    (b == 0x01) ||
                    (b == 0x02)) continue;

                if ((b == 0x03) ||
                     (b == 0x17) ||
                     (b == 0x18) ||
                     (b == 0x19))
                    break;

                ret += (char)b;
            }
            return ret;
        }
    }
}

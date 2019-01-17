using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IEC10x
{
    public class IEC107Port
    {
        private SerialPort port;
        private int baseBaudRate = 300;

        /// <summary>
        /// Минимальное время между последним байтом запроса и первым байтом ответа
        /// </summary>
        private int trMin = 20 / 4;

        /// <summary>
        /// Заявленное производителем время ответа
        /// </summary>
        private int trMax = 200;
                
        /// <summary>
        /// Initializes a new instance of the <see cref="IEC107Port"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        public IEC107Port(SerialPort port)
        {
            this.port = port;
            this.baseBaudRate = port.BaudRate;
        }

        /// <summary>
        /// Установка новой BaudRate с отключение и подключением.
        /// </summary>
        /// <param name="newBaudRate">The new baud rate.</param>
        public void SetBaudRate(int newBaudRate)
        {
            if (port.BaudRate == newBaudRate) return;
            var isOpen = port.IsOpen;
            if (isOpen) port.Close();
            port.BaudRate = newBaudRate;
            if (isOpen) port.Open();
        }

        /// <summary>
        /// Сброс BaudRate к изначальному
        /// </summary>
        public void ResetBaudRate()
        {
            if (port.BaudRate == baseBaudRate) return;
            var isOpen = port.IsOpen;
            if (isOpen) port.Close();
            port.BaudRate = baseBaudRate;
            if (isOpen) port.Open();
        }

        /// <summary>
        /// Ожидает Count доступных байтов в порту для чтения в течении TrMax миллисекунд.
        /// </summary>
        /// <param name="count">Кол-во ожидаемых байтов</param>
        /// <returns>True - дождались, False - время истекло</returns>
        public bool WaitBytes(int count)
        {
            if (!port.IsOpen)
                throw new Exception("WaitBytes() port is closed");

            int lcount = port.BytesToRead;
            int t = 0;
            while (lcount < count)
            {
                Thread.Sleep(trMin);
                if (lcount == port.BytesToRead)
                {
                    t += trMin;
                    if (t >= trMax)
                        return false;
                }
                else
                {
                    lcount = port.BytesToRead;
                    t = 0;
                }
            }
            return true;
        }
                
        /// <summary>
        /// Читает в буфер до выполнения условия.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The buffer offset.</param>
        /// <param name="eor">End of read predicate</param>
        /// <returns></returns>
        public int Read(ref byte[] buffer, int offset, Predicate<byte> eor)
        {

            int i = offset;
            byte b;
            var bLen = buffer.LongLength - offset;
            do {
                if(bLen-- == 0)
                    throw new Exception("Read(,,p) end of buffer");
                if (!WaitBytes(1))
                    throw new Exception("Read(,,p) timeout");
                b = (byte)port.ReadByte();
                buffer[i++] = b;
            } while (!eor(b));

            return i - offset;
        }

        /// <summary>
        /// Читает заданное количество байт
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public int Read(ref byte[] buffer, int offset, int length)
        {
            if (buffer.LongLength - offset < length)
                throw new Exception("Read(,,l) buffer length less then read length");
            if (!WaitBytes(length))
                throw new Exception("Read(,,l) timeout");
            return port.Read(buffer, offset, port.BytesToRead);
        }

        /// <summary>
        /// Читает один байт
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Read() timeout</exception>
        public byte ReadByte()
        {
            if (WaitBytes(1))
                throw new Exception("Read() timeout");
            return (byte)port.ReadByte();
        }

        /// <summary>
        /// Записывает буфер в порт
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="Exception">Write(..). Длина буфера меньше требуемой длины.</exception>
        public void Write(ref byte[] buffer, int offset, int count)
        {
            if (buffer.Length < (offset + count))
                throw new Exception("Write(..). Длина буфера меньше требуемой длины.");

            port.Write(buffer, offset, count);
        }

        /// <summary>
        /// Записывает байт
        /// </summary>
        /// <param name="b">байт.</param>
        public void WriteByte(byte b)
        {
            port.Write(new byte[] { b }, 0, 1);
        }
    }
}

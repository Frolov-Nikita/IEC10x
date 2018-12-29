using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IEC10x
{
    /// <summary>
    /// Управляющие символы ASCII
    /// https://ru.wikipedia.org/wiki/ASCII
    /// </summary>
    public enum AsciiControlSymbols : byte
    {
        /// <summary>
        /// start of heading
        /// начало «заголовка»
        /// </summary>
        SOH = 0x1,
        /// <summary>
        /// start of text
        /// начало «текста»
        /// </summary>
        STX = 0x2,
        /// <summary>
        /// end of text
        /// конец «текста»
        /// </summary>
        ETX = 0x3,
        /// <summary>
        /// end of transmission
        /// конец передачи
        /// </summary>
        EOT = 0x4,
        /// <summary>
        /// enquire
        /// «Прошу подтверждения!»
        /// </summary>
        ENQ = 0x5,
        /// <summary>
        /// acknowledgement
        /// «Подтверждаю!»
        /// </summary>
        ACK = 0x6,
        /// <summary>
        /// bell
        /// звуковой сигнал: звонок
        /// </summary>
        BEL = 0x7,
        /// <summary>
        /// backspace
        /// возврат на один символ
        /// </summary>
        BS = 0x8,
        /// <summary>
        /// tab
        /// горизонтальная табуляция
        /// </summary>
        TAB = 0x9,
        /// <summary>
        /// line feed
        /// перевод строки
        /// </summary>
        LF = 0x0A,
        /// <summary>
        /// vertical tab
        /// вертикальная табуляция
        /// </summary>
        VT = 0x0B,
        /// <summary>
        /// form feed
        /// «прогон страницы», новая страница
        /// </summary>
        FF = 0x0C,
        /// <summary>
        /// carriage return
        /// возврат каретки
        /// </summary>
        CR = 0x0D,
        /// <summary>
        /// shift out
        /// «Переключиться на другую ленту (кодировку)»
        /// </summary>
        SO = 0x0E,
        /// <summary>
        /// shift in
        /// «Переключиться на исходную ленту (кодировку)»
        /// </summary>
        SI = 0x0F,
        /// <summary>
        /// data link escape
        /// «Экранирование канала данных»
        /// </summary>
        DLE = 0x10,
        /// <summary>
        /// device control 1
        /// Первый символ управления устройством
        /// </summary>
        DC1 = 0x11,
        /// <summary>
        /// device control 2
        /// Второй символ управления устройством
        /// </summary>
        DC2 = 0x12,
        /// <summary>
        /// device control 3
        /// Третий символ управления устройством
        /// </summary>
        DC3 = 0x13,
        /// <summary>
        /// device control 4
        /// Четвёртый символ управления устройством
        /// </summary>
        DC4 = 0x14,
        /// <summary>
        /// negative acknowledgment
        /// «Не подтверждаю!»
        /// </summary>
        NAK = 0x15,
        /// <summary>
        /// synchronization
        /// 
        /// </summary>
        SYN = 0x16,
        /// <summary>
        /// end of text block
        /// конец текстового блока
        /// </summary>
        ETB = 0x17,
        /// <summary>
        /// cancel
        /// «Отмена»
        /// </summary>
        CAN = 0x18,
        /// <summary>
        /// end of medium
        /// «Конец носителя»
        /// </summary>
        EM = 0x19,
        /// <summary>
        /// substitute
        /// «Подставить»
        /// </summary>
        SUB = 0x1A,
        /// <summary>
        /// escape
        /// 
        /// </summary>
        ESC = 0x1B,
        /// <summary>
        /// delete
        /// стереть последний символ
        /// </summary>
        DEL = 0x7F,
    }

    public class IEC107
    {
        SerialPort port;

        int startBaudRate = 300;

        byte[] buffer = new byte[4096];

        public string ManufacturerCode { get; private set; }

        public string Identifier { get; private set; }

        public IEC107(SerialPort serialPort, string deviceAddress = "", int startBaudRate = 300)
        {
            this.port = serialPort;
        }
        
        /// <summary>
        /// символы стандартных команд
        /// </summary>
        public enum CommandSymbbols : byte
        {
            /// <summary>
            /// Команда пароля
            /// </summary>
            Password = (byte)'P',
            /// <summary>
            /// Команда записи
            /// </summary>
            Write = (byte)'W',
            /// <summary>
            /// Команда чтения
            /// </summary>
            Read = (byte)'R',
            /// <summary>
            /// Команда выполнения
            /// </summary>
            Execute = (byte)'E',
            /// <summary>
            /// Команда завершения сеанса связи
            /// </summary>
            Bye = (byte)'B',
        }

        /// <summary>
        /// Режимы протокола
        /// </summary>
        public enum ProtocolMode
        {

            A,
            B,
            C,
            D
        }
        
        /// <summary>
        /// максимальное время между последним байтом запроса и первым байтом ответа
        /// </summary>
        /// <value>
        /// The tr.
        /// </value>
        public int Tr { get; set; } = 200;

        public ProtocolMode Mode { get; set; }

        public DateTime LastSessUpdate { get; set; }

        public struct DataSet
        {
            public string Address;
            public string Value;
            public string Device;
        }

        /// <summary>
        /// Получает символ скорости для режима "B"
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <returns>символ скорости</returns>
        byte GetBaudRateSymbolB(int baudRate)
        {
            switch (baudRate)
            {
                case 600:
                    return (byte)'A';
                case 1200:
                    return (byte)'B';
                case 2400:
                    return (byte)'C';
                case 4800:
                    return (byte)'D';
                case 9600:
                    return (byte)'E';
                default:
                    return (byte)'E';
            }
        }

        /// <summary>
        /// Получает символ скорости для режима "C"
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <returns>символ скорости</returns>
        byte GetBaudRateSymbolC(int baudRate)
        {
            switch (baudRate)
            {
                case 300:
                    return (byte)'0';
                case 600:
                    return (byte)'1';
                case 1200:
                    return (byte)'2';
                case 2400:
                    return (byte)'3';
                case 4800:
                    return (byte)'4';
                case 9600:
                    return (byte)'5';
                default:
                    return (byte)'5';
            }
        }

        /// <summary>
        /// Байт контрольной суммы
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Указанная дина меньше длины буфера.</exception>
        byte GetBCC(ref byte[] buffer, int startindex, int length)
        {
            if (buffer.Length < length)
                throw new ArgumentOutOfRangeException("Указанная дина меньше длины буфера.");

            byte sum = 0;
            for (int i = startindex; i < length; i++)
                sum += buffer[i];
            return sum;
        }

        /// <summary>
        /// Fits the length of the buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        void FitBufferLength(ref byte[] buffer, int length)
        {
            if (buffer.Length < length)
                buffer = new byte[length];
        }

        /// <summary>
        /// Makes the innitial query.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="address">The address.</param>
        /// <returns>count of buffer bytes</returns>
        /// <exception cref="ArgumentOutOfRangeException">Параметр address должен быть меньше 32-х символов</exception>
        public int MakeInit(ref byte[] buffer, string address = "")
        {
            if (address.Length > 32)
                throw new ArgumentOutOfRangeException("Параметр address должен быть меньше 32-х символов");

            int length = 5 + address.Length;
            FitBufferLength(ref buffer, length);

            buffer[0] = (byte)'/';
            buffer[1] = (byte)'?';
            int i = 2;

            foreach (char c in address)
                buffer[i++] = (byte)c;

            buffer[i++] = (byte)'!';
            buffer[i++] = (byte)AsciiControlSymbols.CR;
            buffer[i++] = (byte)AsciiControlSymbols.LF;

            return length;
        }

        /// <summary>
        /// Makes the ack.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="isSecondary">Нормальная или вторичная процедура протокола</param>
        /// <param name="prog">if set to <c>true</c> [prog].</param>
        /// <param name="baudRate">The baud rate. 0 - don`t change (Use this.BaudRate)</param>
        /// <returns></returns>
        public int MakeAck(ref byte[] buffer, bool isSecondary = false, bool prog = false, int baudRate = 0)
        {
            int length = 6, i = 0;
            FitBufferLength(ref buffer, length);
            baudRate = baudRate > 0 ? baudRate : port.BaudRate;
            buffer[i++] = (byte)AsciiControlSymbols.ACK;
            buffer[i++] = (byte)(isSecondary ? '1' : '0');
            buffer[i++] = GetBaudRateSymbolC(baudRate);
            buffer[i++] = (byte)(prog ? '1' : '0');
            buffer[i++] = (byte)AsciiControlSymbols.CR;
            buffer[i++] = (byte)AsciiControlSymbols.LF;
            return length;
        }

        /// <summary>
        /// Makes the query of the command.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="command">The command.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="data">The data.</param>
        /// <param name="isFullBlocks">if set to <c>true</c> [is full blocks].</param>
        /// <returns></returns>
        public int MakeCommand(ref byte[] buffer, CommandSymbbols command, char commandType = '0', string data = "", bool isFullBlocks = true)
        {
            int length = 6 + data.Length, i = 0;
            FitBufferLength(ref buffer, length);
            buffer[i++] = (byte)AsciiControlSymbols.SOH;
            buffer[i++] = (byte)command;
            buffer[i++] = (byte)commandType;

            if (commandType != '0')
            {
                buffer[i++] = (byte)AsciiControlSymbols.STX;

                foreach (char c in data)
                    buffer[i++] = (byte)c;
            }
            buffer[i++] = (byte)((isFullBlocks) ? AsciiControlSymbols.ETX : AsciiControlSymbols.EOT);
            buffer[i++] = GetBCC(ref buffer, 1, i);
            return i;
        }

        /// <summary>
        /// Определяет режим, код производ, скорость, идентификатор, тайминг по ответу на первичный запрос
        /// </summary>
        /// <param name="buffer">buffer of response</param>
        /// <param name="length">length of the buffer</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Длина буфера ответа меньше заявленной длины
        /// or
        /// Длина ответа меньше минимального
        /// or
        /// Длина ответа больше максимального
        /// or
        /// Ответ неподходящего формата
        /// or
        /// Ответ неподходящего формата. Код производителя.
        /// or
        /// Ответ неподходящего формата. Код скорости обмена.
        /// </exception>
        public void HandleIdentifierResponse(ref byte[] buffer, int length)
        {
            if (buffer.Length < length)
                throw new ArgumentException("Длина буфера ответа меньше заявленной длины");

            if (length < 8)
                throw new ArgumentException("Длина ответа меньше минимального");

            if (length > 23) // 1 + 3 + 1 + 16max + 2
                throw new ArgumentException("Длина ответа больше максимального");

            if ((buffer[0] != '/') || // заголовок
                (buffer[length - 2] != (byte)AsciiControlSymbols.CR) || // конец строки
                (buffer[length - 1] != (byte)AsciiControlSymbols.LF))
                throw new ArgumentException("Ответ неподходящего формата");


            if ((buffer[3] > (byte)'a') && (buffer[3] < (byte)'z'))
                Tr = 20;
            else if ((buffer[3] > (byte)'A') && (buffer[3] < (byte)'Z'))
                Tr = 200;
            else
                throw new ArgumentException("Ответ неподходящего формата. Код производителя.");

            ManufacturerCode = ASCIIEncoding.ASCII.GetString(buffer, 1, 3);

            Mode = ProtocolMode.A;
            switch ((char)buffer[4])
            {
                case '0':
                    port.BaudRate = 300;
                    Mode = ProtocolMode.C;
                    break;
                case '1':
                    port.BaudRate = 600;
                    Mode = ProtocolMode.C;
                    break;
                case '2':
                    port.BaudRate = 1200;
                    Mode = ProtocolMode.C;
                    break;
                case '3':
                    port.BaudRate = 2400;
                    Mode = ProtocolMode.C;
                    break;
                case '4':
                    port.BaudRate = 4800;
                    Mode = ProtocolMode.C;
                    break;
                case '5':
                    port.BaudRate = 9600;
                    Mode = ProtocolMode.C;
                    break;
                case '6':
                    port.BaudRate = 19200;
                    Mode = ProtocolMode.C;
                    break;
                case '7':
                    port.BaudRate = 38400;
                    Mode = ProtocolMode.C;
                    break;
                case '8':
                    port.BaudRate = 76800;
                    Mode = ProtocolMode.C;
                    break;
                case '9':
                    port.BaudRate = 153600;
                    Mode = ProtocolMode.C;
                    break;

                case 'A':
                    port.BaudRate = 600;
                    Mode = ProtocolMode.B;
                    break;
                case 'B':
                    port.BaudRate = 1200;
                    Mode = ProtocolMode.B;
                    break;
                case 'C':
                    port.BaudRate = 2400;
                    Mode = ProtocolMode.B;
                    break;
                case 'D':
                    port.BaudRate = 4800;
                    Mode = ProtocolMode.B;
                    break;
                case 'E':
                    port.BaudRate = 9600;
                    Mode = ProtocolMode.B;
                    break;
                case 'F':
                    port.BaudRate = 19200;
                    Mode = ProtocolMode.B;
                    break;
                case 'G':
                    port.BaudRate = 38400;
                    Mode = ProtocolMode.B;
                    break;
                case 'H':
                    port.BaudRate = 76800;
                    Mode = ProtocolMode.B;
                    break;
                case 'I':
                    port.BaudRate = 153600;
                    Mode = ProtocolMode.B;
                    break;

                default:
                    throw new ArgumentException("Ответ неподходящего формата. Код скорости обмена.");
            }

            Identifier = ASCIIEncoding.ASCII.GetString(buffer, 5, length - 5 - 2);
        }

        /// <summary>
        /// Получает тело блока данных из информационного сообщения (ответа на ф-цию чтения в режиме программирования)
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Длина буфера ответа меньше заявленной длины
        /// or
        /// Длина ответа меньше минимальной длины
        /// or
        /// Формат ответа некорректный
        /// or
        /// Формат ответа некорректный
        /// or
        /// BCC не сходится
        /// </exception>
        public string GetDataBlockInfoMessage(ref byte[] buffer, int length)
        {
            if (buffer.Length < length)
                throw new ArgumentException("Длина буфера ответа меньше заявленной длины");
            if (length < 3)
                throw new ArgumentException("Длина ответа меньше минимальной длины");
            if (buffer[0] != (byte)AsciiControlSymbols.STX)
                throw new ArgumentException("Формат ответа некорректный");
            if ((buffer[length - 2] != (byte)AsciiControlSymbols.ETX) && ((buffer[length - 2] != (byte)AsciiControlSymbols.EOT)))
                throw new ArgumentException("Формат ответа некорректный");

            byte bcc = GetBCC(ref buffer, 1, length - 1 - 1);
            if (bcc != buffer[length - 1])
                throw new ArgumentException("BCC не сходится");

            return ASCIIEncoding.ASCII.GetString(buffer, 1, length - 2 - 1);
        }

        /// <summary>
        /// Парсинг блока данных
        /// </summary>
        /// <param name="dataBlock">The data block.</param>
        /// <returns></returns>
        public DataSet[] GetDataSetsFromDataBlock(string dataBlock)
        {
            var dataStrings = dataBlock
                .Split(new string[] { "\r\n" },
                StringSplitOptions.RemoveEmptyEntries);

            DataSet[] ds = new DataSet[dataStrings.Length];

            for (int i = 0; i < dataStrings.Length; i++)
            {
                var s = dataStrings[i];
                var leftBracket = s.IndexOf('(');
                var rightBracket = s.IndexOf(')');
                var asterisk = s.IndexOf('*');

                ds[i].Address = s.Substring(0, leftBracket);

                if (asterisk == -1) // без устройства
                {
                    ds[i].Value = s.Substring(leftBracket + 1, rightBracket - (leftBracket + 1));
                }
                else
                {
                    ds[i].Value = s.Substring(leftBracket + 1, asterisk - (leftBracket + 1));
                    ds[i].Device = s.Substring(asterisk + 1, rightBracket - (asterisk + 1));
                }
            }

            return ds;
        }
        
        public void InitSession(string address = "")
        {
            const int minSleepMs = 2;
            int waitMs = 1500; //максимально возможный интервал ожидания
            int symMaxWaitMs = 12; //максимально возможный интервал ожидания следующего символа
            int i = 0;

            if (!port.IsOpen) port.Open();

            // посылвем запрос (стартуем сессию)
            int len = MakeInit(ref buffer, address);
            port.Write(buffer, 0, len);

            Thread.Sleep(21); // минимальное обязательное ожидание + 1
            //дополнительное ожидание до максимально возможного ожидания
            for(int c = 21; (c < waitMs) || (port.BytesToRead > 0); c += minSleepMs)
                Thread.Sleep(minSleepMs);

            if (port.BytesToRead == 0)
                throw new Exception("Устройство не отвечает");

            // ждем полной загрузки заголовка ответа
            if (port.BytesToRead < 5)
                Thread.Sleep((5 - port.BytesToRead) * symMaxWaitMs);

            if (port.BytesToRead < 5)
                throw new Exception("Устройство отвечает слишком медленно");

            if (port.ReadByte() != (int)'/')
                throw new Exception("Устройство отвечает не корректно");

            // читаем код производителя и определяем поддерживаемую задержку между запросом и ответом
            port.Read(buffer, 0, 3);
            ManufacturerCode = ASCIIEncoding.ASCII.GetString(buffer, 0, 3);
            char lmc = ManufacturerCode[2];// последний символ кода

            if ((lmc> (byte)'a') && (lmc < (byte)'z'))
                Tr = 20;
            else if ((lmc > (byte)'A') && (lmc < (byte)'Z'))
                Tr = 200;
            else
                throw new ArgumentException("Ответ неподходящего формата. Код производителя.");

            // читаем скорость передачи и предполагаемый режим
            int z = port.ReadByte();
            int preBaudRate = port.BaudRate;
            ProtocolMode preMode;
            switch (z)
            {
                case '0':
                    preBaudRate = 300;
                    preMode = ProtocolMode.C;
                    break;
                case '1':
                    preBaudRate = 600;
                    preMode = ProtocolMode.C;
                    break;
                case '2':
                    preBaudRate = 1200;
                    preMode = ProtocolMode.C;
                    break;
                case '3':
                    preBaudRate = 2400;
                    preMode = ProtocolMode.C;
                    break;
                case '4':
                    preBaudRate = 4800;
                    preMode = ProtocolMode.C;
                    break;
                case '5':
                    preBaudRate = 9600;
                    preMode = ProtocolMode.C;
                    break;
                case '6':
                    preBaudRate = 19200;
                    preMode = ProtocolMode.C;
                    break;
                case '7':
                    preBaudRate = 38400;
                    preMode = ProtocolMode.C;
                    break;
                case '8':
                    preBaudRate = 76800;
                    preMode = ProtocolMode.C;
                    break;
                case '9':
                    preBaudRate = 153600;
                    preMode = ProtocolMode.C;
                    break;

                case 'A':
                    preBaudRate = 600;
                    preMode = ProtocolMode.B;
                    break;
                case 'B':
                    preBaudRate = 1200;
                    preMode = ProtocolMode.B;
                    break;
                case 'C':
                    preBaudRate = 2400;
                    preMode = ProtocolMode.B;
                    break;
                case 'D':
                    preBaudRate = 4800;
                    preMode = ProtocolMode.B;
                    break;
                case 'E':
                    preBaudRate = 9600;
                    preMode = ProtocolMode.B;
                    break;
                case 'F':
                    preBaudRate = 19200;
                    preMode = ProtocolMode.B;
                    break;
                case 'G':
                    preBaudRate = 38400;
                    preMode = ProtocolMode.B;
                    break;
                case 'H':
                    preBaudRate = 76800;
                    preMode = ProtocolMode.B;
                    break;
                case 'I':
                    preBaudRate = 153600;
                    preMode = ProtocolMode.B;
                    break;

                default:
                    throw new ArgumentException("Ответ неподходящего формата. Код скорости обмена.");
            }

            // читаем идентификатор до <cr><lf>
            len = 0;
            byte b = 0;
            for (int c = 0; c < symMaxWaitMs; c += minSleepMs){                
                if (port.BytesToRead > 0)
                {
                    c = 0;
                    b = (byte)port.ReadByte();
                    buffer[len++] = b;
                    if (b == (byte)AsciiControlSymbols.LF)
                        break;
                }
                else
                    Thread.Sleep(minSleepMs);
            }
            Identifier = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 2);

            // определяем режим A, B, или С
            if (port.BytesToRead == 0)
                Thread.Sleep(minSleepMs);
            if (port.BytesToRead > 0)
                Mode = ProtocolMode.A;
            else
                Mode = preMode;

            // меняем скорость если попался режим B или С
            if ((preBaudRate != port.BaudRate) && ((preMode == ProtocolMode.B) || (preMode == ProtocolMode.C)))
            {
                port.Close();
                port.BaudRate = preBaudRate;
                port.Open();
            }

            // читаем данные для режимов A или B
            if ((preMode == ProtocolMode.A) || (preMode == ProtocolMode.B))
            {

                b = (byte)port.ReadByte();
            }

            //для режима B меняем скорость обратно
            if ((Mode == ProtocolMode.B) && (port.BaudRate != startBaudRate))
            {
                port.Close();
                port.BaudRate = startBaudRate;
                port.Open();
            }

        }


    }
}

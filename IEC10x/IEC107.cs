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
        /// Метод кодирования данных для команды
        /// </summary>
        public enum CommandDataEncoding : byte
        {
            Reserved = (byte)'0',
            Ascii = (byte)'1',
            Formated = (byte)'2',
            AsciiIncomplete = (byte)'3',
            FormatedIncomplete = (byte)'4',
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
        /// Состояние текущего подключения
        /// </summary>
        public enum SessionState:int
        {
            Disconnected = 0,
            Innitialised = 1,
            AcessLevel2 = 2, // Пароль уровня 2 введен
            /* 
             * AcessLevel3, // Нажатие на охранную кнопку Или "секретные манипуляции с данными"
             * AcessLevel4, // Переключатель внутри корпуса устройства
             */
        }

        /// <summary>
        /// Набор данных из строка данных из блока данных
        /// </summary>
        public struct DataSet
        {
            public string Address;
            public string Value;
            public string Device;
        }

        private SerialPort port;

        private int basicBaudRate = 300;

        private TimeSpan minTimeToReset = new TimeSpan(0, 1, 0);

        private byte[] buffer = new byte[4096];

        /// <summary>
        /// Минимальное время между последним байтом запроса и первым байтом ответа
        /// </summary>
        private int trMin = 20 / 4;

        /// <summary>
        /// Заявленное производителем время ответа
        /// </summary>
        private int tr = 200;

        /// <summary>
        /// Максимальное время между последним байтом запроса и первым байтом ответа
        /// </summary>
        private int trMax = 1500;

        private DateTime lastSessUpdate;

        private ProtocolMode mode;

        private SessionState _sessState = SessionState.Disconnected;

        public SessionState SessState {
            get {
                if ((lastSessUpdate + minTimeToReset) > DateTime.Now)
                    _sessState = SessionState.Disconnected;
                 
                return _sessState;
            }
            private set { _sessState = value; }
        }

        public string Password { get; set; }

        public string BasicDeviceAddress { get; private set; }

        public string ManufacturerCode { get; private set; }

        public string Identifier { get; private set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public List<DataSet> Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IEC107"/> class.
        /// </summary>
        /// <param name="serialPort">The serial port.</param>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="startBaudRate">The start baud rate.</param>
        public IEC107(SerialPort port, string deviceAddress = "")
        {
            this.port = port;
            if (port.IsOpen) port.Close();            
            this.basicBaudRate = port.BaudRate;
            this.BasicDeviceAddress = deviceAddress;
            port.Open();
        }

        /// <summary>
        /// Ожидает Count доступных байтов в порту для чтения в течении TrMax миллисекунд.
        /// </summary>
        /// <param name="count">Кол-во ожидаемых байтов</param>
        /// <returns>True - дождались, False - время истекло</returns>
        private bool WaitForBytes(int count)
        {
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

        private byte ReadByte()
        {
            if (!port.IsOpen) port.Open();
            WaitForBytes(1);
            return (byte)port.ReadByte();
        }

        /// <summary>
        /// Записывает буфер в порт
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="Exception">Write(..). Длина буфера меньше требуемой длины.</exception>
        private void Write(ref byte[] buffer, int offset, int count)
        {
            if (buffer.Length < (offset + count))
                throw new Exception("Write(..). Длина буфера меньше требуемой длины.");

            port.Write(buffer, offset, count);
            lastSessUpdate = DateTime.Now;
        }

        /// <summary>
        /// Sets the baud rate.
        /// </summary>
        /// <param name="newBaudRate">The new baud rate.</param>
        private void SetBaudRate(int newBaudRate)
        {
            if (port.BaudRate == newBaudRate) return;
            var isOpen = port.IsOpen;
            if (isOpen) port.Close();
            port.BaudRate = newBaudRate;
            if (isOpen) port.Open();
        }

        /// <summary>
        /// Получает символ скорости для режима "C"
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <returns>символ скорости</returns>
        private byte GetBaudRateSymbolC(int baudRate)
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
        private byte GetBCC(ref byte[] buffer, int startindex, int length)
        {
            if (buffer.Length < length)
                throw new ArgumentOutOfRangeException("Указанная дина меньше длины буфера.");

            byte sum = 0;
            for (int i = startindex; i < length; i++)
                sum += buffer[i];
            return (byte)(sum & 0x7F);
        }

        /// <summary>
        /// Fits the length of the buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        private void FitBufferLength(ref byte[] buffer, int length)
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
        private int MakeInit(ref byte[] buffer, string address = "")
        {
            if (address.Length > 32)
                throw new ArgumentOutOfRangeException("Параметр address должен быть меньше 32-х символов");

            int length = 5 + address.Length;
            FitBufferLength(ref buffer, length);

            int i = 0;

            buffer[i++] = (byte)'/';
            buffer[i++] = (byte)'?';

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
        /// <param name="ctrlSym">'0' - данные, '1' - программирование, остальные - на усмотрение изготовителя</param>
        /// <param name="baudRate">The baud rate. 0 - don`t change (Use this.BaudRate)</param>
        /// <returns>buffer length</returns>
        private int MakeAck(ref byte[] buffer, bool isSecondary = false, char ctrlSym = '0', int baudRate = 0)
        {
            int length = 6, i = 0;
            FitBufferLength(ref buffer, length);
            baudRate = baudRate > 0 ? baudRate : port.BaudRate;
            buffer[i++] = (byte)AsciiControlSymbols.ACK;
            buffer[i++] = (byte)(isSecondary ? '1' : '0');
            buffer[i++] = GetBaudRateSymbolC(baudRate);
            buffer[i++] = (byte)ctrlSym;
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
        private int MakeCommand(ref byte[] buffer, CommandSymbbols command, char commandType = '0', string data = "", bool isFullBlocks = true)
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
            buffer[i++] = GetBCC(ref buffer, 1, i - 2);
            return i;
        }

        /// <summary>
        /// Парсинг блока данных
        /// </summary>
        /// <param name="dataBlock">The data block.</param>
        /// <returns></returns>
        private List<DataSet> GetDataSetsFromDataBlock(string dataBlock)
        {
            var dataStrings = dataBlock
                .Split(new string[] { "\r\n" },
                StringSplitOptions.RemoveEmptyEntries);

            List<DataSet> ds = new List<DataSet>(dataStrings.Length);

            for (int i = 0; i < dataStrings.Length; i++)
            {
                var s = dataStrings[i];
                var leftBracket = s.IndexOf('(');
                var rightBracket = s.IndexOf(')');
                var asterisk = s.IndexOf('*');
                DataSet d = new DataSet();
                d.Address = s.Substring(0, leftBracket);

                if (asterisk == -1) // без устройства
                {
                    d.Value = s.Substring(leftBracket + 1, rightBracket - (leftBracket + 1));
                }
                else
                {
                    d.Value = s.Substring(leftBracket + 1, asterisk - (leftBracket + 1));
                    d.Device = s.Substring(asterisk + 1, rightBracket - (asterisk + 1));
                }

                ds.Add(d);
            }

            return ds;
        }

        /// <summary>
        /// Initializes the session.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <exception cref="Exception">
        /// Устройство отвечает не корректно
        /// or
        /// Устройство не отвечает
        /// or
        /// Устройство отвечает слишком медленно
        /// or
        /// Не удалось получить идентификатор.
        /// or
        /// or
        /// Не удалось получить данные при идентификации.
        /// or
        /// Не удалось получить данные при идентификации.
        /// or
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Ответ неподходящего формата. Код производителя.
        /// or
        /// Ответ неподходящего формата. Код скорости обмена.
        /// </exception>
        private void InitSession(string address = "")
        {
            if (!port.IsOpen) port.Open();

            // посылаем запрос (стартуем сессию)
            int len = MakeInit(ref buffer, address);
            port.Write(buffer, 0, len);

            // ждем начала ответа
            if (WaitForBytes(5))
            {
                if (port.ReadByte() != (int)'/')
                    throw new Exception("Устройство отвечает не корректно");
            }
            else
            {
                if (port.BytesToRead == 0)
                    throw new Exception("Устройство не отвечает");
                else
                    throw new Exception("Устройство отвечает слишком медленно");
            }

            // читаем код производителя и определяем поддерживаемую задержку между запросом и ответом
            port.Read(buffer, 0, 3);
            ManufacturerCode = ASCIIEncoding.ASCII.GetString(buffer, 0, 3);
            char lmc = ManufacturerCode[2];// последний символ кода

            if ((lmc > (byte)'a') && (lmc < (byte)'z'))
                tr = 20;
            else if ((lmc > (byte)'A') && (lmc < (byte)'Z'))
                tr = 200;
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
            while (b != (byte)AsciiControlSymbols.LF)
            {
                if (WaitForBytes(1))
                {
                    b = (byte)port.ReadByte();
                    buffer[len++] = b;
                }
                else
                    throw new Exception("Не удалось получить идентификатор.");
            }
            Identifier = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 2);

            // определяем режим A, B, или С
            if (WaitForBytes(1))
                mode = ProtocolMode.A;
            else
                mode = preMode;

            // меняем скорость если попался режим B или С
            if ((mode == ProtocolMode.B) || (mode == ProtocolMode.C))
                SetBaudRate(preBaudRate);

            // посылаем команду на чтения данных для режима С
            if (mode == ProtocolMode.C)
            {
                len = MakeAck(ref buffer, false, '0');
                port.Write(buffer, 0, len);
            }

            // читаем данные для режимов A или B или C
            if (WaitForBytes(7))// STX MinData ! CR LF ETX BCC
            {
                b = (byte)port.ReadByte();
                if (b != (byte)AsciiControlSymbols.STX)
                    throw new Exception($"Wrong start symbol for mode {preMode}.");

                len = 0;
                while (b != (byte)AsciiControlSymbols.ETX)
                {
                    if (WaitForBytes(1))
                    {
                        b = (byte)port.ReadByte();
                        buffer[len++] = b;
                    }
                    else
                        throw new Exception("Не удалось получить данные при идентификации.");
                }

                if (WaitForBytes(1))// Контрольная сумма
                    b = (byte)port.ReadByte();
                else
                    throw new Exception("Не удалось получить данные при идентификации.");

                byte bcc = GetBCC(ref buffer, 0, len); // Без STX и ETX
                if (b != bcc)
                    throw new Exception($"Контрольная сумма не сходится {b}!={bcc}.");

                string dataBlockStr = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 5); // Без STX ! CR LF ETX ( BCC вне буфера ).
                Data = GetDataSetsFromDataBlock(dataBlockStr);
            }
            else
                throw new Exception("Нет тарифных данных.");

            //для режима B меняем скорость обратно
            if (mode == ProtocolMode.B)
                SetBaudRate(basicBaudRate);

            lastSessUpdate = DateTime.Now;
            SessState = SessionState.Innitialised;
        }//InitSession

        /// <summary>
        /// Читает из port и разбирает информационное сообщение. В том числе и многостраничные.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">
        /// Ошибка разбора InfoMessage. Нет данных.
        /// or
        /// Ошибка разбора InfoMessage. Нет STX.
        /// or
        /// Ошибка разбора InfoMessage. Нет достигнут ETX.
        /// or
        /// Ошибка разбора InfoMessage. Нет BCC.
        /// or
        /// </exception>
        private List<DataSet> ReadProgInfoMessage()
        {   // STX (D..D) ETX||EOT BCC
            // (D..D) блок данных == [строки данных] разделение по CR LF
            // строка данных == набор данных: "адрес ( данные * устройство )"
            if (!WaitForBytes(2))
                throw new Exception("Ошибка разбора InfoMessage. Нет данных.");

            byte b = (byte)port.ReadByte();
            if (b != (byte)AsciiControlSymbols.STX)
                throw new Exception("Ошибка разбора InfoMessage. Нет STX.");

            int len = 0;
            while ((b != (byte)AsciiControlSymbols.ETX) || (b != (byte)AsciiControlSymbols.EOT))
                if (WaitForBytes(1))
                {
                    b = (byte)port.ReadByte();
                    buffer[len++] = b;
                }
                else
                    throw new Exception("Ошибка разбора InfoMessage. Нет достигнут ETX.");

            if (WaitForBytes(1))
                b = (byte)port.ReadByte();
            else
                throw new Exception("Ошибка разбора InfoMessage. Нет BCC.");

            byte bcc = GetBCC(ref buffer, 1, len - 2);
            if (b != bcc)
                throw new Exception($"Ошибка разбора InfoMessage. Не совпадает BCC: {bcc} != {b} .");

            var str = ASCIIEncoding.ASCII.GetString(buffer, 1, len - 1 - 1);

            List<DataSet> ds = GetDataSetsFromDataBlock(str);

            if (buffer[len - 1] == (byte)AsciiControlSymbols.EOT)
            {
                port.Write(new byte[] { (byte)AsciiControlSymbols.ACK }, 0, 1);
                List<DataSet> dsPartal = ReadProgInfoMessage();
                ds.AddRange(dsPartal);
            }

            return ds;
        }

        /// <summary>
        /// Перевод устройства в режим программирования
        /// </summary>
        /// <param name="Зassword">The password.</param>
        /// <exception cref="Exception">
        /// </exception>
        private void ToProgram()
        {
            int len;
            if (mode == ProtocolMode.C)
            {
                // Для режима С нужен сперва запрос
                len = MakeAck(ref buffer, false, '1');
                port.Write(buffer, 0, len);

                if (!WaitForBytes(3))// SOH P 0
                    throw new Exception($"Переход в режим программирования. Нет запроса пароля.");
                var dss = ReadProgInfoMessage();
                if ((dss?.Count ?? 0) == 0)
                    throw new Exception($"Переход в режим программирования. Не корректный запрос пароля.");
                var sn = dss[0].Value;
            }
            
            // Ввод пароля
            len = MakeCommand(ref buffer, CommandSymbbols.Password, '1', $"({Password})");
            port.Write(buffer, 0, len);

            if (!WaitForBytes(1))// ACK || NAK || SOH B 0 ETX BCC
                throw new Exception($"Переход в режим программирования. Нет запроса пароля.");

            buffer[0] = (byte)port.ReadByte();
            if (buffer[0] != (byte)AsciiControlSymbols.ACK)
                throw new Exception($"Переход в режим программирования. Пароль не подходит.");

            // Теперь устройство в Авторизовано
            SessState = SessionState.AcessLevel2;
        }

        /// <summary>
        /// Запись строки в буфер
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        protected virtual int StringToBuffer(ref byte[] buffer, int offset, string str)
        {
            int i = offset;
            foreach(char c in str) {
                buffer[i++] = (byte)c;
            }
            return i - offset;
        }

        /// <summary>
        /// Запись набора данных в буфер
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="dataSet">The data set.</param>
        /// <returns></returns>
        protected virtual int MakeDataSetAscii(ref byte[] buffer, int offset, DataSet dataSet) {
            int i = offset;
            i += StringToBuffer(ref buffer, i, dataSet.Address);
            if (!string.IsNullOrEmpty(dataSet.Value)) {
                buffer[i++] = (byte)'(';
                i += StringToBuffer(ref buffer, i, dataSet.Value);
                if (!string.IsNullOrEmpty(dataSet.Device)) {
                    buffer[i++] = (byte)'*';
                    i += StringToBuffer(ref buffer, i, dataSet.Device);
                }
                buffer[i++] = (byte)')';
            }
            return i - offset;
        }

        /// <summary>
        /// Сбор буфера для информационного сообщения
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="dataSet">The data set.</param>
        /// <param name="cde">The cde.</param>
        /// <returns>кол-во байтов записаных в буфер</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual int MakeProgInfoMessageRequest(ref byte[] buffer, int offset,  DataSet dataSet, CommandDataEncoding cde)
        { // STX DataSet ETX||EOT BCC
            int i = offset;
            buffer[i++] = (byte)AsciiControlSymbols.STX;

            switch (cde) {
                case CommandDataEncoding.Ascii:
                    i += MakeDataSetAscii(ref buffer, i, dataSet);
                    buffer[i++] = (byte)AsciiControlSymbols.ETX;
                    break;                   
                case CommandDataEncoding.AsciiIncomplete:
                case CommandDataEncoding.Reserved:
                case CommandDataEncoding.Formated:
                case CommandDataEncoding.FormatedIncomplete:                    
                default:
                    throw new NotImplementedException($"Тип кодирования {(char)cde} не реализован.");
            }

            int len = i - offset;
            byte bcc = GetBCC(ref buffer, offset + 1, len - 2);
            buffer[i++] = bcc;
            return i - offset;
        }

        /// <summary>
        /// Команда чтения
        /// </summary>
        /// <param name="address">The data.</param>
        /// <param name="data">The data.</param>
        /// <param name="cde">The Command Data Encoding</param>
        /// <returns></returns>
        public List<DataSet> CmdR(DataSet dataSet, CommandDataEncoding cde = CommandDataEncoding.Ascii)
        {
            if (SessState == SessionState.Disconnected)
                InitSession();

            if (SessState != SessionState.AcessLevel2)
                ToProgram();

            int i = 0;
            buffer[i++] = (byte)AsciiControlSymbols.SOH;
            buffer[i++] = (byte)CommandSymbbols.Read;
            buffer[i++] = (byte)cde;
            i += MakeProgInfoMessageRequest(ref buffer, i, dataSet, cde);
            Write(ref buffer, 0, i);
            return ReadProgInfoMessage();
        }

        /// <summary>
        /// Закрываем сессию.
        /// </summary>
        public void Bye()
        {
            int len = MakeCommand(ref buffer, CommandSymbbols.Bye);
            Write(ref buffer, 0, len);

            SessState = SessionState.Disconnected;
            port.Close();
        }
    }
}

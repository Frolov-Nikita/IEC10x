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
        /// file separator
        /// 
        /// </summary>
        FS = 0x1C,
        /// <summary>
        /// group separator
        /// 
        /// </summary>
        GS = 0x1D,
        /// <summary>
        /// record separator
        /// 
        /// </summary>
        RS = 0x1E,
        /// <summary>
        /// unit separator
        /// 
        /// </summary>
        US = 0x1F,
        /// <summary>
        /// delete
        /// стереть последний символ
        /// </summary>
        DEL = 0x7F,
    }

    public delegate void IEC107Comm(IEC107PortCommDirection direction, byte[] buffer);

    public abstract class IEC107
    {
        protected byte[] buffer = new byte[4096];

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

        // TODO добавить полноценную поддержку
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
        /// Состояние текущего подключения
        /// </summary>
        public enum SessionState:int
        {
            Disconnected = 0,
            Innitialised = 1,
            Program = 2, // Пароль уровня 2 введен
            /* 
             * AcessLevel3, // Нажатие на охранную кнопку Или "секретные манипуляции с данными"
             * AcessLevel4, // Переключатель внутри корпуса устройства
             */
        }

        /// <summary>
        /// Набор данных из строка данных из блока данных
        /// </summary>
        public class DataSet
        {
            public string Address;
            public string Value;
            public string Device;

            public static DataSet Parse(string str)
            {
                str = str.Trim();
                if (string.IsNullOrEmpty(str))
                    return null;
                var leftBracket = str.IndexOf('(');
                var rightBracket = str.IndexOf(')');
                var asterisk = str.IndexOf('*');
                DataSet dataSet = new DataSet();
                dataSet.Address = str.Substring(0, leftBracket);
                if (asterisk == -1) // без устройства
                    dataSet.Value = str.Substring(leftBracket + 1, rightBracket - (leftBracket + 1));
                else
                { // с устройством
                    dataSet.Value = str.Substring(leftBracket + 1, asterisk - (leftBracket + 1));
                    dataSet.Device = str.Substring(asterisk + 1, rightBracket - (asterisk + 1));
                }
                return dataSet;
            }

            public override string ToString() =>
                Address + "(" + Value + (string.IsNullOrEmpty(Device) ?"":"*" + Device) + ")";

            public string ToString(CommandDataEncoding cde)
            {
                switch (cde)
                {
                    case CommandDataEncoding.Reserved:
                    case CommandDataEncoding.Ascii:
                    case CommandDataEncoding.AsciiIncomplete:
                        return ToString();
                    case CommandDataEncoding.Formated:
                    case CommandDataEncoding.FormatedIncomplete:
                    default:
                        throw new NotImplementedException();
                }                
            }

            public int ToBuffer(ref byte[] buffer, int offset, CommandDataEncoding cde) {
                var str = ToString(cde);
                return ASCIIEncoding.ASCII.GetBytes(str, 0, str.Length, buffer, offset);
            }
        }

        /// <summary>
        /// Блок данных
        /// </summary>
        /// <seealso cref="System.Collections.Generic.List{IEC10x.IEC107.DataSet}" />
        public class DataBlock : List<DataSet>
        {
            public AsciiControlSymbols ControlSymbol { get; set; }

            public DataBlock(AsciiControlSymbols controlSymbol) : base()
            {
                this.ControlSymbol = controlSymbol;
            }

            public DataBlock() : base()
            { }
            public DataBlock(int capacity = 0) : base(capacity)
            { }

            public static DataBlock Parse(string str)
            {
                str = str.Trim();
                if (string.IsNullOrEmpty(str))
                    return null;
                var dataStrings = str.Split(new string[] { "\r\n" },
                    StringSplitOptions.RemoveEmptyEntries);

                var dataBlock = new DataBlock(dataStrings.Length);

                foreach (var ds in dataStrings) {
                    var newDataSet = DataSet.Parse(ds);
                    if(newDataSet != null)
                        dataBlock.Add(newDataSet);
                }
                    
                return dataBlock;
            }

            public override string ToString() {
                string r = "";
                foreach(var ds in this)
                    r += ds.ToString() + "\r\n";
                return r;
            }

            public int ToBuffer(ref byte[] buffer, int offset)
            {
                var str = ToString();
                return ASCIIEncoding.ASCII.GetBytes(str, 0, str.Length, buffer, offset);
            }
        }

        /// <summary>
        /// Команда
        /// </summary>
        public class Command
        {
            public CommandSymbbols Cmd;
            public CommandDataEncoding Cde;
            public DataSet DataSet;

            public int ToBuffer(ref byte[] buffer, int offset = 0)
            {
                int i = offset;
                buffer[i++] = (byte)AsciiControlSymbols.SOH;
                buffer[i++] = (byte)Cmd;
                buffer[i++] = (byte)Cde;

                if(DataSet != null)
                {
                    buffer[i++] = (byte)AsciiControlSymbols.STX;
                    i += DataSet.ToBuffer(ref buffer, i, Cde);
                }

                switch (Cde)
                {
                    case CommandDataEncoding.Reserved:
                    case CommandDataEncoding.Ascii:
                    case CommandDataEncoding.AsciiIncomplete:
                        buffer[i++] = (byte)AsciiControlSymbols.ETX;
                        break;
                    case CommandDataEncoding.Formated:
                    case CommandDataEncoding.FormatedIncomplete:
                    default:
                        buffer[i++] = (byte)AsciiControlSymbols.EOT;
                        break;
                }

                buffer[i++] = GetBCC(ref buffer, offset + 1, i - offset - 1);
                return i - offset;
            }
        }

        protected IEC107Port port;
        
        private TimeSpan minTimeToReset = new TimeSpan(0, 1, 0);

        private DateTime lastSessUpdate;
        
        private SessionState _sessState = SessionState.Disconnected;

        public SessionState SessState {
            get {
                if (((lastSessUpdate + minTimeToReset) < DateTime.Now) || (!port.IsOpen))
                    _sessState = SessionState.Disconnected;
                 
                return _sessState;
            }
            protected set {
                _sessState = value;
                lastSessUpdate = DateTime.Now;
            }
        }
        
        public string DeviceAddress { get; protected set; }

        public string ManufacturerCode { get; private set; }

        public string Identifier { get; private set; }

        public int BaudRateInIdentifier = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="IEC107"/> class.
        /// </summary>
        /// <param name="serialPort">The serial port.</param>
        /// <param name="deviceAddress">The device address.</param>
        /// <param name="startBaudRate">The start baud rate.</param>
        public IEC107(SerialPort port, string deviceAddress = "")
        {
            this.port = new IEC107Port(port);
            DeviceAddress = deviceAddress;
            this.port.OnCommunicate += Port_OnCommunicate;
        }

        public event IEC107Comm OnCommunicate;

        private void Port_OnCommunicate(IEC107PortCommDirection direction, byte[] buffer, int offset, int length)
        {
            if ((OnCommunicate == null) ||
                (buffer == null) ||
                (length <= 0) ||
                (buffer.Length - offset < length))
                return;

            var bufferCopy = new byte[length];
            for (var i = 0; i < length; i++)
                bufferCopy[i] = buffer[i + offset];

            OnCommunicate?.Invoke(direction, bufferCopy);
        }

        /// <summary>
        /// Получает символ скорости для режима "C"
        /// </summary>
        /// <param name="baudRate">The baud rate.</param>
        /// <returns>символ скорости</returns>
        private byte BaudRateToChar(int baudRate)
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
        /// Расшифровывает символ скорости обмена порта
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Ответ неподходящего формата. Код скорости обмена.</exception>
        private int BoudRateFromChar(char c)
        {
            switch (c)
            {
                // для режима C
                case '0':
                    return 300;
                case '1':
                    return 600;
                case '2':
                    return 1200;
                case '3':
                    return 2400;
                case '4':
                    return 4800;
                case '5':
                    return 9600;
                case '6':
                    return 19200;
                case '7':
                    return 38400;
                case '8':
                    return 76800;
                case '9':
                    return 153600;
                // для режима B
                case 'A':
                    return 600;
                case 'B':
                    return 1200;
                case 'C':
                    return 2400;
                case 'D':
                    return 4800;
                case 'E':
                    return 9600;
                case 'F':
                    return 19200;
                case 'G':
                    return 38400;
                case 'H':
                    return 76800;
                case 'I':
                    return 153600;

                default:
                    throw new ArgumentException("Ответ неподходящего формата. Код скорости обмена.");
            }
        }

        /// <summary>
        /// Байт контрольной суммы
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="ArgumentOutOfRangeException">Указанная дина меньше длины буфера.</exception>
        protected static byte GetBCC(ref byte[] buffer, int offset, int length)
        {
            if (buffer.Length < length)
                throw new ArgumentOutOfRangeException("Указанная дина меньше длины буфера.");

            byte sum = 0;
            for (int i = offset; i < length; i++)
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
        /// <param name="deviceAddress">The address.</param>
        /// <returns>count of buffer bytes</returns>
        /// <exception cref="ArgumentOutOfRangeException">Параметр address должен быть меньше 32-х символов</exception>
        private int MakeInit(ref byte[] buffer, string deviceAddress = "")
        {
            if (deviceAddress.Length > 32)
                throw new ArgumentOutOfRangeException("Параметр address должен быть меньше 32-х символов");

            int length = 5 + deviceAddress.Length;
            FitBufferLength(ref buffer, length);

            int i = 0;

            buffer[i++] = (byte)'/';
            buffer[i++] = (byte)'?';

            foreach (char c in deviceAddress)
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
        protected int MakeAck(ref byte[] buffer, bool isSecondary = false, char ctrlSym = '0', int baudRate = 0)
        {
            int length = 6, i = 0;
            FitBufferLength(ref buffer, length);
            baudRate = baudRate > 0 ? baudRate : port.BaudRate;
            buffer[i++] = (byte)AsciiControlSymbols.ACK;
            buffer[i++] = (byte)(isSecondary ? '1' : '0');
            buffer[i++] = BaudRateToChar(baudRate);
            buffer[i++] = (byte)ctrlSym;
            buffer[i++] = (byte)AsciiControlSymbols.CR;
            buffer[i++] = (byte)AsciiControlSymbols.LF;
            return length;
        }

        /// <summary>
        /// Initializes the session.
        /// </summary>
        /// <param name="deviceAddress">The address.</param>
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
        protected void StartSession(string deviceAddress = "")
        {
            if (SessState != SessionState.Disconnected)
                CmdB();

            if (!port.IsOpen)
                port.Open();

            // посылаем запрос (стартуем сессию)
            int len = MakeInit(ref buffer, deviceAddress);
            port.Write(ref buffer, 0, len);

            if (port.ReadByte() != (int)'/')
                throw new Exception("Устройство отвечает не корректно");

            // читаем код производителя
            port.Read(ref buffer, 0, 3);
            ManufacturerCode = ASCIIEncoding.ASCII.GetString(buffer, 0, 3);

            // читаем скорость передачи
            BaudRateInIdentifier = BoudRateFromChar((char)port.ReadByte());

            // читаем идентификатор до <cr><lf>
            len = port.Read(ref buffer, 0, b => b == (byte)AsciiControlSymbols.LF);
            Identifier = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 2);

            SessState = SessionState.Innitialised;
        }

        /// <summary>
        /// Прочитать информационное сообщение (кроме режима программирования)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">
        /// Ошибка разбора InfoMessage. Нет STX.
        /// or
        /// </exception>
        protected DataBlock ReadInfoMessage()
        {   // STX (D..D) ! CR LF ETX BCC
            // (D..D) блок данных == [строки данных] разделение по CR LF
            // строка данных == набор данных: "адрес ( данные * устройство )"

            byte b = (byte)port.ReadByte();
            if (b == (byte)AsciiControlSymbols.NAK)
                return new DataBlock(AsciiControlSymbols.NAK);
            if (b != (byte)AsciiControlSymbols.STX)
                throw new Exception("Ошибка разбора InfoMessage. Нет STX.");

            int len = port.Read(ref buffer, 0, c => c == (byte)'!');

            len += port.Read(ref buffer, len, 4); //CR LF ETX BCC
            
            byte bcc = GetBCC(ref buffer, 0, len -1);
            if (buffer[len - 1] != bcc)
                throw new Exception($"Ошибка разбора InfoMessage. Не совпадает BCC: {bcc} != {b} .");

            var str = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 5);

            return DataBlock.Parse(str);
        }

        /// <summary>
        /// Прочитать информационное сообщение для режима программирования. В том числе и многостраничные.
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
        private DataBlock ReadProgInfoMessage()
        {   // STX (D..D) ETX||EOT BCC
            // (D..D) блок данных == [строки данных] разделение по CR LF
            // строка данных == набор данных: "адрес ( данные * устройство )"
 
            byte b = port.ReadByte();
            if (b == (byte)AsciiControlSymbols.ACK)
                return new DataBlock(AsciiControlSymbols.ACK);
            if (b == (byte)AsciiControlSymbols.NAK)
                return new DataBlock(AsciiControlSymbols.NAK);
            if (b != (byte)AsciiControlSymbols.STX)
                throw new Exception("Ошибка разбора InfoMessage. Нет STX.");

            int len = port.Read(ref buffer, 0, c => (c == (byte)AsciiControlSymbols.ETX) || (c == (byte)AsciiControlSymbols.EOT));

            b = port.ReadByte();

            byte bcc = GetBCC(ref buffer, 0, len);
            if (b != bcc)
                throw new Exception($"Ошибка разбора InfoMessage. Не совпадает BCC: {bcc} != {b} .");

            var str = ASCIIEncoding.ASCII.GetString(buffer, 0, len - 1);

            var ds = DataBlock.Parse(str);

            if (buffer[len - 1] == (byte)AsciiControlSymbols.EOT)
            {
                port.WriteByte((byte)AsciiControlSymbols.ACK);
                var dsPartal = ReadProgInfoMessage();
                ds.AddRange(dsPartal);
            }

            return ds;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected abstract void Init(string deviceAddress = "");

        /// <summary>
        /// Перевод устройства в режим программирования
        /// </summary>
        protected abstract void ToProgram();

        /// <summary>
        /// Прочитать отправленную из устройства команду
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">
        /// </exception>
        protected Command ReadCommand() {
            // SOH CMD CDE STX (D..D) ETX BCC
            byte b = port.ReadByte();

            if ( b != (byte)AsciiControlSymbols.SOH)
                throw new Exception($"Некорректный заголовок команды.");

            int i = 0;
            buffer[i++] = port.ReadByte(); // command
            buffer[i++] = port.ReadByte(); // command encoding

            buffer[i++] = port.ReadByte();
            if (buffer[i-1] != (byte)AsciiControlSymbols.STX)
                throw new Exception($"Некорректный заголовок 2 команды.");

            i += port.Read(ref buffer, i, c => c == (byte)AsciiControlSymbols.ETX);
            b = port.ReadByte(); // BCC
            byte bcc = GetBCC(ref buffer, 0, i);
            if(b != bcc)
                throw new Exception($"Не сходится BCC {b} != {bcc}");
            
            var str = ASCIIEncoding.ASCII.GetString(buffer, 3, i - 4);
            var ds = DataSet.Parse(str);

            return new Command
            {
                Cmd = (CommandSymbbols)buffer[0],
                Cde = (CommandDataEncoding)buffer[1],
                DataSet = ds,
            };
        }

        /// <summary>
        /// Записывает команду в устройство
        /// </summary>
        /// <param name="cmd">The command.</param>
        protected void WriteCmd(Command cmd)
        {
            int len = cmd.ToBuffer(ref buffer, 0);
            port.Write(ref buffer, 0, len);
            //lastSessUpdate = DateTime.Now;
        }

        /// <summary>
        /// Записывает команду чтения затем читает результат. В случае NAK делает nakRetries попыток.
        /// </summary>
        /// <param name="dataSet">Набор данных (обычно только адрес)</param>
        /// <param name="cde">Метод кодирования данных</param>
        /// <param name="nakRetries">Число попыток в случае NAK</param>
        /// <returns>Набор данных</returns>
        public DataBlock CmdR(DataSet dataSet, CommandDataEncoding cde = CommandDataEncoding.Ascii, int nakRetries = 1)
        {
            if (SessState == SessionState.Disconnected)
                Init();

            if (SessState != SessionState.Program)
                ToProgram();

            var cmd = new Command
            {
                Cmd = CommandSymbbols.Read,
                Cde = cde,
                DataSet = dataSet,
            };

            WriteCmd(cmd);
            var result = ReadProgInfoMessage();
            lastSessUpdate = DateTime.Now;

            if ((result.ControlSymbol == AsciiControlSymbols.NAK) && (nakRetries-- > 0))
                return CmdR(dataSet, cde, nakRetries);

            return result;
        }

        /// <summary>
        /// Записывает команду записи затем читает результат . В случае NAK делает nakRetries попыток.
        /// </summary>
        /// <param name="dataSet">Набор данных (обычно только адрес)</param>
        /// <param name="cde">Метод кодирования данных</param>
        /// <param name="nakRetries">Число попыток в случае NAK</param>
        /// <returns>Набор данных</returns>
        public AsciiControlSymbols CmdW(DataSet dataSet, CommandDataEncoding cde = CommandDataEncoding.Ascii, int nakRetries = 1)
        {
            if (SessState == SessionState.Disconnected)
                Init();

            if (SessState != SessionState.Program)
                ToProgram();

            var cmd = new Command
            {
                Cmd = CommandSymbbols.Write,
                Cde = cde,
                DataSet = dataSet,
            };

            WriteCmd(cmd);
            var result = (AsciiControlSymbols)port.ReadByte();

            if ((result != AsciiControlSymbols.ACK) || (result != AsciiControlSymbols.NAK))
                throw new Exception("Некорректный ответ усройства на команду записи");

            lastSessUpdate = DateTime.Now;

            if ((result == AsciiControlSymbols.NAK) && (nakRetries-- > 0))
                return CmdW(dataSet, cde, nakRetries);

            return result;
        }

        /// <summary>
        /// Записывает команду пароля затем читает результат. В случае NAK делает nakRetries попыток.
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <param name="nakRetries">Число попыток в случае NAK</param>
        /// <returns>Набор данных</returns>
        public AsciiControlSymbols CmdP(string password, int nakRetries = 1)
        {
            if (SessState == SessionState.Disconnected)
                Init();

            if (SessState != SessionState.Program)
                ToProgram();

            var cmd = new Command
            {
                Cmd = CommandSymbbols.Password,
                Cde = CommandDataEncoding.Ascii,
                DataSet = new DataSet { Value = password},
            };

            WriteCmd(cmd);
            var result = (AsciiControlSymbols)port.ReadByte();
            lastSessUpdate = DateTime.Now;

            switch (result)
            {
                case AsciiControlSymbols.ACK:
                    return result;
                case AsciiControlSymbols.NAK:
                    if (nakRetries-- > 0)
                        result = CmdP(password, nakRetries);
                    break;
                case AsciiControlSymbols.SOH:
                    int i = port.Read(ref buffer, 0, 4);// SOH [B 0 ETX BCC]
                    SessState = SessionState.Disconnected;
                    port.Close();
                    break;
                default:
                    throw new Exception("Некорректный ответ усройства на команду ввода пароля");
            }

            return result;
        }

        /// <summary>
        /// Записывает команду пароля затем читает результат. В случае NAK делает nakRetries попыток.
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <param name="nakRetries">Число попыток в случае NAK</param>
        /// <returns>Набор данных</returns>
        public AsciiControlSymbols CmdE(DataSet dataSet, CommandDataEncoding cde = CommandDataEncoding.Ascii, int nakRetries = 1)
        {
            if (SessState == SessionState.Disconnected)
                Init();

            if (SessState != SessionState.Program)
                ToProgram();

            var cmd = new Command
            {
                Cmd = CommandSymbbols.Execute,
                Cde = CommandDataEncoding.Ascii,
                DataSet = dataSet,
            };

            WriteCmd(cmd);
            var result = (AsciiControlSymbols)port.ReadByte();
            lastSessUpdate = DateTime.Now;

            switch (result)
            {
                case AsciiControlSymbols.ACK:
                    return result;
                case AsciiControlSymbols.NAK:
                    if (nakRetries-- > 0)
                        result = CmdE(dataSet, cde, nakRetries);
                    break;
                default:
                    throw new NotImplementedException("Некорректный ответ усройства на команду ввода выполнения");
            }
            return result;
        }

        /// <summary>
        /// Закрываем сессию.
        /// </summary>
        public void CmdB()
        {
            var cmdBye = new Command {Cmd = CommandSymbbols.Bye, Cde = CommandDataEncoding.Reserved };
            WriteCmd(cmdBye);
            SessState = SessionState.Disconnected;
            port.Close();
        }
    }
}

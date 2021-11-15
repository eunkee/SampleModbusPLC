using System;
using System.Threading;
using ModbusTCP;

namespace SampleModbusPLC
{
    class InterfaceModbus
    {
        private ModbusTCP.Master MBmaster = null;
        private readonly int POLLING_DELAY = 50;
        private readonly int RECONNECT_DELAY = 10000;
        private readonly object LockReadObject = new();
        private readonly object LockWriteObject = new();

        //Read holding register
        readonly ushort ID = 3;
        readonly byte unit = 0;
        readonly ushort startAddress = 51;
        readonly UInt16 readLength = Convert.ToUInt16(100);

        public int shaft1 = -1;
        public int shaft2 = -1;
        public int oldShaft1 = -1;
        public int oldShaft2 = -1;
        public bool tempWriteBoolen1 = false;
        public bool tempWriteBoolen2 = false;
        public bool tempWriteBoolen3 = false;
        public bool tempWriteBoolen4 = false;
        public bool tempWriteBoolen5 = false;
        public bool tempWriteBoolen6 = false;
        public bool tempWriteBoolen7 = false;
        public bool tempWriteBoolen8 = false;
        public bool tempWriteBoolen9 = false;
        public bool tempWriteBoolen10 = false;
        public byte oldByte161a = 0;
        public byte oldByte161b = 0;

        public bool Connect(string address, int port)
        {
            bool rslt = false;
            try
            {
                DisConnect();

                MBmaster = new Master(address, (ushort)port, true);
                if (MBmaster != null)
                {
                    MBmaster.OnResponseData += new ModbusTCP.Master.ResponseData(MBmaster_OnResponseData);
                    MBmaster.OnException += new ModbusTCP.Master.ExceptionData(MBmaster_OnException);
                    rslt = true;
                }
            }
            finally
            {
                if (!MBmaster.connected)
                {
                    Console.WriteLine($"Failed: Connect");
                }
            }
            return rslt;
        }

        //Modbus 접속 상태
        public bool IsConnected
        {
            get
            {
                bool rslt = false;
                if (MBmaster != null)
                {
                    if (MBmaster.connected)
                    {
                        rslt = true;
                    }
                }
                return rslt;
            }
        }

        //Read+Write 무한 재귀함수
        public void ShiftReadWrite()
        {
            try
            {
                while (MBmaster != null)
                {
                    if (MBmaster.connected)
                    {
                        ReadData();
                        WriteData();

                        Thread.Sleep(POLLING_DELAY);
                        if (MBmaster == null)
                        {
                            break;
                        }
                    }
                    else
                    {
                        Thread.Sleep(RECONNECT_DELAY);
                    }
                }
            }
            finally
            {
                Thread.Sleep(RECONNECT_DELAY);
                ShiftReadWrite();
            }
        }

        public static byte[] PlcWriteByte(Int32 value)
        {
            byte[] data = new Byte[2];

            //음수 short처리
            if (value < 0)
            {
                value += 65536;
            }
            if (value >= 256)
            {
                data[0] = Convert.ToByte(value / 256);
                data[1] = Convert.ToByte(value % 256);
            }
            else
            {
                data[0] = 0;
                data[1] = Convert.ToByte(value);
            }

            return data;
        }

        public static long PlcReadDword(Int32 value0, Int32 value1)
        {
            //공백처리
            string zerostring = string.Empty;
            if (Convert.ToString(value0, 2).Length < 16)
            {
                for (int i = Convert.ToString(value0, 2).Length; i < 16; i++)
                {
                    zerostring += "0";
                }
            }
            long rlst = Convert.ToInt64(Convert.ToString(value1, 2) + zerostring + Convert.ToString(value0, 2), 2);
            //unsign -> sign
            if (rlst > 2147483647)
            {
                rlst -= 4294967296;
            }
            return rlst;
        }

        public void WriteData()
        {
            lock (LockWriteObject)
            {
                try
                {
                    //PLC 입장에서 여러개의 번지를 한번에 받아들이지 못하는 경우 (임계영역 처리가 안되는 경우)
                    //값이 이전과 바뀔 경우에 즉시 전송하고 다른 번지의 값이 다른지 확인함
                    ushort WriteID = 8;
                    byte[] data = new Byte[2];

                    //example
                    //141번지 word 전송
                    if (oldShaft1 != shaft1)
                    {
                        data[0] = PlcWriteByte(shaft1)[0];
                        data[1] = PlcWriteByte(shaft1)[1];
                        if (MBmaster != null)
                        {
                            if (MBmaster.connected)
                            {
                                MBmaster.WriteMultipleRegister(WriteID, unit, (Convert.ToUInt16(141)), data);
                                oldShaft1 = shaft1;
                            }
                        }
                        return;
                    }

                    //142번지 word 전송
                    if (oldShaft2 != shaft2)
                    {
                        data[0] = PlcWriteByte(shaft2)[0];
                        data[1] = PlcWriteByte(shaft2)[1];
                        if (MBmaster != null)
                        {
                            if (MBmaster.connected)
                            {
                                MBmaster.WriteMultipleRegister(WriteID, unit, (Convert.ToUInt16(142)), data);
                                oldShaft2 = shaft2;
                            }
                        }
                        return;
                    }

                    //bit 전송
                    //161번지 dword
                    int temp161b = 0;
                    //161.a
                    temp161b += tempWriteBoolen1 ? 100 : 0;
                    //161.b
                    temp161b += tempWriteBoolen2 ? 1000 : 0;
                    //161.c
                    temp161b += tempWriteBoolen3 ? 10000 : 0;
                    //161.d
                    temp161b += tempWriteBoolen4 ? 100000 : 0;
                    //161.e
                    temp161b += tempWriteBoolen5 ? 1000000 : 0;
                    //161.f
                    temp161b += tempWriteBoolen6 ? 10000000 : 0;
                    int temp161a = 0;
                    //161.0
                    temp161a += tempWriteBoolen7 ? 1 : 0;
                    //161.1
                    temp161a += tempWriteBoolen8 ? 10 : 0;
                    //161.2
                    temp161a += tempWriteBoolen9 ? 100 : 0;
                    //161.3
                    temp161a += tempWriteBoolen10 ? 1000 : 0;

                    byte byte161b = Convert.ToByte(Convert.ToString(temp161b), 2);
                    byte byte161a = Convert.ToByte(Convert.ToString(temp161a), 2);
                    if (oldByte161a != byte161a || oldByte161b != byte161b)
                    {
                        data[0] = byte161b;
                        data[1] = byte161a;
                        if (MBmaster != null)
                        {
                            if (MBmaster.connected)
                            {
                                MBmaster.WriteMultipleRegister(WriteID, unit, (Convert.ToUInt16(161)), data);
                                oldByte161a = byte161a;
                                oldByte161b = byte161b;
                            }
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        public bool ReadData()
        {
            bool rslt = false;
            try
            {
                rslt = true;
                MBmaster.ReadHoldingRegister(ID, unit, (Convert.ToUInt16(startAddress)), readLength);
            }
            catch { }
            return rslt;
        }

        public void DisConnect()
        {
            if (MBmaster != null)
            {
                try
                {
                    MBmaster.disconnect();
                    MBmaster.OnResponseData -= new ModbusTCP.Master.ResponseData(MBmaster_OnResponseData);
                    MBmaster.OnException -= new ModbusTCP.Master.ExceptionData(MBmaster_OnException);
                }
                catch { }
                MBmaster = null;
            }
        }

        // ------------------------------------------------------------------------
        // Modbus TCP slave exception
        // ------------------------------------------------------------------------
        private void MBmaster_OnException(ushort id, byte unit, byte function, byte exception)
        {
            try
            {
                string exc = "Modbus says error: ";
                switch (exception)
                {
                    case Master.excIllegalFunction: exc += "Illegal function!"; break;
                    case Master.excIllegalDataAdr: exc += "Illegal data adress!"; break;
                    case Master.excIllegalDataVal: exc += "Illegal data value!"; break;
                    case Master.excSlaveDeviceFailure: exc += "Slave device failure!"; break;
                    case Master.excAck: exc += "Acknoledge!"; break;
                    case Master.excGatePathUnavailable: exc += "Gateway path unavailbale!"; break;
                    case Master.excExceptionTimeout: exc += "Slave timed out!"; break;
                    case Master.excExceptionConnectionLost: exc += "Connection is lost!"; break;
                    case Master.excExceptionNotConnected: exc += "Not connected!"; break;
                }

                //초기화
                DisConnect();
                Console.WriteLine(exc);
            }
            finally
            {
                //Thread.Sleep(RECONNECT_DELAY);
                //Connect();
            }
        }

        private void MBmaster_OnResponseData(ushort ID, byte unit, byte function, byte[] values)
        {
            lock (LockReadObject)
            {
                try
                {
                    if (ID == 3)
                    {
                        int length = values.Length / 2 + Convert.ToInt16(values.Length % 2 > 0);
                        int[] word = new int[length];

                        for (int x = 0; x < length; x += 2)
                        {
                            int n = values[x] * 256 + values[x + 1];

                            //word일 경우에만 보수
                            //dword일 때 제외하고 따로 함수 처리
                            //데이터번지 수정시 반드시 수정 필수
                            //short <- ushort
                            //example excption
                            if ((x == 56 || x == 58 || x == 60 || x == 62 || x == 88 || x == 90))
                            {
                                //Console.WriteLine($"{x}: {n}");
                            }
                            else
                            {
                                if (n > 32767)
                                {
                                    n -= 65536;
                                }
                            }
                            word[x / 2] = n;
                        }

                        //example
                        //83번지 : startAddress부터 32번째 int data 
                        int tempIntData = Convert.ToInt32(Convert.ToString(word[32], 2), 2);
                        //95번지 : startAddress부터 44, 45번째 long data
                        long tempLongData = PlcReadDword(word[44], word[45]);
                        //99번지 : startAddress부터 48번째 bit data /0/1/
                        string temp99 = Convert.ToString(word[48], 2);
                        int n990 = temp99.Length - 1;
                        bool tempBoolenData1 = (n990 >= 0) && (temp99[n990] != '0');
                        int n991 = temp99.Length - 2;
                        bool tempBoolenData2 = (n991 >= 0) && (temp99[n991] != '0');
                        //example

                        Console.WriteLine($"tempIntData: {tempIntData}");
                        Console.WriteLine($"tempLongData: {tempLongData}");
                        Console.WriteLine($"tempBoolenData1: {tempBoolenData1}");
                        Console.WriteLine($"tempBoolenData2: {tempBoolenData2}");
                    }
                }
                catch { }
            }
        }
    }
}

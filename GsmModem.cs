/*----------------------------------------------------------------
 *类库GSMMODEM完成通过短信猫发送和接收短信
 *开源地址：http://code.google.com/p/gsmmodem/
 * 
 *类库GSMMODEM遵循开源协议LGPL
 *有关协议内容参见：http://www.gnu.org/licenses/lgpl.html
 * 
 * Copyright (C) 2011 刘中原
 * 版权所有。 
 * 
 * 文件名： GsmModem.cs
 * 
 * 文件功能描述：   完成短信猫设备的打开关闭，短信的发送与接收以及
 *              其他相应功能
 *              
 * 创建标识：   刘中原20110520
 * 
 * 修改标识：   刘中原20110617
 * 修改描述：   修改为1.0正式版
 * 
**----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace GSMMODEM
{
    /// <summary>
    /// “猫”设备类，完成短信发送 接收等
    /// </summary>
    public class GsmModem
    {
        private object lockObject = new object();
        static AutoResetEvent isSendMessageEvent = new AutoResetEvent(true);
        #region 构造函数
        /// <summary>
        /// 默认构造函数 完成有关初始化工作
        /// </summary>
        /// <remarks>默认 端口号：COM1，波特率：9600</remarks>
        public GsmModem()
            : this("COM1", 9600)
        { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="comPort">串口号</param>
        /// <param name="baudRate">波特率</param>
        public GsmModem(string comPort, int baudRate)
        {
            _com = new MyCom();

            _com.PortName = comPort;          //
            _com.BaudRate = baudRate;
            _com.ReadTimeout = 10000;         //读超时时间 发送短信时间的需要
            _com.RtsEnable = true;            //必须为true 这样串口才能接收到数据

            _com.DataReceived += new EventHandler(sp_DataReceived);
        }

        //单元测试用构造函数
        internal GsmModem(ICom com)
        {
            _com = com;

            _com.ReadTimeout = 10000;         //读超时时间 发送短信时间的需要
            _com.RtsEnable = true;            //必须为true 这样串口才能接收到数据

            _com.DataReceived += new EventHandler(sp_DataReceived);
        }

        #endregion 构造函数

        #region 私有字段
        private ICom _com;              //私有字段 串口对象

        private Queue<int> newMsgIndexQueue = new Queue<int>();            //新消息序号
        /// <summary>
        /// 开放出来最新短信索引
        /// </summary>
        public Queue<int> NewMsgIndexQueue
        {
            get { return newMsgIndexQueue; }
            set { newMsgIndexQueue = value; }
        }

        private string msgCenter = string.Empty;           //短信中心号码

        #endregion 私有字段

        #region 属性

        /// <summary>
        /// 串口号 运行时只读 设备打开状态写入将引发异常
        /// 提供对串口端口号的访问
        /// </summary>
        public string ComPort
        {
            get
            {
                return _com.PortName;
            }
            set
            {
                _com.PortName = value;
            }
        }

        /// <summary>
        /// 波特率 可读写
        /// 提供对串口波特率的访问
        /// </summary>
        public int BaudRate
        {
            get
            {
                return _com.BaudRate;
            }
            set
            {
                _com.BaudRate = value;
            }
        }

        /// <summary>
        /// 设备是否打开
        /// 对串口IsOpen属性访问
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return _com.IsOpen;
            }
        }

        private bool autoDelMsg = false;

        /// <summary>
        /// 对autoDelMsg访问
        /// 设置是否在阅读短信后自动删除 SIM 卡内短信存档
        /// 默认为 false 
        /// </summary>
        public bool AutoDelMsg
        {
            get
            {
                return autoDelMsg;
            }
            set
            {
                autoDelMsg = value;
            }
        }

        #endregion

        #region 收到短信事件

        /// <summary>
        /// 收到短信息事件 OnRecieved 
        /// 收到短信将引发此事件
        /// </summary>
        public event EventHandler SmsRecieved;

        #endregion

        #region 串口收到数据检测短信收到

        /// <summary>
        /// 从串口收到数据 串口事件
        /// 程序未完成需要的可自己添加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sp_DataReceived(object sender, EventArgs e)
        {
            //Console.WriteLine("!!!!!!!!!!!!!!!!!sp_DataReceived锁死");
            //isSendMessageEvent.WaitOne();
            string temp = _com.ReadLine();
            if (temp.Length > 8)
            {
                if (temp.Substring(0, 6) == "+CMTI:")
                {
                    newMsgIndexQueue.Enqueue(Convert.ToInt32(temp.Split(',')[1]));  //存储新信息序号
                    OnSmsRecieved(e);                                //触发事件
                }
            }
            //isSendMessageEvent.Set();
            //Console.WriteLine("!!!!!!!!!!!!!!!!!sp_DataReceived释放");
        }

        /// <summary>
        /// 保护虚方法，引发收到短信事件
        /// </summary>
        /// <param name="e">事件数据</param>
        protected virtual void OnSmsRecieved(EventArgs e)
        {
            if (SmsRecieved != null)
            {
                SmsRecieved(this, e);
            }
        }

        #endregion

        #region 方法

        #region 设备打开与关闭

        /// <summary>
        /// 设备打开函数，无法时打开将引发异常
        /// </summary>
        public void Open()
        {
            //如果串口已打开 则先关闭
            if (_com.IsOpen)
            {
                _com.Close();
            }

            _com.Open();

            //初始化设备
            if (_com.IsOpen)
            {
                _com.Write("ATE0\r");
                Thread.Sleep(50);
                _com.Write("AT+CMGF=0\r");
                Thread.Sleep(50);
                _com.Write("AT+CNMI=2,1\r");
            }
        }

        /// <summary>
        /// 设备关闭函数
        /// </summary>
        public void Close()
        {
            try
            {
                _com.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion 设备打开与关闭

        #region 获取和设置设备有关信息

        /// <summary>
        /// 获取机器码
        /// </summary>
        /// <returns>机器码字符串（设备厂商，本机号码）</returns>
        public string GetMachineNo()
        {
            Console.WriteLine("!!!!!!!!!!!!!!!!!GetMachineNo锁死");

            isSendMessageEvent.WaitOne();
            string result = SendAT("AT+CGMI");
            isSendMessageEvent.Set();
            Console.WriteLine("!!!!!!!!!!!!!!!!!GetMachineNo释放");
            if (result.Substring(result.Length - 4, 3).Trim() == "OK")
            {
                result = result.Substring(0, result.Length - 5).Trim();
            }
            else
            {
                throw new Exception("获取机器码失败");
            }
            
            return result;
        }

        /// <summary>
        /// 设置短信中心号码
        /// </summary>
        /// <param name="msgCenterNo">短信中心号码</param>
        public void SetMsgCenterNo(string msgCenterNo)
        {
            msgCenter = msgCenterNo;
        }

        /// <summary>
        /// 获取短信中心号码
        /// </summary>
        /// <returns></returns>
        public string GetMsgCenterNo()
        {
            
            string tmp = string.Empty;
            if (msgCenter != null && msgCenter.Length != 0)
            {
                return msgCenter;
            }
            else
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!GetMsgCenterNo锁死");
                isSendMessageEvent.WaitOne();
                tmp = SendAT("AT+CSCA?");
                isSendMessageEvent.Set();
                Console.WriteLine("!!!!!!!!!!!!!!!!!GetMsgCenterNo释放");
                if (tmp.Substring(tmp.Length - 4, 3).Trim() == "OK")
                {
                    return tmp.Split('\"')[1].Trim();
                }
                else
                {
                    throw new Exception("获取短信中心失败");
                }
            }
            
        }

        #endregion 获取和设置设备有关信息

        #region 发送AT指令

        /// <summary>
        /// 发送AT指令 逐条发送AT指令 调用一次发送一条指令
        /// 能返回一个OK或ERROR算一条指令
        /// </summary>
        /// <param name="ATCom">AT指令</param>
        /// <returns>发送指令后返回的字符串</returns>
        public string SendAT(string ATCom)
        {

            string result = string.Empty;
            Console.WriteLine("忽略接收缓冲区内容，准备发送");
            //忽略接收缓冲区内容，准备发送
            _com.DiscardInBuffer();
            Console.WriteLine("注销事件关联，为发送做准备");
            //注销事件关联，为发送做准备
            lock (lockObject)
            {

                _com.DataReceived -= sp_DataReceived;

            }
            //发送AT指令
            try
            {
                Console.WriteLine("发送AT指令");
                _com.Write(ATCom + "\r");
            }
            catch (Exception ex)
            {
                Console.WriteLine("发送AT指令失败:" + ex.Message);
                lock (lockObject)
                {
                    _com.DataReceived += sp_DataReceived;
                }
                throw ex;
            }
            Console.WriteLine("接收数据 循环读取数据 直至收到“OK”或“ERROR”");
            //接收数据 循环读取数据 直至收到“OK”或“ERROR”
            try
            {
                string temp = string.Empty;
                while (temp.Trim() != "OK" && temp.Trim() != "ERROR")
                {
                    temp = _com.ReadLine();
                    Console.WriteLine("调试:\"" + ATCom + "\"---返回:" + temp);
                    result += temp;

                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //事件重新绑定 正常监视串口数据
                lock (lockObject)
                {
                    _com.DataReceived += sp_DataReceived;
                }

            }
        }

        #endregion 发送AT指令

        #region 发送短信

        /// <summary>
        /// 发送短信
        /// 发送失败将引发异常
        /// </summary>
        /// <param name="phone">手机号码</param>
        /// <param name="msg">短信内容</param>
        public void SendMsg(string phone, string msg)
        {
            try
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!SendMsg锁死");
                isSendMessageEvent.WaitOne();
                Console.WriteLine("初始化PDUEncoding解码类");
                PDUEncoding pe = new PDUEncoding();
                pe.ServiceCenterAddress = msgCenter;                    //短信中心号码 服务中心地址

                string tmp = string.Empty;
                Console.WriteLine("获取PDUEncoding列表");
                var cmlist = pe.PDUEncoder(phone, msg);
                foreach (CodedMessage cm in cmlist)
                {
                    try
                    {
                        Console.WriteLine("注销事件关联，为发送做准备");
                        //注销事件关联，为发送做准备
                        lock (lockObject)
                        {

                            _com.DataReceived -= sp_DataReceived;
                        }
                        Console.WriteLine("准备发送");
                        _com.Write("AT+CMGS=" + cm.Length.ToString() + "\r");
                        _com.ReadTo(">");
                        _com.DiscardInBuffer();
                        Console.WriteLine("事件重新绑定 正常监视串口数据");
                        //事件重新绑定 正常监视串口数据
                        lock (lockObject)
                        {
                            _com.DataReceived += sp_DataReceived;
                        }
                        Console.WriteLine("准备发送短信");
                        tmp = SendAT(cm.PduCode + (char)(26));  //26 Ctrl+Z ascii码
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("短信发送失败:" + ex.Message);
                    }
                    if (tmp.Contains("OK"))
                    {
                        continue;
                    }

                    throw new Exception("短信发送失败");
                }
            }
            catch (Exception exmsg)
            {
                throw exmsg;
            }
            finally
            {
                isSendMessageEvent.Set();
                Console.WriteLine("!!!!!!!!!!!!!!!!!SendMsg释放");
            }

        }

        #endregion 发送短信

        #region 读取短信

        /// <summary>
        /// 获取未读信息列表
        /// </summary>
        /// <returns>未读信息列表（中心号码，手机号码，发送时间，短信内容）</returns>
        public List<DecodedMessage> GetUnreadMsg()
        {
            List<DecodedMessage> result = new List<DecodedMessage>();
            string[] temp = null;
            string tmp = string.Empty;
            Console.WriteLine("!!!!!!!!!!!!!!!!!GetUnreadMsg锁死");
            isSendMessageEvent.WaitOne();
            tmp = SendAT("AT+CMGL=0");
            isSendMessageEvent.Set();
            Console.WriteLine("!!!!!!!!!!!!!!!!!GetUnreadMsg释放");
            if (tmp.Contains("OK"))
            {
                temp = tmp.Split('\r');
            }

            PDUEncoding pe = new PDUEncoding();
            foreach (string str in temp)
            {
                if (str != null && str.Length > 18)   //短信PDU长度仅仅短信中心就18个字符
                {
                    result.Add(pe.PDUDecoder(str));
                }
            }

            return result;
        }

        /// <summary>
        /// 读取新消息
        /// </summary>
        /// <returns>新消息解码后内容</returns>
        /// <remarks>建议在收到短信事件中调用</remarks>
        public DecodedMessage ReadNewMsg()
        {
            return ReadMsgByIndex(newMsgIndexQueue.Dequeue());
        }

        /// <summary>
        /// 按序号读取短信
        /// </summary>
        /// <param name="index">序号</param>
        /// <returns>信息字符串 (中心号码，手机号码，发送时间，短信内容)</returns>
        public DecodedMessage ReadMsgByIndex(int index)
        {
            string temp = string.Empty;
            //string msgCenter, phone, msg, time;
            PDUEncoding pe = new PDUEncoding();
            try
            {
                Console.WriteLine("!!!!!!!!!!!!!!!!!ReadMsgByIndex锁死");
                isSendMessageEvent.WaitOne();
                temp = SendAT("AT+CMGR=" + index.ToString());
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                isSendMessageEvent.Set();
                Console.WriteLine("!!!!!!!!!!!!!!!!!ReadMsgByIndex释放");
            }

            if (temp.Trim() == "ERROR")
            {
                throw new Exception("没有此短信");
            }
            temp = temp.Split((char)(13))[2];       //取出PDU串(char)(13)为0x0a即\r 按\r分为多个字符串 第3个是PDU串

            //pe.PDUDecoder(temp, out msgCenter, out phone, out msg, out time);

            if (AutoDelMsg)
            {
                try
                {
                    DeleteMsgByIndex(index);
                }
                catch
                {

                }
            }

            return pe.PDUDecoder(temp);
            //return msgCenter + "," + phone + "," + time + "," + msg;
        }

        #endregion 读取短信

        #region 删除短信

        /// <summary>
        /// 按索引号删除短信
        /// </summary>
        /// <param name="index">The index.</param>
        public void DeleteMsgByIndex(int index)
        {
            Console.WriteLine("!!!!!!!!!!!!!!!!!DeleteMsgByIndex锁死");
            isSendMessageEvent.WaitOne();
            if (SendAT("AT+CMGD=" + index.ToString()).Trim() == "OK")
            {
                isSendMessageEvent.Set();
                Console.WriteLine("!!!!!!!!!!!!!!!!!DeleteMsgByIndex释放");
                return;
            }
            isSendMessageEvent.Set();
            Console.WriteLine("!!!!!!!!!!!!!!!!!DeleteMsgByIndex释放");
            throw new Exception("删除失败");
        }

        #endregion 删除短信

        #endregion
    }

}

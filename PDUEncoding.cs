/*----------------------------------------------------------------
 *类库GSMMODEM完成通过短信猫发送和接收短信
 *开源地址：http://code.google.com/p/gsmmodem/
 * 
 * 类库GSMMODEM遵循开源协议LGPL
 *有关协议内容参见：http://www.gnu.org/licenses/lgpl.html
 * 
 * Copyright (C) 2011 刘中原
 * 版权所有。 
 * 
 * 文件名： PDUEncoding.cs
 * 
 * 文件功能描述：   完成PDU短信格式的编码与解码
 *              
 * 创建标识：   刘中原20110520
 * 
 * 修改标识：   
 * 修改描述：   
 * 
**----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace GSMMODEM
{
    /// <summary>
    /// PDU编解码类，完成PDU短信格式的编码与解码
    /// 代码不是很安全，投入使用的话需要一定的改动 
    /// 私有类，只能命名空间内部使用 调试此类时须设为公有 完成后去掉public
    /// </summary>
    internal class PDUEncoding
    {
        /// <summary>
        /// 各个字段和属性 
        /// 字段 为 位组值（解码后） 属性对外显示 正常值（解码前）
        /// </summary>
        #region PDU编码和解码所需各段位组值

        private string serviceCenterAddress = "00";
        /// <summary>
        /// 消息服务中心(1-12个8位组)
        /// </summary>
        public string ServiceCenterAddress
        {
            get
            {
                int len = 2 * Convert.ToInt32(serviceCenterAddress.Substring(0, 2), 16);
                string result = serviceCenterAddress.Substring(4, len - 2);

                result = ParityChange(result);
                result = result.TrimEnd('F', 'f');
                return result;
            }
            set
            {
                if (value == null || value.Length == 0)      //号码为空
                {
                    serviceCenterAddress = "00";
                }
                else
                {
                    value = value.TrimStart('+');

                    if (value.Substring(0, 2) != "86")
                    {
                        value = "86" + value;
                    }

                    value = "91" + ParityChange(value);
                    serviceCenterAddress = (value.Length / 2).ToString("X2") + value;
                }

            }
        }

        private string protocolDataUnitType = "11";
        /// <summary>
        /// 协议数据单元类型(1个8位组)
        /// </summary>
        public string ProtocolDataUnitType
        {
            set
            {
                protocolDataUnitType = value;
            }
            get
            {
                return protocolDataUnitType;
            }
        }

        private string messageReference = "00";
        /// <summary>
        /// 所有成功的短信发送参考数目（0..255）
        /// (1个8位组)
        /// </summary>
        public string MessageReference
        {
            get
            {
                return "00";
            }
            set
            {
                messageReference = value;
            }
        }

        private string originatorAddress = "00";
        /// <summary>
        /// 发送方地址（手机号码）(2-12个8位组) 仅接收有效 只读属性
        /// </summary>
        public string OriginatorAddress
        {
            get
            {
                int len = Convert.ToInt32(originatorAddress.Substring(0, 2), 16);    //十六进制字符串转为整形数据
                string result = string.Empty;

                if (len % 2 == 1)       //号码长度是奇数，长度加1 编码时加了F
                {
                    len++;
                }

                result = originatorAddress.Substring(4, len);
                result = ParityChange(result).TrimEnd('F', 'f');    //奇偶互换，并去掉结尾F

                return result;
            }
        }

        private string destinationAddress = "00";
        /// <summary>
        /// 接收方地址（手机号码）(2-12个8位组) 仅发送有效 只写
        /// </summary>
        public string DestinationAddress
        {
            set
            {
                if (value == null || value.Length == 0)      //号码为空
                {
                    throw new ArgumentNullException("目的号码不允许为空");
                }
                else
                {
                    value = value.TrimStart('+');

                    if (value.Substring(0, 2) == "86")
                    {
                        value = value.TrimStart('8', '6');
                    }

                    int len = value.Length;
                    value = ParityChange(value);

                    destinationAddress = len.ToString("X2") + "A1" + value;
                }
            }
        }

        private string protocolIdentifer = "00";
        /// <summary>
        /// 参数显示消息中心以何种方式处理消息内容
        /// （比如FAX,Voice）(1个8位组)
        /// </summary>
        public string ProtocolIdentifer
        {
            get
            {
                return protocolIdentifer;
            }
            set
            {
                protocolIdentifer = value;
            }
        }

        private string dataCodingScheme = "08";
        /// <summary>
        /// 参数显示用户数据编码方案(1个8位组)
        /// </summary>
        public string DataCodingScheme
        {
            get
            {
                return dataCodingScheme;
            }
            set
            {
                dataCodingScheme = value;
            }
        }

        private string serviceCenterTimeStamp = "";
        /// <summary>
        /// 消息中心收到消息时的时间戳(7个8位组)  仅接收有效 只读属性
        /// </summary>
        public string ServiceCenterTimeStamp
        {
            get
            {
                string result = ParityChange(serviceCenterTimeStamp);
                result = "20" + result.Substring(0, 12);            //年加开始的“20”

                return result;
            }
        }

        private string validityPeriod = "C4";       //暂时固定有效期
        /// <summary>
        /// 短消息有效期(0,1,7个8位组)
        /// </summary>
        public string ValidityPeriod
        {
            get
            {
                return "C4";
            }
            set
            {
                validityPeriod = value;
            }
        }

        private string userDataLenghth = "00";
        /// <summary>
        /// 用户数据长度(1个8位组)
        /// </summary>
        public string UserDataLenghth
        {
            get
            {
                return (userData.Length / 2).ToString("X2");
            }
            set
            {
                userDataLenghth = value;
            }
        }

        private string userData = "";
        /// <summary>
        /// 用户数据(0-140个8位组)
        /// </summary>
        public string UserData
        {
            get
            {
                string result = string.Empty;

                if (dataCodingScheme.Substring(1, 1) == "8")             //USC2编码
                {
                    int len = Convert.ToInt32(userDataLenghth, 16) * 2;

                    //四个一组，每组译为一个USC2字符
                    for (int i = 0; i < len; i += 4)
                    {
                        string temp = userData.Substring(i, 4);

                        int byte1 = Convert.ToInt16(temp, 16);

                        result += ((char)byte1).ToString();
                    }
                }
                else
                {
                    result = PDU7bitContentDecoder(userData);
                }

                return result;
            }
            set
            {
                if (dataCodingScheme.Substring(1, 1) == "8")           //USC2编码使用
                {
                    userData = string.Empty;
                    Encoding encodingUTF = Encoding.BigEndianUnicode;

                    byte[] Bytes = encodingUTF.GetBytes(value);

                    for (int i = 0; i < Bytes.Length; i++)
                    {
                        userData += BitConverter.ToString(Bytes, i, 1);
                    }
                    userDataLenghth = (userData.Length / 2).ToString("X2");
                }
                else                                                                //7bit编码使用
                {
                    userData = string.Empty;
                    userDataLenghth = value.Length.ToString("X2");                  //7bit编码 用户数据长度：源字符串长度

                    Encoding encodingAsscii = Encoding.ASCII;
                    byte[] bytes = encodingAsscii.GetBytes(value);

                    string temp = string.Empty;                                     //存储中间字符串 二进制串
                    string tmp;
                    for (int i = value.Length; i > 0; i--)                          //高低交换 二进制串
                    {
                        tmp = Convert.ToString(bytes[i - 1], 2);
                        while (tmp.Length < 7)                                      //不够7位，补齐
                        {
                            tmp = "0" + tmp;
                        }
                        temp += tmp;
                    }

                    for (int i = temp.Length; i > 0; i -= 8)                    //每8位取位为一个字符 即完成编码
                    {
                        if (i > 8)
                        {
                            userData += Convert.ToInt32(temp.Substring(i - 8, 8), 2).ToString("X2");
                        }
                        else
                        {
                            userData += Convert.ToInt32(temp.Substring(0, i), 2).ToString("X2");
                        }
                    }

                }
            }
        }

        #endregion PDU编码和解码所需各段位组值

        #region 私有方法

        /// <summary>
        /// 奇偶互换 (+F)
        /// </summary>
        /// <param name="str">要被转换的字符串</param>
        /// <returns>转换后的结果字符串</returns>
        private string ParityChange(string str)
        {
            string result = string.Empty;

            if (str.Length % 2 != 0)         //奇字符串 补F
            {
                str += "F";
            }
            for (int i = 0; i < str.Length; i += 2)     //奇偶互换
            {
                result += str[i + 1];
                result += str[i];
            }

            return result;
        }

        /// <summary>
        /// 判断字符串中是否不含中文字符（是否是ASCII字符串）
        /// </summary>
        /// <param name="str">要判断的字符串</param>
        /// <returns>bool值 是ASCII字符串，返回True；否则false</returns>
        private bool IsASCII(string str)
        {
            int strLen = str.Length;

            //字符串的字节数，字母占1位，汉字占2位,注意，一定要UTF8
            int byteLen = System.Text.Encoding.UTF8.GetBytes(str).Length;

            //字符数和字节数相等，则全部是ASCII码字符；不相等 则字节数大于字符数 含有汉字字符
            return (strLen == byteLen);
        }

        /// <summary>
        /// 十六进制字符串转二进制字节串
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>转化得到的byte[]</returns>
        private byte[] Hex2Bin(string hex)
        {
            byte[] bin = new byte[hex.Length / 2];  //结果字节串

            for (int i = 0; i < hex.Length; i += 2)
            {
                //两个字符一组 转化为一个字节
                bin[i / 2] = (byte)Convert.ToByte((hex[i].ToString() + hex[i + 1].ToString()), 16);
            }

            return bin;
        }

        /// <summary>
        /// byte数组转换为字符串 byte最高位0 忽略 每个byte 7个字符
        /// </summary>
        /// <param name="bytes">byte数组</param>
        /// <returns>结果字符串</returns>
        private string Bin2BinStringof8Bit(byte[] bytes)
        {
            string result = string.Empty;

            foreach(byte b in bytes)
            {
                string tmp = Convert.ToString(b, 2);
                while (tmp.Length < 8)
                {
                    tmp = "0" + tmp;        //前导零补足8bit
                }
                result += tmp;
            }

            return result;
        }

        /// <summary> 
        /// 二进制字符串转化为16进制字符串 每4位一个十六进制字符
        /// </summary>
        /// <param name="bin">二进制字符串</param>
        /// <returns></returns>
        private string BinStringof8Bit2AsciiwithReverse(string bin)
        {
            string temp = bin;
            byte[] bytes = new byte[temp.Length / 7];

            //二进制 不是7倍数 去除前导0
            temp = temp.Remove(0, temp.Length % 7);

            for (int i = 0; i < temp.Length; i += 7)
            {
                bytes[i / 7] = Convert.ToByte(temp.Substring(i, 7), 2);
            }

            Array.Reverse(bytes);

            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// PDU7bit的解码，供UserData的get访问器调用
        /// </summary>
        /// <param name="userData">数据部分PDU字符串</param>
        /// <returns></returns>
        private string PDU7bitContentDecoder(string userData)
        {
            string result = string.Empty;
            byte[] b;

            b = Hex2Bin(userData);

            Array.Reverse(b);       //字节串翻转

            result = Bin2BinStringof8Bit(b);

            result = BinStringof8Bit2AsciiwithReverse(result);

            return result;
        }

        /// <summary>
        /// PDU编码器，完成PDU编码(USC2编码，超过70个字时 分多条发送，PDU各个串之间逗号分隔)
        /// </summary>
        /// <param name="phone">目的手机号码</param>
        /// <param name="Text">短信内容</param>
        /// <returns>编码后的PDU字符串 长短信时 逗号分隔</returns>
        private List<CodedMessage> PDUUSC2Encoder(string phone, string Text)
        {
            dataCodingScheme = "08";
            DestinationAddress = phone;

            List<CodedMessage> result = new List<CodedMessage>();

            if (Text.Length > 70)
            {
                //长短信设TP-UDHI位为1 PDU-type = “51”
                ProtocolDataUnitType = "51";

                //计算长短信条数
                int count = Text.Length / 67;

                if (Text.Length % 67 != 0)
                {
                    count++;
                }

                for (int i = 0; i < count; i++)
                {
                    //如果不是最后一条
                    if (i < count - 1)
                    {
                        UserData = Text.Substring(i * 67, 67);

                        result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                            + messageReference + destinationAddress + protocolIdentifer
                             + dataCodingScheme + validityPeriod + (userData.Length / 2 + 6).ToString("X2")
                             + "05000339" + count.ToString("X2") + (i + 1).ToString("X2") + userData));
                    }
                    else
                    {
                        UserData = Text.Substring(i * 67);

                        if (userData != null || userData.Length != 0)
                        {

                            result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                                + messageReference + destinationAddress + protocolIdentifer
                                 + dataCodingScheme + validityPeriod + (userData.Length / 2 + 6).ToString("X2")
                                 + "05000339" + count.ToString("X2") + (i + 1).ToString("X2") + userData));
                        }
                    }
                }
            }
            else        //不是长短信
            {
                ProtocolDataUnitType = "11";
                UserData = Text;
                result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                    + messageReference + destinationAddress + protocolIdentifer
                    + dataCodingScheme + validityPeriod + userDataLenghth + userData));
            }
            return result;
        }

        /// <summary>
        /// 7bit 编码(超过160个字时 分多条发送，PDU各个串之间逗号分隔)
        /// </summary>
        /// <param name="phone">手机号码</param>
        /// <param name="Text">短信内容</param>
        /// <returns>编码后的字符串 长短信时 逗号分隔</returns>
        private List<CodedMessage> PDU7BitEncoder(string phone, string Text)
        {
            dataCodingScheme = "00";
            DestinationAddress = phone;

            List<CodedMessage> result = new List<CodedMessage>();

            if (Text.Length > 160)
            {
                //长短信设TP-UDHI位为1 PDU-type = “51”
                ProtocolDataUnitType = "51";

                //计算长短信条数
                int count = Text.Length / 153;

                //如果有下一条
                if (Text.Length % 153 != 0)
                {
                    count++;
                }

                for (int i = 0; i < count; i++)
                {
                    //如果不是最后一条
                    if (i < count - 1)
                    {
                        UserData = Text.Substring(i * 153 + 1, 152);    //去掉第一个字母（特殊编码）

                        result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                            + messageReference + destinationAddress + protocolIdentifer
                             + dataCodingScheme + validityPeriod + (160).ToString("X2")
                             + "05000339" + count.ToString("X2") + (i + 1).ToString("X2")
                             + ((int)(new ASCIIEncoding().GetBytes(Text.Substring(i * 153, 1))[0] << 1)).ToString("X2") + userData));
                    }
                    else
                    {
                        UserData = Text.Substring(i * 153 + 1);

                        int len = Text.Substring(i * 153).Length;

                        if (userData != null || userData.Length != 0)
                        {
                            result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                                + messageReference + destinationAddress + protocolIdentifer
                                 + dataCodingScheme + validityPeriod + (len + 7).ToString("X2")
                                 + "05000339" + count.ToString("X2") + (i + 1).ToString("X2")
                                 + ((int)(new ASCIIEncoding().GetBytes(Text.Substring(i * 153, 1))[0] << 1)).ToString("X2")
                                 + userData));
                        }
                    }
                }

                return result;
            }

            UserData = Text;

            result.Add(new CodedMessage(serviceCenterAddress + protocolDataUnitType
                + messageReference + destinationAddress + protocolIdentifer
                + dataCodingScheme + validityPeriod + userDataLenghth + userData));

            return result;
        }

        #endregion 私有方法

        #region 编码

        public List<CodedMessage> PDUEncoder(string phone, string text)
        {
            if (IsASCII(text))
            {
                return PDU7BitEncoder(phone, text);
            }
            else
            {
                return PDUUSC2Encoder(phone, text);
            }
        }

        #endregion 编码

        #region 解码

        #region 解码注释掉
        /*
        /// <summary>
        /// 完成手机或短信猫收到PDU格式短信的解码 暂时仅支持中文编码
        /// 未用DCS部分
        /// </summary>
        /// <param name="strPDU">短信PDU字符串</param>
        /// <param name="msgCenter">短消息服务中心 输出</param>
        /// <param name="phone">发送方手机号码 输出</param>
        /// <param name="msg">短信内容 输出</param>
        /// <param name="time">时间字符串 输出</param>
        public void PDUDecoder(string strPDU, out string msgCenter, out string phone, out string msg, out string time)
        {
            int lenSCA = Convert.ToInt32(strPDU.Substring(0, 2), 16) * 2 + 2;       //短消息中心占长度
            serviceCenterAddress = strPDU.Substring(0, lenSCA);

            int lenOA = Convert.ToInt32(strPDU.Substring(lenSCA + 2, 2),16);           //OA占用长度
            if (lenOA % 2 == 1)                                                     //奇数则加1 F位
            {
                lenOA++;
            }
            lenOA += 4;                 //加号码编码的头部长度
            originatorAddress = strPDU.Substring(lenSCA + 2, lenOA);

            dataCodingScheme = strPDU.Substring(lenSCA + lenOA + 4, 2);             //DCS赋值，区分解码7bit

            serviceCenterTimeStamp = strPDU.Substring(lenSCA + lenOA + 6, 14);

            userDataLenghth = strPDU.Substring(lenSCA + lenOA + 20, 2);
            int lenUD = Convert.ToInt32(userDataLenghth, 16) * 2;
            userData = strPDU.Substring(lenSCA + lenOA + 22);

            msgCenter = ServiceCenterAddress;
            phone = OriginatorAddress;
            msg = UserData;
            time = ServiceCenterTimeStamp;
        }
        */
        #endregion 解码注释掉

        /// <summary>
        /// 重载 解码，返回信息字符串 格式 
        /// </summary>
        /// <param name="strPDU">短信PDU字符串</param>
        /// <returns>信息字符串（MMNN,中心号码，手机号码，发送时间，短信内容 MM这批短信总条数 NN本条所在序号）</returns>
        public DecodedMessage PDUDecoder(string strPDU)
        {
            int lenSCA = Convert.ToInt32(strPDU.Substring(0, 2), 16) * 2 + 2;       //短消息中心占长度
            serviceCenterAddress = strPDU.Substring(0, lenSCA);

            //PDU-type位组
            protocolDataUnitType = strPDU.Substring(lenSCA, 2);

            int lenOA = Convert.ToInt32(strPDU.Substring(lenSCA + 2, 2), 16);           //OA占用长度
            if (lenOA % 2 == 1)                                                     //奇数则加1 F位
            {
                lenOA++;
            }
            lenOA += 4;                 //加号码编码的头部长度
            originatorAddress = strPDU.Substring(lenSCA + 2, lenOA);

            dataCodingScheme = strPDU.Substring(lenSCA + lenOA + 4, 2);             //DCS赋值，区分解码7bit

            serviceCenterTimeStamp = strPDU.Substring(lenSCA + lenOA + 6, 14);

            userDataLenghth = strPDU.Substring(lenSCA + lenOA + 20, 2);
            int lenUD = Convert.ToInt32(userDataLenghth, 16) * 2;

            if ((Convert.ToInt32(protocolDataUnitType, 16) & 0x40) != 0)    //长短信
            {
                if (dataCodingScheme.Substring(1, 1) == "8")           //USC2 长短信 去掉消息头
                {
                    userDataLenghth = (Convert.ToInt16(strPDU.Substring(lenSCA + lenOA + 20, 2), 16) - 6).ToString("X2");
                    userData = strPDU.Substring(lenSCA + lenOA + 22 + 6 * 2);

                    return new DecodedMessage(strPDU.Substring(lenSCA + lenOA + 22 + 4 * 2, 2 * 2)
                        + strPDU.Substring(lenSCA + lenOA + 22 + 3 * 2, 2)
                        , ServiceCenterAddress
                        , ServiceCenterTimeStamp.Substring(0, 4) + "-" + ServiceCenterTimeStamp.Substring(4, 2) + "-"
                        + ServiceCenterTimeStamp.Substring(6, 2) + " " + ServiceCenterTimeStamp.Substring(8, 2) + ":"
                        + ServiceCenterTimeStamp.Substring(10, 2) + ":" + ServiceCenterTimeStamp.Substring(12, 2)
                        , OriginatorAddress, UserData);
                }
                else
                {
                    userData = strPDU.Substring(lenSCA + lenOA + 22 + 6 * 2 + 1 * 2);   //消息头六字节,第一字节特殊译码 >>7

                    //首字节译码 
                    byte byt = Convert.ToByte(strPDU.Substring(lenSCA + lenOA + 22 + 6 * 2, 2), 16);
                    char first = (char)(byt >> 1);

                    return new DecodedMessage(strPDU.Substring(lenSCA + lenOA + 22 + 4 * 2, 2 * 2)
                        + strPDU.Substring(lenSCA + lenOA + 22 + 3 * 2, 2)
                        , ServiceCenterAddress
                        , ServiceCenterTimeStamp.Substring(0, 4) + "-" + ServiceCenterTimeStamp.Substring(4, 2) + "-"
                        + ServiceCenterTimeStamp.Substring(6, 2) + " " + ServiceCenterTimeStamp.Substring(8, 2) + ":"
                        + ServiceCenterTimeStamp.Substring(10, 2) + ":" + ServiceCenterTimeStamp.Substring(12, 2)
                        , OriginatorAddress
                        , first + UserData);
                }
            }

            userData = strPDU.Substring(lenSCA + lenOA + 22);
            return new DecodedMessage("010100"
                , ServiceCenterAddress
                , ServiceCenterTimeStamp.Substring(0, 4) + "-" + ServiceCenterTimeStamp.Substring(4, 2) + "-"
                + ServiceCenterTimeStamp.Substring(6, 2) + " " + ServiceCenterTimeStamp.Substring(8, 2) + ":"
                + ServiceCenterTimeStamp.Substring(10, 2) + ":" + ServiceCenterTimeStamp.Substring(12, 2)
                , OriginatorAddress
                , UserData);
        }

        #endregion 解码

    }
}

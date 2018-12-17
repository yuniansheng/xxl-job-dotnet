using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing an object for known object types.
    /// </summary>
    public class BasicDeserializer : AbstractDeserializer
    {
        public const int NULL = BasicSerializer.NULL;
        public const int BOOLEAN = BasicSerializer.BOOLEAN;
        public const int BYTE = BasicSerializer.BYTE;
        public const int SHORT = BasicSerializer.SHORT;
        public const int INTEGER = BasicSerializer.INTEGER;
        public const int LONG = BasicSerializer.LONG;
        public const int FLOAT = BasicSerializer.FLOAT;
        public const int DOUBLE = BasicSerializer.DOUBLE;
        public const int CHARACTER = BasicSerializer.CHARACTER;
        public const int CHARACTER_OBJECT = BasicSerializer.CHARACTER_OBJECT;
        public const int STRING = BasicSerializer.STRING;
        public const int DATE = BasicSerializer.DATE;
        public const int NUMBER = BasicSerializer.NUMBER;
        public const int OBJECT = BasicSerializer.OBJECT;

        public const int BOOLEAN_ARRAY = BasicSerializer.BOOLEAN_ARRAY;
        public const int BYTE_ARRAY = BasicSerializer.BYTE_ARRAY;
        public const int SHORT_ARRAY = BasicSerializer.SHORT_ARRAY;
        public const int INTEGER_ARRAY = BasicSerializer.INTEGER_ARRAY;
        public const int LONG_ARRAY = BasicSerializer.LONG_ARRAY;
        public const int FLOAT_ARRAY = BasicSerializer.FLOAT_ARRAY;
        public const int DOUBLE_ARRAY = BasicSerializer.DOUBLE_ARRAY;
        public const int CHARACTER_ARRAY = BasicSerializer.CHARACTER_ARRAY;
        public const int STRING_ARRAY = BasicSerializer.STRING_ARRAY;
        public const int OBJECT_ARRAY = BasicSerializer.OBJECT_ARRAY;

        private int _code;

        public BasicDeserializer(int code)
        {
            _code = code;
        }

        public override Type GetTargetType()
        {
            switch (_code)
            {
                case NULL:
                    return typeof(void);
                case BOOLEAN:
                    return typeof(bool);
                case BYTE:
                    return typeof(byte);
                case SHORT:
                    return typeof(short);
                case INTEGER:
                    return typeof(int);
                case LONG:
                    return typeof(long);
                case FLOAT:
                    return typeof(float);
                case DOUBLE:
                    return typeof(double);
                case CHARACTER:
                    return typeof(char);
                case CHARACTER_OBJECT:
                    return typeof(char);
                case STRING:
                    return typeof(string);
                case DATE:
                    return typeof(DateTime);
                case NUMBER:
                    return typeof(decimal);
                case OBJECT:
                    return typeof(object);

                case BOOLEAN_ARRAY:
                    return typeof(bool[]);
                case BYTE_ARRAY:
                    return typeof(byte[]);
                case SHORT_ARRAY:
                    return typeof(short[]);
                case INTEGER_ARRAY:
                    return typeof(int[]);
                case LONG_ARRAY:
                    return typeof(int[]);
                case FLOAT_ARRAY:
                    return typeof(float[]);
                case DOUBLE_ARRAY:
                    return typeof(double[]);
                case CHARACTER_ARRAY:
                    return typeof(char[]);
                case STRING_ARRAY:
                    return typeof(string[]);
                case OBJECT_ARRAY:
                    return typeof(object[]);
                default:
                    throw new NotSupportedException();
            }
        }

        public object ReadObject(AbstractHessianInput input)
        {
            switch (_code)
            {
                case NULL:
                    // hessian/3490
                    input.ReadObject();

                    return null;

                case BOOLEAN:
                    return input.ReadBoolean();

                case BYTE:
                    return (byte)input.ReadInt();

                case SHORT:
                    return (short)input.ReadInt();

                case INTEGER:
                    return input.ReadInt();

                case LONG:
                    return input.ReadLong();

                case FLOAT:
                    return (float)input.ReadDouble();

                case DOUBLE:
                    return input.ReadDouble();

                case STRING:
                    return input.ReadString();

                case OBJECT:
                    return input.ReadObject();

                case CHARACTER:
                    {
                        string s = input.ReadString();
                        if (s == null || s.Equals(""))
                            return (char)0;
                        else
                            return s[0];
                    }

                case CHARACTER_OBJECT:
                    {
                        string s = input.ReadString();
                        if (s == null || s.Equals(""))
                            return null;
                        else
                            return s[0];
                    }

                case DATE:
                    return new DateTime(input.ReadUTCDate(), DateTimeKind.Utc);

                case NUMBER:
                    return input.ReadObject();

                case BYTE_ARRAY:
                    return input.ReadBytes();

                case CHARACTER_ARRAY:
                    {
                        string s = input.ReadString();

                        if (s == null)
                            return null;
                        else
                        {
                            int len = s.Length;
                            char[] chars = new char[len];
                            s.CopyTo(0, chars, 0, len);
                            return chars;
                        }
                    }

                case BOOLEAN_ARRAY:
                case SHORT_ARRAY:
                case INTEGER_ARRAY:
                case LONG_ARRAY:
                case FLOAT_ARRAY:
                case DOUBLE_ARRAY:
                case STRING_ARRAY:
                    {
                        int code = input.ReadListStart();

                        switch (code)
                        {
                            case 'N':
                                return null;

                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x13:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                            case 0x17:
                            case 0x18:
                            case 0x19:
                            case 0x1a:
                            case 0x1b:
                            case 0x1c:
                            case 0x1d:
                            case 0x1e:
                            case 0x1f:
                                int length = code - 0x10;
                                input.ReadInt();

                                return ReadLengthList(input, length);

                            default:
                                string type = input.ReadType();
                                length = input.ReadLength();

                                return ReadList(input, length);
                        }
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        public object ReadList(AbstractHessianInput input, int length)
        {
            switch (_code)
            {
                case BOOLEAN_ARRAY:
                    {
                        if (length >= 0)
                        {
                            bool[] data = new bool[length];

                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadBoolean();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add(input.ReadBoolean());

                            input.ReadEnd();

                            bool[] data = new bool[list.Count];

                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = (bool)list[i];

                            return data;
                        }
                    }

                case SHORT_ARRAY:
                    {
                        if (length >= 0)
                        {
                            short[] data = new short[length];

                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = (short)input.ReadInt();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add((short)input.ReadInt());

                            input.ReadEnd();

                            short[] data = new short[list.Count];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (short)list[i];

                            input.AddRef(data);

                            return data;
                        }
                    }

                case INTEGER_ARRAY:
                    {
                        if (length >= 0)
                        {
                            int[] data = new int[length];

                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadInt();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add(input.ReadInt());


                            input.ReadEnd();

                            int[] data = new int[list.Count];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (int)list[i];

                            input.AddRef(data);

                            return data;
                        }
                    }

                case LONG_ARRAY:
                    {
                        if (length >= 0)
                        {
                            long[] data = new long[length];

                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadLong();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add(input.ReadLong());

                            input.ReadEnd();

                            long[] data = new long[list.Count];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (long)list[i];

                            input.AddRef(data);

                            return data;
                        }
                    }

                case FLOAT_ARRAY:
                    {
                        if (length >= 0)
                        {
                            float[] data = new float[length];
                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = (float)input.ReadDouble();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add((float)input.ReadDouble());

                            input.ReadEnd();

                            float[] data = new float[list.Count];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (float)list[i];

                            input.AddRef(data);

                            return data;
                        }
                    }

                case DOUBLE_ARRAY:
                    {
                        if (length >= 0)
                        {
                            double[] data = new double[length];
                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadDouble();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add(input.ReadDouble());

                            input.ReadEnd();

                            double[] data = new double[list.Count];
                            input.AddRef(data);
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (double)list[i];

                            return data;
                        }
                    }

                case STRING_ARRAY:
                    {
                        if (length >= 0)
                        {
                            string[] data = new string[length];
                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadString();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            while (!input.IsEnd())
                                list.Add(input.ReadString());

                            input.ReadEnd();

                            string[] data = new string[list.Count];
                            input.AddRef(data);
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (string)list[i];

                            return data;
                        }
                    }

                case OBJECT_ARRAY:
                    {
                        if (length >= 0)
                        {
                            object[] data = new object[length];
                            input.AddRef(data);

                            for (int i = 0; i < data.Length; i++)
                                data[i] = input.ReadObject();

                            input.ReadEnd();

                            return data;
                        }
                        else
                        {
                            ArrayList list = new ArrayList();

                            input.AddRef(list); // XXX: potential issues here

                            while (!input.IsEnd())
                                list.Add(input.ReadObject());

                            input.ReadEnd();

                            object[] data = new object[list.Count];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = (object)list[i];

                            return data;
                        }
                    }

                default:
                    throw new NotSupportedException(ToString());
            }
        }

        public object ReadLengthList(AbstractHessianInput input, int length)
        {
            switch (_code)
            {
                case BOOLEAN_ARRAY:
                    {
                        bool[] data = new bool[length];

                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadBoolean();

                        return data;
                    }

                case SHORT_ARRAY:
                    {
                        short[] data = new short[length];

                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = (short)input.ReadInt();

                        return data;
                    }

                case INTEGER_ARRAY:
                    {
                        int[] data = new int[length];

                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadInt();

                        return data;
                    }

                case LONG_ARRAY:
                    {
                        long[] data = new long[length];

                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadLong();

                        return data;
                    }

                case FLOAT_ARRAY:
                    {
                        float[] data = new float[length];
                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = (float)input.ReadDouble();

                        return data;
                    }

                case DOUBLE_ARRAY:
                    {
                        double[] data = new double[length];
                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadDouble();

                        return data;
                    }

                case STRING_ARRAY:
                    {
                        string[] data = new string[length];
                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadString();

                        return data;
                    }

                case OBJECT_ARRAY:
                    {
                        object[] data = new object[length];
                        input.AddRef(data);

                        for (int i = 0; i < data.Length; i++)
                            data[i] = input.ReadObject();

                        return data;
                    }

                default:
                    throw new NotSupportedException(ToString());
            }
        }

        public string ToString()
        {
            return GetType().Name + "[" + _code + "]";
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing an object for known object types.
    /// </summary>
    public class BasicSerializer : AbstractSerializer : ObjectSerializer
    {
        public const int NULL = 0;
        public const int BOOLEAN = NULL + 1;
        public const int BYTE = BOOLEAN + 1;
        public const int SHORT = BYTE + 1;
        public const int INTEGER = SHORT + 1;
        public const int LONG = INTEGER + 1;
        public const int FLOAT = LONG + 1;
        public const int DOUBLE = FLOAT + 1;
        public const int CHARACTER = DOUBLE + 1;
        public const int CHARACTER_OBJECT = CHARACTER + 1;
        public const int STRING = CHARACTER_OBJECT + 1;
        public const int STRING_BUILDER = STRING + 1;
        public const int DATE = STRING_BUILDER + 1;
        public const int NUMBER = DATE + 1;
        public const int OBJECT = NUMBER + 1;

        public const int BOOLEAN_ARRAY = OBJECT + 1;
        public const int BYTE_ARRAY = BOOLEAN_ARRAY + 1;
        public const int SHORT_ARRAY = BYTE_ARRAY + 1;
        public const int INTEGER_ARRAY = SHORT_ARRAY + 1;
        public const int LONG_ARRAY = INTEGER_ARRAY + 1;
        public const int FLOAT_ARRAY = LONG_ARRAY + 1;
        public const int DOUBLE_ARRAY = FLOAT_ARRAY + 1;
        public const int CHARACTER_ARRAY = DOUBLE_ARRAY + 1;
        public const int STRING_ARRAY = CHARACTER_ARRAY + 1;
        public const int OBJECT_ARRAY = STRING_ARRAY + 1;

        public const int BYTE_HANDLE = OBJECT_ARRAY + 1;
        public const int SHORT_HANDLE = BYTE_HANDLE + 1;
        public const int FLOAT_HANDLE = SHORT_HANDLE + 1;

        private static readonly BasicSerializer BYTE_HANDLE_SERIALIZER
          = new BasicSerializer(BYTE_HANDLE);

        private static readonly BasicSerializer SHORT_HANDLE_SERIALIZER
          = new BasicSerializer(SHORT_HANDLE);

        private static readonly BasicSerializer FLOAT_HANDLE_SERIALIZER
          = new BasicSerializer(FLOAT_HANDLE);

        private int _code;

        public BasicSerializer(int code)
        {
            _code = code;
        }

        public ISerializer GetObjectSerializer()
        {
            switch (_code)
            {
                case BYTE:
                    return BYTE_HANDLE_SERIALIZER;
                case SHORT:
                    return SHORT_HANDLE_SERIALIZER;
                case FLOAT:
                    return FLOAT_HANDLE_SERIALIZER;
                default:
                    return this;
            }
        }

        public void WriteObject(object obj, AbstractHessianOutput output)
        {
            switch (_code)
            {
                case BOOLEAN:
                    output.WriteBoolean(((Boolean)obj).BoolValue());
                    break;

                case BYTE:
                case SHORT:
                case INTEGER:
                    output.WriteInt(((Number)obj).IntValue());
                    break;

                case LONG:
                    output.WriteLong(((Number)obj).LongValue());
                    break;

                case FLOAT:
                case DOUBLE:
                    output.WriteDouble(((Number)obj).DoubleValue());
                    break;

                case CHARACTER:
                case CHARACTER_OBJECT:
                    output.WriteString(String.ValueOf(obj));
                    break;

                case STRING:
                    output.WriteString((String)obj);
                    break;

                case STRING_BUILDER:
                    output.WriteString(((StringBuilder)obj).ToString());
                    break;

                case DATE:
                    output.WriteUTCDate(((Date)obj).GetTime());
                    break;

                case BOOLEAN_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        bool[] data = (bool[])obj;
                        bool hasEnd = output.WriteListBegin(data.Length, "[bool");
                        for (int i = 0; i < data.Length; i++)
                            output.WriteBoolean(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();

                        break;
                    }

                case BYTE_ARRAY:
                    {
                        byte[] data = (byte[])obj;
                        output.WriteBytes(data, 0, data.Length);
                        break;
                    }

                case SHORT_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        short[] data = (short[])obj;
                        bool hasEnd = output.WriteListBegin(data.Length, "[short");

                        for (int i = 0; i < data.Length; i++)
                            output.WriteInt(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case INTEGER_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        int[] data = (int[])obj;

                        bool hasEnd = output.WriteListBegin(data.Length, "[int");

                        for (int i = 0; i < data.Length; i++)
                            output.WriteInt(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();

                        break;
                    }

                case LONG_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        long[] data = (long[])obj;

                        bool hasEnd = output.WriteListBegin(data.Length, "[long");

                        for (int i = 0; i < data.Length; i++)
                            output.WriteLong(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case FLOAT_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        float[] data = (float[])obj;

                        bool hasEnd = output.WriteListBegin(data.Length, "[float");

                        for (int i = 0; i < data.Length; i++)
                            output.WriteDouble(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case DOUBLE_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        double[] data = (double[])obj;
                        bool hasEnd = output.WriteListBegin(data.Length, "[double");

                        for (int i = 0; i < data.Length; i++)
                            output.WriteDouble(data[i]);

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case STRING_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        string[] data = (string[])obj;

                        bool hasEnd = output.WriteListBegin(data.Length, "[string");

                        for (int i = 0; i < data.Length; i++)
                        {
                            output.WriteString(data[i]);
                        }

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case CHARACTER_ARRAY:
                    {
                        char[] data = (char[])obj;
                        output.WriteString(data, 0, data.Length);
                        break;
                    }

                case OBJECT_ARRAY:
                    {
                        if (output.AddRef(obj))
                            return;

                        object[] data = (object[])obj;

                        bool hasEnd = output.WriteListBegin(data.Length, "[object");

                        for (int i = 0; i < data.Length; i++)
                        {
                            output.WriteObject(data[i]);
                        }

                        if (hasEnd)
                            output.WriteListEnd();
                        break;
                    }

                case NULL:
                    output.WriteNull();
                    break;

                case OBJECT:
                    ObjectHandleSerializer.SER.WriteObject(obj, out);
                    break;

                case BYTE_HANDLE:
                    output.WriteObject(new ByteHandle((Byte)obj));
                    break;

                case SHORT_HANDLE:
                    output.WriteObject(new ShortHandle((Short)obj));
                    break;

                case FLOAT_HANDLE:
                    output.WriteObject(new FloatHandle((Float)obj));
                    break;

                default:
                    throw new RuntimeException(_code + " unknown code for " + obj.GetClass());
            }
        }
    }

}
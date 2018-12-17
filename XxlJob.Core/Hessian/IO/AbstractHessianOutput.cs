using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Abstract output stream for Hessian requests.
    ///
    /// <code>
    /// OutputStream os = ...; // from http connection
    /// AbstractOutput out = new HessianSerializerOutput(os);
    /// string value;
    ///
    /// out.StartCall("hello");  // start hello call
    /// out.WriteString("arg1"); // write a string argument
    /// out.CompleteCall();      // complete the call
    /// </code>
    /// </summary>
    public abstract class AbstractHessianOutput
    {
        // serializer factory
        private SerializerFactory _defaultSerializerFactory;

        // serializer factory
        protected SerializerFactory _serializerFactory;

        private byte[] _byteBuffer;

        /// <summary>
        /// Sets the serializer factory.
        /// </summary>
        public void SetSerializerFactory(SerializerFactory factory)
        {
            _serializerFactory = factory;
        }

        /// <summary>
        /// Gets the serializer factory.
        /// </summary>
        public SerializerFactory GetSerializerFactory()
        {
            // the default serializer factory cannot be modified by external
            // callers
            if (_serializerFactory == _defaultSerializerFactory)
            {
                _serializerFactory = new SerializerFactory();
            }

            return _serializerFactory;
        }

        /// <summary>
        /// Gets the serializer factory.
        /// </summary>
        protected SerializerFactory FindSerializerFactory()
        {
            SerializerFactory factory = _serializerFactory;

            if (factory == null)
            {
                factory = SerializerFactory.CreateDefault();
                _defaultSerializerFactory = factory;
                _serializerFactory = factory;
            }

            return factory;
        }

        /// <summary>
        /// Initialize the output with a new underlying stream.
        /// </summary>
        public void Init(Stream outputStream)
        {
        }

        public bool SetUnshared(bool isUnshared)
        {
            throw new NotSupportedException(GetType().Name);
        }

        /// <summary>
        /// Writes a complete method call.
        /// </summary>
        public void Call(string method, object[] args)
        {
            int length = args != null ? args.Length : 0;

            StartCall(method, length);

            for (int i = 0; i < length; i++)
                WriteObject(args[i]);

            CompleteCall();
        }

        /// <summary>
        /// Starts the method call:
        ///
        /// <code><pre>
        /// C
        /// </code></code>
        ///
        /// <param name="method">the method name to call.</param>
        /// </summary>
        public abstract void StartCall();

        /// <summary>
        /// Starts the method call:
        ///
        /// <code><pre>
        /// C string int
        /// </code></code>
        ///
        /// <param name="method">the method name to call.</param>
        /// </summary>
        public abstract void StartCall(string method, int length);

        /// <summary>
        /// For Hessian 2.0, use the Header envelope instead
        ///
        /// @deprecated
        /// </summary>
        public void WriteHeader(string name)
        {
            throw new NotSupportedException(GetType().Name);
        }

        /// <summary>
        /// Writes the method tag.
        ///
        /// <code><pre>
        /// string
        /// </code></code>
        ///
        /// <param name="method">the method name to call.</param>
        /// </summary>
        public abstract void WriteMethod(string method);

        /// <summary>
        /// Completes the method call:
        ///
        /// <code><pre>
        /// </code></code>
        /// </summary>
        public abstract void CompleteCall();

        /// <summary>
        /// Writes a bool value to the stream.  The bool will be written
        /// with the following syntax:
        ///
        /// <code><pre>
        /// T
        /// F
        /// </code></code>
        ///
        /// <param name="value">the bool value to write.</param>
        /// </summary>
        public abstract void WriteBoolean(bool value);

        /// <summary>
        /// Writes an integer value to the stream.  The integer will be written
        /// with the following syntax:
        ///
        /// <code><pre>
        /// I b32 b24 b16 b8
        /// </code></code>
        ///
        /// <param name="value">the integer value to write.</param>
        /// </summary>
        public abstract void WriteInt(int value);

        /// <summary>
        /// Writes a long value to the stream.  The long will be written
        /// with the following syntax:
        ///
        /// <code><pre>
        /// L b64 b56 b48 b40 b32 b24 b16 b8
        /// </code></code>
        ///
        /// <param name="value">the long value to write.</param>
        /// </summary>
        public abstract void WriteLong(long value);

        /// <summary>
        /// Writes a double value to the stream.  The double will be written
        /// with the following syntax:
        ///
        /// <code><pre>
        /// D b64 b56 b48 b40 b32 b24 b16 b8
        /// </code></code>
        ///
        /// <param name="value">the double value to write.</param>
        /// </summary>
        public abstract void WriteDouble(double value);

        /// <summary>
        /// Writes a date to the stream.
        ///
        /// <code><pre>
        /// T  b64 b56 b48 b40 b32 b24 b16 b8
        /// </code></code>
        ///
        /// <param name="time">the date in milliseconds from the epoch in UTC</param>
        /// </summary>
        public abstract void WriteUTCDate(long time);

        /// <summary>
        /// Writes a null value to the stream.
        /// The null will be written with the following syntax
        ///
        /// <code><pre>
        /// N
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteNull();

        /// <summary>
        /// Writes a string value to the stream using UTF-8 encoding.
        /// The string will be written with the following syntax:
        ///
        /// <code><pre>
        /// S b16 b8 string-value
        /// </code></code>
        ///
        /// If the value is null, it will be written as
        ///
        /// <code><pre>
        /// N
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteString(string value);

        /// <summary>
        /// Writes a string value to the stream using UTF-8 encoding.
        /// The string will be written with the following syntax:
        ///
        /// <code><pre>
        /// S b16 b8 string-value
        /// </code></code>
        ///
        /// If the value is null, it will be written as
        ///
        /// <code><pre>
        /// N
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteString(char[] buffer, int offset, int length);

        /// <summary>
        /// Writes a byte array to the stream.
        /// The array will be written with the following syntax:
        ///
        /// <code><pre>
        /// B b16 b18 bytes
        /// </code></code>
        ///
        /// If the value is null, it will be written as
        ///
        /// <code><pre>
        /// N
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteBytes(byte[] buffer);
        /// <summary>
        /// Writes a byte array to the stream.
        /// The array will be written with the following syntax:
        ///
        /// <code><pre>
        /// B b16 b18 bytes
        /// </code></code>
        ///
        /// If the value is null, it will be written as
        ///
        /// <code><pre>
        /// N
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteBytes(byte[] buffer, int offset, int length);

        /// <summary>
        /// Writes a byte buffer to the stream.
        /// </summary>
        public abstract void WriteByteBufferStart();

        /// <summary>
        /// Writes a byte buffer to the stream.
        ///
        /// <code><pre>
        /// b b16 b18 bytes
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteByteBufferPart(byte[] buffer,
                                                 int offset,
                                                 int length);

        /// <summary>
        /// Writes the last chunk of a byte buffer to the stream.
        ///
        /// <code><pre>
        /// b b16 b18 bytes
        /// </code></code>
        ///
        /// <param name="value">the string value to write.</param>
        /// </summary>
        public abstract void WriteByteBufferEnd(byte[] buffer,
                                          int offset,
                                          int length);

        /// <summary>
        /// Writes a full output stream.
        /// </summary>
        public void WriteByteStream(Stream inputStream)
        {
            WriteByteBufferStart();

            if (_byteBuffer == null)
                _byteBuffer = new byte[1024];

            byte[] buffer = _byteBuffer;

            int len;
            while ((len = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (len < buffer.Length)
                {
                    int len2 = inputStream.Read(buffer, len, buffer.Length - len);

                    if (len2 < 0)
                    {
                        WriteByteBufferEnd(buffer, 0, len);
                        return;
                    }

                    len += len2;
                }

                WriteByteBufferPart(buffer, 0, len);
            }

            WriteByteBufferEnd(buffer, 0, 0);
        }

        /// <summary>
        /// Writes a reference.
        ///
        /// <code><pre>
        /// Q int
        /// </code></code>
        ///
        /// <param name="value">the integer value to write.</param>
        /// </summary>
        abstract protected void WriteRef(int value);

        /// <summary>
        /// Removes a reference.
        /// </summary>
        public bool RemoveRef(object obj)
        {
            return false;
        }

        /// <summary>
        /// Replaces a reference from one object to another.
        /// </summary>
        public abstract bool ReplaceRef(object oldRef, object newRef);

        /// <summary>
        /// Adds an object to the reference list.  If the object already exists,
        /// writes the reference, otherwise, the caller is responsible for
        /// the serialization.
        ///
        /// <code><pre>
        /// R b32 b24 b16 b8
        /// </code></code>
        ///
        /// <param name="object">the object to add as a reference.</param>
        ///
        /// <returns>true if the object has already been written.</returns>
        /// </summary>
        public abstract bool AddRef(object obj);

        /// <summary>
        /// </summary>
        public abstract int GetRef(object obj);

        /// <summary>
        /// Resets the references for streaming.
        /// </summary>
        public void ResetReferences()
        {
        }

        /// <summary>
        /// Writes a generic object to the output stream.
        /// </summary>
        public abstract void WriteObject(object obj);

        /// <summary>
        /// Writes the list header to the stream.  List writers will call
        /// <code>writeListBegin</code> followed by the list contents and then
        /// call <code>writeListEnd</code>.
        ///
        /// <code><pre>
        /// V
        ///   x13 java.util.ArrayList   # type
        ///   x93                       # length=3
        ///   x91                       # 1
        ///   x92                       # 2
        ///   x93                       # 3
        /// &lt;/list>
        /// </code></code>
        /// </summary>
        public abstract bool WriteListBegin(int length, string type);

        /// <summary>
        /// Writes the tail of the list to the stream.
        /// </summary>
        public abstract void WriteListEnd();

        /// <summary>
        /// Writes the map header to the stream.  Map writers will call
        /// <code>writeMapBegin</code> followed by the map contents and then
        /// call <code>writeMapEnd</code>.
        ///
        /// <code><pre>
        /// M type (<key> <value>)* Z
        /// </code></code>
        /// </summary>
        public abstract void WriteMapBegin(string type);

        /// <summary>
        /// Writes the tail of the map to the stream.
        /// </summary>
        public abstract void WriteMapEnd();

        /// <summary>
        /// Writes the object header to the stream (for Hessian 2.0), or a
        /// Map for Hessian 1.0.  object writers will call
        /// <code>writeObjectBegin</code> followed by the map contents and then
        /// call <code>writeObjectEnd</code>.
        ///
        /// <code><pre>
        /// C type int <key>*
        /// C int <value>*
        /// </code></code>
        ///
        /// <returns>true if the object has already been defined.</returns>
        /// </summary>
        public int WriteObjectBegin(string type)
        {
            WriteMapBegin(type);

            return -2;
        }

        /// <summary>
        /// Writes the end of the class.
        /// </summary>
        public void WriteClassFieldLength(int len)
        {
        }

        /// <summary>
        /// Writes the tail of the object to the stream.
        /// </summary>
        public void WriteObjectEnd()
        {
        }

        public void WriteReply(object o)
        {
            StartReply();
            WriteObject(o);
            CompleteReply();
        }


        public void StartReply()
        {
        }

        public void CompleteReply()
        {
        }

        public void WriteFault(string code, string message, object detail)
        {
        }

        public void Flush()
        {
        }

        public void Close()
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Hessian.IO
{
    /// <summary>
    /// Abstract base class for Hessian requests.  Hessian users should only
    /// need to use the methods in this class.
    /// <code>
    /// AbstractHessianInput in = ...; // get input
    /// string value;
    ///
    /// in.StartReply();         // read reply header
    /// value = in.ReadString(); // read string value
    /// in.CompleteReply();      // read reply footer
    /// </code>
    /// </summary>
    public abstract class AbstractHessianInput
    {
        private HessianRemoteResolver resolver;
        private byte[] _buffer;

        /// <summary>
        /// Initialize the Hessian stream with the underlying input stream.
        /// </summary>
        public void Init(Stream inputStream)
        {
        }

        /// <summary>
        /// Returns the call's method
        /// </summary>
        public abstract string GetMethod();

        /// <summary>
        /// Sets the resolver used to lookup remote objects.
        /// </summary>
        public void SetRemoteResolver(HessianRemoteResolver resolver)
        {
            this.resolver = resolver;
        }

        /// <summary>
        /// Sets the resolver used to lookup remote objects.
        /// </summary>
        public HessianRemoteResolver GetRemoteResolver()
        {
            return resolver;
        }

        /// <summary>
        /// Sets the serializer factory.
        /// </summary>
        public void SetSerializerFactory(SerializerFactory ser)
        {
        }

        /// <summary>
        /// Reads the call
        ///
        /// <code>
        /// c major minor
        /// </code>
        /// </summary>
        public abstract int ReadCall();

        /// <summary>
        /// For backward compatibility with HessianSkeleton
        /// </summary>
        public void SkipOptionalCall()
        {
        }

        /// <summary>
        /// Reads a header, returning null if there are no headers.
        ///
        /// <code>
        /// H b16 b8 value
        /// </code>
        /// </summary>
        public abstract string ReadHeader();

        /// <summary>
        /// Starts reading the call
        ///
        /// <para/>A successful completion will have a single value:
        ///
        /// <code>
        /// m b16 b8 method
        /// </code>
        /// </summary>
        public abstract string ReadMethod();

        /// <summary>
        /// Reads the number of method arguments
        ///
        /// <returns>-1 for a variable length (hessian 1.0)</returns>
        /// </summary>
        public int ReadMethodArgLength()
        {
            return -1;
        }

        /// <summary>
        /// Starts reading the call, including the headers.
        ///
        /// <para/>The call expects the following protocol data
        ///
        /// <code>
        /// c major minor
        /// m b16 b8 method
        /// </code>
        /// </summary>
        public abstract void StartCall();

        /// <summary>
        /// Completes reading the call
        ///
        /// <para/>The call expects the following protocol data
        ///
        /// <code>
        /// Z
        /// </code>
        /// </summary>
        public abstract void CompleteCall();

        /// <summary>
        /// Reads a reply as an object.
        /// If the reply has a fault, exception.
        /// </summary>
        public abstract object ReadReply(Type expectedClass);

        /// <summary>
        /// Starts reading the reply
        ///
        /// <para/>A successful completion will have a single value:
        ///
        /// <code>
        /// r
        /// v
        /// </code>
        /// </summary>
        public abstract void StartReply();

        /// <summary>
        /// Starts reading the body of the reply, i.e. after the 'r' has been
        /// parsed.
        /// </summary>
        public void StartReplyBody()
        {
        }

        /// <summary>
        /// Completes reading the call
        ///
        /// <para/>A successful completion will have a single value:
        ///
        /// <code>
        /// z
        /// </code>
        /// </summary>
        public abstract void CompleteReply();

        /// <summary>
        /// Reads a bool
        ///
        /// <code>
        /// T
        /// F
        /// </code>
        /// </summary>
        public abstract bool ReadBoolean();

        /// <summary>
        /// Reads a null
        ///
        /// <code>
        /// N
        /// </code>
        /// </summary>
        public abstract void ReadNull();

        /// <summary>
        /// Reads an integer
        ///
        /// <code>
        /// I b32 b24 b16 b8
        /// </code>
        /// </summary>
        public abstract int ReadInt();

        /// <summary>
        /// Reads a long
        ///
        /// <code>
        /// L b64 b56 b48 b40 b32 b24 b16 b8
        /// </code>
        /// </summary>
        public abstract long ReadLong();

        /// <summary>
        /// Reads a double.
        ///
        /// <code>
        /// D b64 b56 b48 b40 b32 b24 b16 b8
        /// </code>
        /// </summary>
        public abstract double ReadDouble();

        /// <summary>
        /// Reads a date.
        ///
        /// <code>
        /// T b64 b56 b48 b40 b32 b24 b16 b8
        /// </code>
        /// </summary>
        public abstract long ReadUTCDate();

        /// <summary>
        /// Reads a string encoded in UTF-8
        ///
        /// <code>
        /// s b16 b8 non-readonly string chunk
        /// S b16 b8 readonly string chunk
        /// </code>
        /// </summary>
        public abstract string ReadString();

        /// <summary>
        /// Reads an XML node encoded in UTF-8
        ///
        /// <code>
        /// x b16 b8 non-readonly xml chunk
        /// X b16 b8 readonly xml chunk
        /// </code>
        /// </summary>
        public XmlNode ReadNode()
        {
            throw new NotSupportedException(GetType().Name);
        }

        /// <summary>
        /// Starts reading a string.  All the characters must be read before
        /// calling the next method.  The actual characters will be read with
        /// the reader's Read() or Read(char[] , int, int).
        ///
        /// <code>
        /// s b16 b8 non-readonly string chunk
        /// S b16 b8 readonly string chunk
        /// </code>
        /// </summary>
        public abstract StringReader GetReader();

        /// <summary>
        /// Starts reading a byte array using an input stream.  All the bytes
        /// must be read before calling the following method.
        ///
        /// <code>
        /// b b16 b8 non-readonly binary chunk
        /// B b16 b8 readonly binary chunk
        /// </code>
        /// </summary>
        public abstract Stream ReadInputStream();

        /// <summary>
        /// Reads data to an output stream.
        ///
        /// <code>
        /// b b16 b8 non-readonly binary chunk
        /// B b16 b8 readonly binary chunk
        /// </code>
        /// </summary>
        public bool ReadToOutputStream(Stream outputStream)
        {
            var inputStream = ReadInputStream();

            if (inputStream == null)
                return false;

            if (_buffer == null)
                _buffer = new byte[256];

            try
            {
                int len;

                while ((len = inputStream.Read(_buffer, 0, _buffer.Length)) > 0)
                {
                    outputStream.Write(_buffer, 0, len);
                }

                return true;
            }
            finally
            {
                inputStream.Close();
            }
        }



        /// <summary>
        /// Reads a byte array.
        ///
        /// <code>
        /// b b16 b8 non-readonly binary chunk
        /// B b16 b8 readonly binary chunk
        /// </code>
        /// </summary>
        public abstract byte[] ReadBytes();

        /// <summary>
        /// Reads an arbitrary object from the input stream.
        ///
        /// <param name="expectedClass">the expected class if the protocol doesn't supply it.</param>
        /// </summary>
        public abstract object ReadObject(Type expectedClass);

        /// <summary>
        /// Reads an arbitrary object from the input stream.
        /// </summary>
        public abstract object ReadObject();

        /// <summary>
        /// Reads a remote object reference to the stream.  The type is the
        /// type of the remote interface.
        ///
        /// <code><pre>
        /// 'r' 't' b16 b8 type url
        /// </code></code>
        /// </summary>
        public abstract object ReadRemote();

        /// <summary>
        /// Reads a reference
        ///
        /// <code>
        /// R b32 b24 b16 b8
        /// </code>
        /// </summary>
        public abstract object ReadRef();

        /// <summary>
        /// Adds an object reference.
        /// </summary>
        public abstract int AddRef(object obj);

        /// <summary>
        /// Sets an object reference.
        /// </summary>
        public abstract void SetRef(int i, object obj);

        /// <summary>
        /// Resets the references for streaming.
        /// </summary>
        public void ResetReferences()
        {
        }

        /// <summary>
        /// Reads the start of a list
        /// </summary>
        public abstract int ReadListStart();

        /// <summary>
        /// Reads the length of a list.
        /// </summary>
        public abstract int ReadLength();

        /// <summary>
        /// Reads the start of a map
        /// </summary>
        public abstract int ReadMapStart();

        /// <summary>
        /// Reads an object type.
        /// </summary>
        public abstract string ReadType();

        /// <summary>
        /// Returns true if the data has ended.
        /// </summary>
        public abstract bool IsEnd();

        /// <summary>
        /// Read the end byte
        /// </summary>
        public abstract void ReadEnd();

        /// <summary>
        /// Read the end byte
        /// </summary>
        public abstract void ReadMapEnd();

        /// <summary>
        /// Read the end byte
        /// </summary>
        public abstract void ReadListEnd();

        public void Close()
        {
        }
    }
}
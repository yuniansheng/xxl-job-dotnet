using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Deserializing an object. Custom deserializers should extend
    /// from AbstractDeserializer to avoid issues with signature
    /// changes.
    /// </summary>
    public interface IDeserializer
    {
        Type GetTargetType();

        bool IsReadResolve();

        object ReadObject(AbstractHessianInput input);

        object ReadList(AbstractHessianInput input, int length);

        object ReadLengthList(AbstractHessianInput input, int length);

        object ReadMap(AbstractHessianInput input);

        /// <summary>
        /// Creates an empty array for the deserializers field
        /// entries.
        /// <param name="len">number of fields to be read</param>
        /// <returns>empty array of the proper field type.</returns>
        /// </summary>
        object[] CreateFields(int len);

        /// <summary>
        /// Returns the deserializer's field reader for the given name.
        /// <param name="name">the field name</param>
        /// <returns>the deserializer's internal field reader</returns>
        /// </summary>
        object CreateField(string name);

        /// <summary>
        /// Reads the object from the input stream, given the field
        /// definition.
        /// <param name="in">the input stream</param>
        /// <param name="fields">the deserializer's own field marshal</param>
        /// <returns>the new object</returns>
        /// </summary>
        object ReadObject(AbstractHessianInput input, object[] fields);

        object ReadObject(AbstractHessianInput input, string[] fieldNames);
    }
}
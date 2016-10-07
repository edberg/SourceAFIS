using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SourceAFIS.General
{
    [ComVisible(true)]
    [Serializable()]
    public class ApplicationException : Exception
    {
        public ApplicationException() : base()
        {
        }

        public ApplicationException(string message) : base(message)
        {
        }

        public ApplicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

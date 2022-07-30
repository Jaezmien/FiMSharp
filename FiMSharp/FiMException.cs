using System;

namespace FiMSharp
{
    /// <summary>
    /// For usage with exceptions propagates up to a <c>try { } catch { } statement</c> and add the proper line number.
    /// </summary>
    public class FiMException : Exception
    {
        public FiMException() { }
        public FiMException(string message) : base(message) { }
        public FiMException(string message, Exception inner) : base(message, inner) { }
    }
}

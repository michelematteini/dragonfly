using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dragonfly.Graphics
{
    public class InvalidGraphicCallException : Exception
    {
        public InvalidGraphicCallException(string msg)
            : base(msg)
        { }

        public InvalidGraphicCallException()
        { }
    }
}

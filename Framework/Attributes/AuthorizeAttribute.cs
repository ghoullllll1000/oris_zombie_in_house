using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AuthorizeAttribute : Attribute
    {
    }
}

using System;

namespace Banchou {
    /// <summary>
    /// Action classes with this attribute are not sent to network clients
    /// </summary>
    public class LocalActionAttribute : Attribute { }
}
using System;

namespace Pathfinder.Core.DI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectOptionalAttribute : Attribute { }
}

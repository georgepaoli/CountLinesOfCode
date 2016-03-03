using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    public static class StringToValueType
    {
        public static object ToBool(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "no":
                    return false;
                default:
                    return null;
            }
        }

        public static object ToEnum(string s, Type outputType)
        {
            if (!outputType.GetTypeInfo().IsEnum)
            {
                return null;
            }

            s = s.ToLowerInvariant();
            foreach (var name in Enum.GetNames(outputType))
            {
                if (s == name.ToLowerInvariant())
                {
                    return Enum.Parse(outputType, name);
                }
            }

            return null;
        }

        // Nullable is a ValueType too
        private static object ToNullable(string s, Type outputType)
        {
            if (!outputType.GetTypeInfo().IsGenericType || outputType.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
                return null;
            }

            Type wrappedType = outputType.GetGenericArguments()[0];
            object unwrappedValue = ToType(s, wrappedType);
            if (unwrappedValue == null)
            {
                // is it even possible?
                return null;
            }

            return Activator.CreateInstance(outputType, new object[] { unwrappedValue });
        }

        public static object ToType(string s, Type outputType)
        {
            if (outputType == typeof(string))
            {
                return s;
            }

            if (outputType == typeof(int))
            {
                int ret;
                return int.TryParse(s, out ret) ? (object)ret : null;
            }

            if (outputType == typeof(bool))
            {
                return ToBool(s);
            }

            return ToEnum(s, outputType) ?? ToNullable(s, outputType);
        }
    }
}

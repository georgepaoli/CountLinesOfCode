using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineArgs
{
    public class ParameterInformation
    {
        // TODO: following two fields should be in specialized version of this class
        public object Target;
        public FieldInfo Field;

        // Supported parameter metadata
        // TODO: Should all these metadata be auto generated from custom attributes?
        public Names Names = new Names();
        // TODO: How should Required work with popping args?
        public RequiredAttribute Required = null;
        public bool PopsRemainingArgs = false;
        public bool NoDefaultAlias = false;
        public LastProcessedNamedArgAttribute StopProcessingNamedArgsAfterThis = null;
        public VerbAttribute IsVerb = null;
        public HashSet<char> CombinableSingleLetterAliases = new HashSet<char>();
        public DescriptionAttribute Description = null;
        // TODO: should this value be resetable (and immutable)
        public int MaxArgsToPop = 0;

        // Output - is this info relevant
        public int NumberOfArgsBound = 0;

        public ParameterInformation(object target, FieldInfo field)
        {
            Target = target;
            Field = field;
            Names.GetDefaultNames = () => $"<{field.Name}>";

            foreach (var customAttribute in field.GetCustomAttributes())
            {
                var asAlias = customAttribute as AliasAttribute;
                if (asAlias != null)
                {
                    Names.AddRange(asAlias.Names);
                    foreach (var name in Names)
                    {
                        if (name.Length == 1)
                        {
                            CombinableSingleLetterAliases.Add(name[0]);
                        }
                    }
                }

                Required = (customAttribute as RequiredAttribute) ?? Required;

                if (customAttribute as PopArgAttribute != null)
                {
                    MaxArgsToPop++;
                }

                PopsRemainingArgs |= customAttribute as PopRemainingArgsAttribute != null;
                StopProcessingNamedArgsAfterThis = (customAttribute as LastProcessedNamedArgAttribute) ?? StopProcessingNamedArgsAfterThis;
                NoDefaultAlias |= customAttribute as NoDefaultAliasAttribute != null;
                IsVerb = (customAttribute as VerbAttribute) ?? IsVerb;

                Description = (customAttribute as DescriptionAttribute) ?? Description;
            }

            if (!NoDefaultAlias)
            {
                if (field.Name.Length == 1)
                {
                    CombinableSingleLetterAliases.Add(field.Name[0]);
                }

                Names.AddRange((new AliasAttribute(field.Name)).Names);
            }

            if (!Field.FieldType.IsAssignableFrom(typeof(bool)))
            {
                CombinableSingleLetterAliases.Clear();
            }
        }

        private bool TryAddValueToList(string value)
        {
            if (!Field.FieldType.GetTypeInfo().IsGenericType || Field.FieldType.GetGenericTypeDefinition() != typeof(List<>))
            {
                return false;
            }

            // This must have exactly one arg - this is List<T>
            Type underlyingType = Field.FieldType.GetGenericArguments()[0];

            object resolved = StringToValueType.ToType(value, underlyingType);
            if (resolved == null)
            {
                return false;
            }

            IList list = Field.GetValue(Target) as IList;
            if (list == null)
            {
                list = (IList)Activator.CreateInstance(Field.FieldType);
                Field.SetValue(Target, list);
            }

            return list.Add(resolved) != -1;
        }

        private bool TryAddValueToField(string value)
        {
            if (NumberOfArgsBound >= 1)
            {
                return false;
            }

            object resolved = StringToValueType.ToType(value, Field.FieldType);
            if (resolved == null)
            {
                return false;
            }

            NumberOfArgsBound++;
            Field.SetValue(Target, resolved);

            return true;
        }

        public bool TryBindValue(string value)
        {
            if (value == null)
            {
                return false;
            }

            if (TryAddValueToField(value))
            {
                return true;
            }

            if (TryAddValueToList(value))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!Required)
            {
                sb.Append("[");
            }

            sb.Append(Names.ToString());

            if (!Required)
            {
                sb.Append("]");
            }

            if (Description != null)
            {
                sb.Append(" ");
                sb.Append(Description);
            }

            return sb.ToString();
        }
    }
}

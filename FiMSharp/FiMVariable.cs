using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FiMSharp.GlobalVars;
using FiMSharp;

namespace FiMSharp
{
    public class FiMVariable
    {
        public FiMVariable(object Value, VariableTypes Type, bool IsConstant = false, bool IsArray = false)
        {
            this.Value = Value;
            this.Type = Type;
            IS_CONSTANT = IsConstant;
            IS_ARRAY = IsArray;
        }
        public FiMVariable(VariableTypes Type, bool IsConstant = false, bool IsArray = false)
        {
            this.Type = Type;
            IS_CONSTANT = IsConstant;
            IS_ARRAY = IsArray;
        }

        private bool IS_CONSTANT = false;
        private bool IS_ARRAY = false;

        private object Value = null;
        public VariableTypes Type = VariableTypes.UNDEFINED;

        public bool IsArray() => this.IS_ARRAY;

        // Getters
        public (object, VariableTypes) GetValue(int array_index = 0) // 1-based array index
        {
            if( IS_ARRAY && array_index < 1 )
                throw new Exception("[Internal] GetValue on Array variables need an index");

            if( this.Type == VariableTypes.STRING && array_index > 0 )
            {
                string string_value = (this.Value as string);
                if (array_index >= string_value.Length) return ("", VariableTypes.CHAR);
                return (string_value[array_index], VariableTypes.CHAR);
                
            }

            if( IS_ARRAY )
            {
                var raw_dict = this.Value as Dictionary<int,object>;
                if( this.Type == VariableTypes.BOOLEAN_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return (false, VariableTypes.BOOLEAN);
                    return (raw_dict[array_index], VariableTypes.BOOLEAN);
                }
                if (this.Type == VariableTypes.FLOAT_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return (0, VariableTypes.INTEGER);
                    return (raw_dict[array_index], VariableTypes.INTEGER);
                }
                if (this.Type == VariableTypes.STRING_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return ("", VariableTypes.STRING);
                    return (raw_dict[array_index], VariableTypes.STRING);
                }
            }

            return (this.Value, this.Type);
        }
        public object GetRawValue() => this.Value;
        public void SetValue(object value)
        {
            if( this.IS_CONSTANT && this.Value != FiMMethods.GetNullValue( this.Type ) )
                throw new Exception("[Internal] Can only use SetValue if constant variable has no values");
            this.Value = value;
        }
        public void SetArrayValue(int array_index, object value) {
            if( !IS_ARRAY )
                throw new Exception("[Internal] SetArrayValue must be used on an array");
            var raw_dict = this.Value as Dictionary<int,object>;
            raw_dict[ array_index ] = value;
            /*if( this.Type == VariableTypes.BOOLEAN_ARRAY)
            {
                (raw_array.Cast<bool>().ToList())[ array_index ] = (bool)value;
            }
            if (this.Type == VariableTypes.FLOAT_ARRAY)
            {
                float new_value = 0;
                if (array_index < raw_array.Count)
                    new_value = (raw_array.Cast<float>().ToList())[ array_index ];
            }
            if (this.Type == VariableTypes.STRING_ARRAY)
            {
                string new_value = "";
                if (array_index < raw_array.Count)
                    new_value = (raw_array.Cast<string>().ToList())[ array_index ];
            }*/
        }
    }
}
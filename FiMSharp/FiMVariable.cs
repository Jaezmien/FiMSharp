﻿using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Core
{
    public class FiMVariable
    {
        private readonly bool IS_CONSTANT = false;
        private readonly bool IS_ARRAY = false;

        private object _Value = null;
        public VariableTypes Type = VariableTypes.UNDEFINED;

        public FiMVariable(object Value, VariableTypes Type, bool IsConstant = false, bool IsArray = false)
        {
            this._Value = Value;
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

        // Getters
        internal (object, VariableTypes) GetValue(int array_index = 0) // 1-based array index
        {
            if( IS_ARRAY && array_index < 1 )
                throw new Exception("GetValue on Array variables need an index");

            if( this.Type == VariableTypes.STRING && array_index > 0 )
            {
                string string_value = (this._Value as string);
                if (array_index >= string_value.Length) return ("", VariableTypes.CHAR);
                return (string_value[array_index], VariableTypes.CHAR);
                
            }

            if( IS_ARRAY )
            {
                var raw_dict = this._Value as Dictionary<int,object>;
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

            return (this._Value, this.Type);
        }
        internal object GetRawValue() => this._Value;
        internal void SetValue(object value)
        {
            if( this.IS_CONSTANT && this.Value != FiMMethods.GetNullValue( this.Type ) )
                throw new Exception("[Internal] Can only use SetValue if constant variable has no values");
            this._Value = value;
        }
        internal void SetArrayValue(int array_index, object value) {
            if( !IS_ARRAY )
                throw new Exception("[Internal] SetArrayValue must be used on an array");
            var raw_dict = this._Value as Dictionary<int,object>;
            raw_dict[ array_index ] = value;
        }

        // Public getters
        public dynamic Value {
            get {
                switch( this.Type ) {
                    case VariableTypes.STRING: {
                        string _s = Convert.ToString(this._Value);
                        return _s.Substring(1, _s.Length-2);
                    };

                    case VariableTypes.BOOLEAN_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToBoolean(v.Value));
                    case VariableTypes.FLOAT_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToSingle(v.Value));
                    case VariableTypes.STRING_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => {
                            string _s = Convert.ToString(v.Value);
                            return _s.Substring(1, _s.Length-2);
                        });
                    
                    default: return this._Value;
                }
            }
            set {
                this._Value = value;
            }
        }

        /// <summary>
        /// Check if FiMVariable type is an array.
        /// Doesn't include strings.
        /// </summary>
        public bool IsArray {
            get {
                return FiMMethods.IsVariableTypeArray(this.Type);
            }
        }
    }
}
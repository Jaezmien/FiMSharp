using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.GlobalStructs;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Core
{
    public class FiMVariable
    {
        private readonly bool IS_CONSTANT = false;
        private readonly bool IS_ARRAY = false;

        private object _Value = null;

        /// <summary>
        /// The <c>VariableTypes</c> of the variable.
        /// </summary>
        public readonly VariableTypes Type = VariableTypes.UNDEFINED;

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
        internal FiMVariableStruct GetValue(int array_index = 0) // 1-based array index
        {
            if( IS_ARRAY && array_index < 1 ) throw FiMError.CreatePartial( FiMErrorType.INDEX_MUST_BE_POSITIVE );

            if( this.Type == VariableTypes.STRING && array_index > 0 )
            {
                string string_value = (this._Value as string);
                if (array_index >= string_value.Length) return new FiMVariableStruct( "", VariableTypes.CHAR );
                return new FiMVariableStruct( string_value[array_index-1], VariableTypes.CHAR );
            }

            if( IS_ARRAY )
            {
                var raw_dict = this._Value as Dictionary<int,object>;
                if( this.Type == VariableTypes.BOOLEAN_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return new FiMVariableStruct(false, VariableTypes.BOOLEAN );
                    return new FiMVariableStruct( raw_dict[array_index], VariableTypes.BOOLEAN );
                }
                if (this.Type == VariableTypes.DOUBLE_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return new FiMVariableStruct(0, VariableTypes.INTEGER );
                    return new FiMVariableStruct( raw_dict[array_index], VariableTypes.INTEGER );
                }
                if (this.Type == VariableTypes.STRING_ARRAY)
                {
                    if (!raw_dict.ContainsKey( array_index )) return new FiMVariableStruct("", VariableTypes.STRING );
                    return new FiMVariableStruct( raw_dict[array_index], VariableTypes.STRING );
                }
            }

            return new FiMVariableStruct( this._Value, this.Type );
        }
        internal object GetRawValue() => this._Value;
        internal void SetValue(object value)
        {
            if( this.IS_CONSTANT && this._Value != FiMMethods.GetNullValue( this.Type ) )
                throw FiMError.CreatePartial( FiMErrorType.CANNOT_MODIFY_CONSTANT );
            this._Value = value;
        }
        internal void SetArrayValue(int array_index, object value) {
            if( !IS_ARRAY )
                throw new Exception("SetArrayValue must be used on an array");
            var raw_dict = this._Value as Dictionary<int,object>;

            if( FiMMethods.IsNullValue(value) ) raw_dict.Remove( array_index );
            else raw_dict[ array_index ] = value;
        }

        // Public getters

        /// <summary>
        /// Gets/Sets the value of the variable.
        /// </summary>
        public dynamic Value {
            get {
                switch( this.Type ) {
                    case VariableTypes.STRING: return Convert.ToString(this._Value);

                    case VariableTypes.BOOLEAN_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToBoolean(v.Value));
                    case VariableTypes.DOUBLE_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToDouble(v.Value));
                    case VariableTypes.STRING_ARRAY:
                        return (this._Value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
                    
                    default: return this._Value;
                }
            }
            set {
                switch( this.Type ) {
                    case VariableTypes.STRING: {
                        if( value.GetType() != typeof(string) ) throw new ArgumentException();       
                        this._Value = $"{value}";
                    }
                    break;
                    case VariableTypes.INTEGER: {
                        double new_value;
                        try { new_value = Convert.ToDouble(value); } catch( Exception ) { throw new ArgumentException(); }
                        this._Value = new_value;
                    }
                    break;
                    case VariableTypes.BOOLEAN: {
                        if( value.GetType() != typeof(bool) ) throw new ArgumentException();       
                        this._Value = value;
                    }
                    break;
                    case VariableTypes.CHAR: {
                        if( value.GetType() != typeof(char) ) throw new ArgumentException();       
                        this._Value = value;
                    }
                    break;
                    
                    case VariableTypes.BOOLEAN_ARRAY: {
                        if( value.GetType() != typeof(Dictionary<int,bool>) ) throw new ArgumentException();
                        this._Value = value as Dictionary<int, object>;
                    }
                    break;
                    case VariableTypes.DOUBLE_ARRAY: {
                        if( value.GetType() != typeof(Dictionary<int,double>) ) throw new ArgumentException();
                        this._Value = value as Dictionary<int, object>;
                    }
                    break;
                    case VariableTypes.STRING_ARRAY: {
                        if( value.GetType() != typeof(Dictionary<int,string>) ) throw new ArgumentException();
                        this._Value = value as Dictionary<int, object>;
                    }
                    break;
                    
                    case VariableTypes.UNDEFINED: {
                        // Can set variable type here
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Check if FiMVariable type is an array (not including strings).
        /// </summary>
        public bool IsArray {
            get {
                return FiMMethods.IsVariableTypeArray(this.Type);
            }
        }
    }
}
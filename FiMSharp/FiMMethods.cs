using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using FiMSharp.GlobalVars;

namespace FiMSharp
{
    static partial class Extension {
        public static bool HasDecimal(this float Value) {
            return Value != Math.Floor(Value);
        }
    }
    public class FiMMethods
    {
        /// <summary>
        /// Create an "Exception string".
        /// Note that this should only be used if it's expected to have an error.
        /// Actual compiler errors should be a raw Exception string
        /// </summary>
        public static string CreateExceptionString( string message, string line, int line_number = 0 )
        {
            string result = "";

            if (line_number > 0)
                result = $"(Line { line_number }): ";
            result += $"{ message }\n{ line }\n";

            return result;
        }

        private static Regex comment_regex = new Regex(@"^(P\.)(P\.)*(S\.)\s.");
        public static bool IsComment( string line ) => comment_regex.IsMatch( line );

        // yeah
        protected static Regex _pre_arrayVariableSet = new Regex(@"^([^\d]+) (\d+) ((?:is|was|ha[sd]|like[sd]?) )");
        protected static Regex _pre_arrayVariableSetb = new Regex(@"^([^\d]+) (\d+)");
        protected static Regex _pre_arrayVariableSet2 = new Regex(@"^(.+)(?= of) (of) (.+)(?= (?:is|was|ha[sd]|like[sd]?)) (is|was|ha[sd]|like[sd]?) ");
        protected static Regex _pre_arrayVariableSet2b = new Regex(@"^(.+)(?= of) (of) (.+)");

        /// <summary>
        /// [VARIABLE_NAME] [INDEX] [KEYWORD]
        /// </summary>
        public static (string, string, int, string) MatchArray1( string line, bool ignore_keyword = false ) {
            if( !ignore_keyword ) {
                var match = _pre_arrayVariableSet.Match( line );
                string variable_name = match.Groups[1].Value;
                int variable_index = int.Parse( match.Groups[2].Value );
                string keyword = match.Groups[3].Value;

                return( match.Value, variable_name, variable_index, keyword );
            } else {
                var match = _pre_arrayVariableSetb.Match( line );
                string variable_name = match.Groups[1].Value;
                int variable_index = int.Parse( match.Groups[2].Value );
                string keyword = "";

                return( match.Value, variable_name, variable_index, keyword );
            }
        }
        public static bool IsMatchArray1( string line, bool ignore_keyword = false )
            => ignore_keyword ? _pre_arrayVariableSetb.IsMatch( line ) : _pre_arrayVariableSet.IsMatch( line );

        /// <summary>
        /// [VARIABLE_INDEX] [KEYWORD] [VARIABLE_NAME] [KEYWORD`
        /// </summary>
        public static (string, string, string, string) MatchArray2( string line, bool ignore_keyword = false ) {
            if( !ignore_keyword ) {
                var match = _pre_arrayVariableSet2.Match( line );
                string variable_name = match.Groups[3].Value;
                string variable_index = match.Groups[1].Value;
                string keyword = match.Groups[4].Value;
    
                return( match.Value, variable_name, variable_index, keyword );
            } else {
                var match = _pre_arrayVariableSet2b.Match( line );
                string variable_name = match.Groups[3].Value;
                string variable_index = match.Groups[1].Value;
                string keyword = "";

                return( match.Value, variable_name, variable_index, keyword );
            }
        }
        public static bool IsMatchArray2( string line, bool ignore_keyword = false )
            => ignore_keyword ? _pre_arrayVariableSet2b.IsMatch( line ) : _pre_arrayVariableSet2.IsMatch( line );

        public static string RemoveStringParentheses( string line ) {
            if( line.Contains("(") && line.Contains(")") )
            {
                string line_buffer = "";

                bool is_in_comment = false,
                is_in_string = false,
                is_special_char = false;
                for(int index = 0; index < line.Length; index++)
                {
                    char character = line[index];

                    if( is_in_comment )
                    {
                        if (character == ')')
                            is_in_comment = false;
                        continue;
                    }

                    if( character == '\\' && is_in_string )
                    {
                        is_special_char = true;
                    }
                    else if (character == '(' && !is_in_string)
                    {
                        is_in_comment = true;
                    }
                    else if(character == '"')
                    {
                        is_in_string = !is_in_string;
                        line_buffer += character;
                    }
                    else
                    {
                        line_buffer += character;
                        //
                        if (is_special_char) is_special_char = false;
                    }
                }
                return line_buffer.Trim();
            }
            return line;
        }

        public static string SanitizeString( string str, FiMReport report, Dictionary<string, FiMVariable> variables )
        {
            string new_string = "";
            string buffer = "";

            string CheckBuffer( string buffer_, bool isEnd ) {
                buffer = buffer.Trim();
                if( buffer.Length > 0 ) {
                    object value = ParseVariable(buffer, report, variables, out VariableTypes type, isEnd );
                    string _value = value.ToString();

                    // i am going to puke
                    if( 
                        (
                            type == VariableTypes.STRING &&
                            (
                                (_value).StartsWith("\"") &&
                                (_value).EndsWith("\"")
                            )
                        )
                        ||
                        (
                            type == VariableTypes.CHAR &&
                            (
                                (_value).StartsWith("'") &&
                                (_value).EndsWith("'")
                            )
                        )
                    )
                    {
                        return (_value).Substring(1, (_value).Length-2);
                    }
                    return (_value);
                }
                return "";
            }

            bool is_in_string = false;
            bool is_escaped = false;

            foreach(char c in str)
            {
                if( c == '"' && !is_escaped )
                {
                    if( !is_in_string ) {
                        new_string += CheckBuffer( buffer, false );
                    } else {
                        new_string += buffer;
                    }
                    buffer = ""; is_in_string = !is_in_string;
                }
                else if ( c == '\\' && !is_escaped )
                {
                    is_escaped = true;
                }
                else {
                    if( is_escaped ) {
                        if( c == 'n' ) buffer += "\n";
                        else buffer += "\\" + c;
                        is_escaped = false;
                    } else {
                        buffer += c;
                    }
                }
            }

            new_string += CheckBuffer( buffer, true );

            return $"\"{ new_string }\"";
        }

        public static string GetOrdinal( int number ) {
            string s = number.ToString();
            if(s.Length > 1) s = s.Substring(s.Length-1);

            float n = int.Parse(s);
            if( n > 10 && n < 20 ) return "th";
            if( n == 1 ) return "st";
            if( n == 2 ) return "nd";
            if( n == 3 ) return "rd";
            return "th";
        }

        public static VariableTypes GetVariableTypeFromDeclaration( string str, out string keyword )
        {
            if( Globals.Methods.Variable_Boolean_Array.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_Boolean_Array.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.BOOLEAN_ARRAY;
            }
            if( Globals.Methods.Variable_Boolean.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_Boolean.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.BOOLEAN;
            }
            if( Globals.Methods.Variable_Number_Array.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_Number_Array.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.FLOAT_ARRAY;
            }
            if( Globals.Methods.Variable_Number.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_Number.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.INTEGER;
            }
            if( Globals.Methods.Variable_String_Array.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_String_Array.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.STRING_ARRAY;
            }
            if( Globals.Methods.Variable_String.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_String.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.STRING;
            }

            if( Globals.Methods.Variable_Character.Any( x => str.StartsWith(x) ) ) {
                keyword = Globals.Methods.Variable_Character.Where( x => str.StartsWith(x) ).FirstOrDefault();
                return VariableTypes.CHAR;
            }

            throw new Exception("Invalid string");
        }
        public static bool HasVariableTypeDeclaration( string str)
        {
            if( Globals.Methods.Variable_Boolean.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_Boolean_Array.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_Number.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_Number_Array.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_String.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_String_Array.Any( x => str.StartsWith(x) ) )
                return true;
            if( Globals.Methods.Variable_Character.Any( x => str.StartsWith(x) ) )
                return true;

            return false;
        }

        public static bool IsVariableTypeArray( VariableTypes type, bool include_string = false ) {
            List<VariableTypes> check = new List<VariableTypes>() {
                VariableTypes.BOOLEAN_ARRAY,
                VariableTypes.FLOAT_ARRAY,
                VariableTypes.STRING_ARRAY,
            };
            if( include_string ) check.Add( VariableTypes.STRING );

            return check.Any(x => x == type);
        }
        public static VariableTypes VariableTypeArraySubType( VariableTypes type ) {
            if( type == VariableTypes.BOOLEAN_ARRAY ) return VariableTypes.BOOLEAN;
            if( type == VariableTypes.FLOAT_ARRAY ) return VariableTypes.INTEGER;
            if( type == VariableTypes.STRING_ARRAY ) return VariableTypes.STRING;
            if( type == VariableTypes.STRING ) return VariableTypes.CHAR;

            throw new Exception("Invalid Variable Type");
        }

        public static bool ConvertStringToBoolean( string str, out bool result ) {
            if( Globals.Methods.Boolean_True.Any( x => x == str ) ) {
                result = true;
                return true;
            }
            if( Globals.Methods.Boolean_False.Any( x => x == str ) ) {
                result = false;
                return true;
            }
            result = false;
            return false;
        }

        public static object GetNullValue( VariableTypes type ) {
            switch( type ) {
                case VariableTypes.BOOLEAN: return false;
                case VariableTypes.CHAR: return '\0';
                case VariableTypes.STRING: return "";
                case VariableTypes.INTEGER: return 0f;
                default: return null;
            }
        }
        
        /// <summary>
        /// Becareful as some boolean false and integer 0 will return true!
        /// </summary>
        public static bool IsNullValue( object value ) {
            if( value is bool ) return (bool)value == false;
            if( value is char ) return (char)value == '\0';
            if( value is string ) return (string)value == "";
            if( value is float  ) return (float)value == 0f;
            throw new Exception("Tried checking NullValue for " + value + ", failed checking type");
        }
        public static (string, FiMVariable) VariableFromTokenizer( FiMReport report, Dictionary<string, FiMVariable> variables, object args ) {
            string _variable_name;
            VariableTypes _variable_type;
            bool _variable_const;
            bool _variable_array;
            string _variable_value;
            {
                List<object> var_args = args as List<object>;
                _variable_name = (string)var_args[0];
                _variable_type = (VariableTypes)var_args[1];
                _variable_const = (bool)var_args[2];
                _variable_array = (bool)var_args[3];
                _variable_value = (string)var_args[4];
            }
            if( variables.ContainsKey( _variable_name ) ) 
                throw new FiMException( $"Variable { _variable_name } already exists!" );
            
            if( _variable_value.Trim().Length == 0 )
                _variable_value = "nothing";

            object variable_value;
            var v_values = new Dictionary<int,object>();
            if( _variable_array )
            {
                VariableTypes expected_type = VariableTypeArraySubType( _variable_type );
                
                if( _variable_value.Contains(" and ") ) {
                    // Multiple values
                    foreach( string _value in _variable_value.Split(new string[] {" and "}, StringSplitOptions.None) ) {
                        object value = FiMMethods.ParseVariable( _value.Trim(), report, variables, out VariableTypes _got_type, fallback: expected_type );
                        if( _got_type != expected_type ) {
                            throw new FiMException(
                                $"Expected type initializer { expected_type }, got { _got_type }"
                            );
                        }
                        v_values.Add( v_values.Keys.Count+1, value );
                    }
                    variable_value = (object)v_values;
                } else {
                    // Single value
                    object value = FiMMethods.ParseVariable( _variable_value, report, variables, out VariableTypes _got_type, fallback: expected_type );
                    
                    if( IsVariableTypeArray(_got_type) ) {
                        variable_value = value;
                    }
                    else {
                        if( _got_type != expected_type ) {
                            throw new FiMException(
                                $"Expected type initializer { expected_type }, got { _got_type }"
                            );
                        }
                        v_values.Add( v_values.Keys.Count+1, value );
                        variable_value = (object)v_values;
                    }
                }
            }
            else
            {
                object value = FiMMethods.ParseVariable( _variable_value, report, variables, out VariableTypes variable_type, fallback: _variable_type );
                if( variable_type != _variable_type ) {
                    if( variable_type == VariableTypes.STRING ) {
                        value = value.ToString();
                    }
                    else {
                        throw new Exception(
                            $"Expected type initializer { _variable_type }, got { variable_type }"
                        );
                    }
                }

                variable_value = value;
            }

            return (
                _variable_name,
                new FiMVariable(
                    variable_value,
                    _variable_type,
                    _variable_const,
                    _variable_array
                )
            );
        }

        public static (int, FiMVariable) ParseArray( int index, string variable_name, Dictionary<string, FiMVariable> variables ) {
            //if( !variables.ContainsKey(variable_name) )
            //    throw new FiMException( $"Variable { variable_name } doesn't exist!" );
            FiMVariable variable = variables[ variable_name ];
            return (index, variable );
        }
        public static (int, FiMVariable) ParseArray( string variable_index, string variable_name, FiMReport report, Dictionary<string, FiMVariable> variables ) {
            //if( !variables.ContainsKey(variable_name) )
            //  throw new FiMException( $"Variable { variable_name } doesn't exist!" );
            //if( !variables.ContainsKey(variable_index) )
            //    throw new FiMException( $"Variable { variable_index } doesn't exist!" );
            FiMVariable variable = variables[ variable_name ];
            //FiMVariable _variable = variables[ variable_index ];
            object _var = ParseVariable( variable_index, report, variables, out var _var_type );
            if( _var_type != VariableTypes.INTEGER )
                throw new FiMException($"Variable as array index must be a number");
            
            float index = Convert.ToSingle( _var );
            if( index.HasDecimal() )
                throw new FiMException($"Variable as array index must be an integer");
            return ((int)index, variable);
        }

        public static object ParseVariable( string str, FiMReport report, Dictionary<string, FiMVariable> variables, out VariableTypes type, bool run_once = true, VariableTypes fallback = VariableTypes.UNDEFINED) {
            type = VariableTypes.UNDEFINED;

            if( str == "nothing" ) {
                if( fallback != VariableTypes.UNDEFINED ) {
                    type = fallback;
                    return GetNullValue( fallback );
                }
                return null;
            }

            if( str.StartsWith("'") && str.EndsWith("'") ) {
                type = VariableTypes.CHAR;
                return str[1];
            }
            if( str.StartsWith("\"") && str.EndsWith("\"") ) {
                type = VariableTypes.STRING;
                return str;
            }

            // What the fuck is this scoping
            {
                if( float.TryParse( str, out float result ) ) {
                    type = VariableTypes.INTEGER;
                    return result;
                }
            }

            {
                if( ConvertStringToBoolean( str, out bool result ) ) {
                    type = VariableTypes.BOOLEAN;
                    return result;
                }
            }

            if( variables.ContainsKey( str ) ) {
                var variable = variables[ str ];

                type = variable.Type;
                //return variable.GetValue().Item1;
                return variable.GetRawValue();
            }

            if( report.Paragraphs.Keys.Any(x => str.StartsWith(x)) ) {
                FiMParagraph p = report.Paragraphs.Where(x => str.StartsWith(x.Key)).FirstOrDefault().Value;
                string _paragraphName = p.Name;
                string line = str.Substring( _paragraphName.Length );
                
                List<object> _params = new List<object>();
                if( p.Parameters.Count > 0 ) {
                    if( line.Contains(" using " ) ) {
                        line = line.Substring(" using ".Length);

                        int index = 0;
                        foreach( string _param in line.Split(new string[] {" and "}, StringSplitOptions.None) ) {
                            string param = _param;
                            if( HasVariableTypeDeclaration(param) ) {
                                GetVariableTypeFromDeclaration(param, out string kw);
                                param = param.Substring(kw.Length + 1);
                            }
                            object value = ParseVariable(param, report, variables, out VariableTypes got_type, fallback: p.Parameters[index].Item2);
                            if( got_type != p.Parameters[index].Item2 )
                                throw new FiMException($"Expected { p.Parameters[index].Item2 }, got { got_type }");
                            _params.Add( value );
                            index++;
                        }

                        if( _params.Count < p.Parameters.Count ) {
                            for( int x = _params.Count; x < p.Parameters.Count; x++ ) {
                                VariableTypes t = p.Parameters[x].Item2;
                                _params.Add( GetNullValue(t) );
                            }
                        }
                    } else {
                        p.Parameters.ForEach(x => {
                            _params.Add( GetNullValue( x.Item2 ) );
                        });
                    }
                }

                (object _value, VariableTypes _type) = p.Execute(report, _params);

                type = _type;
                return _value;
            }

            if( str.StartsWith("length of ") ) {
                string var_name = str.Substring("length of ".Length);
                if( !variables.ContainsKey( var_name ) )
                    throw new Exception($"Cannot find variable { var_name }");
                //if( !variables[ var_name ].IsArray() )
                if( !IsVariableTypeArray(variables[var_name].Type, true) )
                    throw new Exception($"Cannot get length of a non-array variable");
                
                var variable = variables[ var_name ];
                //type = VariableTypeArraySubType( variable.Type );
                type = VariableTypes.INTEGER;
                if( variable.Type == VariableTypes.STRING ) {
                    return ( variable.GetRawValue() as string ).Length - 2;
                } else {
                    return ( variable.GetRawValue() as Dictionary<int,object> ).Keys.Count;
                }
            }

            if( str.StartsWith("char of num " ) ) {
                string var = str.Substring("char of num ".Length);
                object value = ParseVariable(var, report, variables, out var value_type);
                if( value_type != VariableTypes.INTEGER ) 
                    throw new FiMException("Cannot get ASCII value of a non-number value");
                type = VariableTypes.CHAR;
                return (char)(Convert.ToSingle(value));
            }
            if( str.StartsWith("num of char ") ) {
                string var = str.Substring("num of char ".Length);
                object value = ParseVariable(var, report, variables, out var value_type);
                if( value_type != VariableTypes.CHAR ) 
                    throw new FiMException("Cannot get ASCII number of a non-char value");
                type = VariableTypes.INTEGER;

                string v = value.ToString();
                return (int)(char.Parse(v));
            }

            if( str.StartsWith("string of ") ) {
                string var = str.Substring("string of ".Length);
                object value = ParseVariable(var, report, variables, out var value_type);
                type = VariableTypes.STRING;
                if( value_type == VariableTypes.BOOLEAN )
                    return ((bool)value) ? "True" : "False";
                else
                    return value.ToString();
            }
            if( str.StartsWith("number of " ) ) {
                string var = str.Substring("number of ".Length);
                object value = ParseVariable(var, report, variables, out var value_type);
                type = VariableTypes.INTEGER;
                if( value_type == VariableTypes.STRING || value_type == VariableTypes.CHAR ) {
                    string v = value.ToString();
                    if( v.StartsWith("\"") && v.EndsWith("\"") )
                        v = v.Substring(1, v.Length-2);
                    return int.Parse(v);
                }
                else if( value_type == VariableTypes.BOOLEAN ) {
                    return ((bool)value) ? 1 : 0 ;
                }
            }
            
            {
                string[] sqrt = {"sqrt of ","square root of"};
                if( sqrt.Any(x => str.StartsWith(x)) ) {
                    string kw = sqrt.Where(x => str.StartsWith(x)).FirstOrDefault();
                    string var = str.Substring(kw.Length);
                    
                    object value = ParseVariable( var, report, variables, out var var_t );
                    if( var_t != VariableTypes.INTEGER )
                        throw new Exception($"Cannot square root a non-number value");
                    type = VariableTypes.INTEGER;
                    return Math.Sqrt( (double)Convert.ToSingle(value) );
                }
            }
            
            if( FiMConditional.HasConditional(str) ) {
                type = VariableTypes.BOOLEAN;
                return FiMConditional.Evaluate(str, report, variables);
            }

            // Applejack plus 1 of Apple Bloom

            if( IsMatchArray2(str,true) ) {
                (string _result, string variable_name, string variable_index, string _) = FiMMethods.MatchArray2( str,true );
                if( variables.ContainsKey(variable_name) ) {
                    (int, FiMVariable) result = ParseArray( variable_index, variable_name, report, variables );
                    type = VariableTypeArraySubType( result.Item2.Type );
                    return result.Item2.GetValue( result.Item1 ).Item1;
                }
            }
            else if( IsMatchArray1(str,true) ) {
                (string _result, string variable_name, int variable_index, string _) = FiMMethods.MatchArray1( str,true );
                if( variables.ContainsKey(variable_name) ) {
                    (int, FiMVariable) result = ParseArray( variable_index, variable_name, variables );
                    type = VariableTypeArraySubType( result.Item2.Type );
                    return result.Item2.GetValue( result.Item1 ).Item1;
                }
            }

            if( FiMArithmetic.IsArithmetic(str, out var arith_result) ) {
                var arithmetic = new FiMArithmetic( str, arith_result, report, variables );
                float value = arithmetic.Evaluate( report, variables );
                type = VariableTypes.INTEGER;
                return value;
            }

            if( run_once ) {
                throw new Exception($"Cannot convert value { str }");
            } else {
                // Sanitize string
                type = VariableTypes.STRING;
                return SanitizeString( str, report, variables );
            }
        }
    }
}

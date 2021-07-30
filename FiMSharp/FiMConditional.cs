using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.GlobalStructs;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Core
{
    public class FiMConditional
    {
        public static bool Evaluate(string str, FiMReport report, Dictionary<string, FiMVariable> variables)
        {
            string[] and_split = str.Split(new string[] {" and "}, StringSplitOptions.None);
            bool[] and_split_result = and_split.Select(x => false).ToArray();
            for( int a = 0; a < and_split.Length; a++ ) {
                string and_condition = and_split[a];
                string[] or_split = and_condition.Split(new string[] {" or " }, StringSplitOptions.None);
                bool[] or_split_result = or_split.Select(x => false).ToArray();
                for( int o = 0; o < or_split.Length; o++ ) {
                    or_split_result[o] = Calculate( or_split[o], report, variables );
                }
                and_split_result[ a ] = or_split_result.Any( x => x );
            }
            return and_split_result.All( x => x );
        }

        public static string[][] ToString( string str ) {
            List<string[]> result = new List<string[]>();
            string[] and_split = str.Split(new string[] {" and "}, StringSplitOptions.None);
            for( int a = 0; a < and_split.Length; a++ ) {
                string and_condition = and_split[a];
                List<string> _result = new List<string>();
                string[] or_split = and_condition.Split(new string[] {" or " }, StringSplitOptions.None);
                for( int o = 0; o < or_split.Length; o++ ) {
                    _result.Add( or_split[o] );
                }
                result.Add( _result.ToArray() );
            }
            return result.ToArray();
        }

        public static FiMConditionalKeyword GetConditional( string line ) {

            int flip = -1;
            foreach( string _l in line.Split('"') ) {

                flip *= -1;
                if( flip==-1 ) continue;

                if( Globals.Methods.Conditional_LessThanEqual.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_LessThanEqual.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, "<=" );
                }
                if( Globals.Methods.Conditional_GreaterThan.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_GreaterThan.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, ">" );
                }
                if( Globals.Methods.Conditional_GreaterThanEqual.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_GreaterThanEqual.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, ">=" );
                }
                if( Globals.Methods.Conditional_LessThan.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_LessThan.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, "<" );
                }
                if( Globals.Methods.Conditional_Not.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_Not.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, "!=" );
                }
                if( Globals.Methods.Conditional_Equal.Any(x => _l.Contains($" {x} ")) ) {
                    string keyword = Globals.Methods.Conditional_Equal.Where(x => _l.Contains($" {x} ")).FirstOrDefault();
                    return new FiMConditionalKeyword( keyword, "==" );
                }

            }

            throw FiMError.CreatePartial( FiMErrorType.CONDITIONAL_NOT_FOUND );
        }

        public static bool HasConditional( string line ) {

            int flip = -1;
            foreach( string _l in line.Split('"') ) {

                flip *= -1;
                if( flip==-1 ) continue;

                if( Globals.Methods.Conditional_LessThanEqual.Any(x => _l.Contains($" {x} ")) ) return true;
                if( Globals.Methods.Conditional_GreaterThan.Any(x => _l.Contains($" {x} ")) ) return true;
                if( Globals.Methods.Conditional_GreaterThanEqual.Any(x => _l.Contains($" {x} ")) ) return true;
                if( Globals.Methods.Conditional_LessThan.Any(x => _l.Contains($" {x} ")) ) return true;
                if( Globals.Methods.Conditional_Not.Any(x => _l.Contains($" {x} ")) ) return true;
                if( Globals.Methods.Conditional_Equal.Any(x => _l.Contains($" {x} ")) ) return true;

            }

            return false;
        }

        public static bool HasConditional( string line, out FiMConditionalKeyword result ) {
            try {
                result = GetConditional( line ); return true;
            } catch( Exception ) {
                result = new FiMConditionalKeyword(); return false;
            }
        }

        // i have no idea why but yeah
        private static bool Equals( FiMVariableStruct x, FiMVariableStruct y ) {
            dynamic _x;
            dynamic _y;

            switch( x.Type ) {
                case VariableTypes.BOOLEAN: _x = Convert.ToBoolean(x.Value); break;
                case VariableTypes.INTEGER: _x =  Convert.ToDouble(x.Value); break;
                case VariableTypes.STRING:  _x =  Convert.ToString(x.Value); break;
                case VariableTypes.CHAR:    _x =    Convert.ToChar(x.Value); break;
                default: _x = null; break;
            }
            switch( y.Type ) {
                case VariableTypes.BOOLEAN: _y = Convert.ToBoolean(y.Value); break;
                case VariableTypes.INTEGER:  _y = Convert.ToDouble(y.Value); break;
                case VariableTypes.STRING:   _y = Convert.ToString(y.Value); break;
                case VariableTypes.CHAR:       _y = Convert.ToChar(y.Value); break;
                default: _y = null; break;
            }

            return _x == _y;
        }
        public static bool Calculate(string left, string conditional, string right, FiMReport report, Dictionary<string, FiMVariable> variables) {
            object left_value;
            if( FiMMethods.HasVariableTypeDeclaration(left) ) {
                VariableTypes _ = FiMMethods.GetVariableTypeFromDeclaration( left, out string keyword );
                left = left.Substring( keyword.Length + 1 );
            }
            left_value = FiMMethods.ParseVariable(left, report, variables, out VariableTypes left_type);

            object right_value;
            if( FiMMethods.HasVariableTypeDeclaration(right) ) {
                VariableTypes _ = FiMMethods.GetVariableTypeFromDeclaration( right, out string keyword );
                right = right.Substring( keyword.Length + 1 );
            }
            right_value = FiMMethods.ParseVariable(right, report, variables, out VariableTypes right_type);

            if( left_value == null && right_value != null ) {
                left_type = right_type;
                left_value = FiMMethods.GetNullValue( left_type );
            }
            else if( right_value == null && left_value != null ) {
                right_type = left_type;
                right_value = FiMMethods.GetNullValue( left_type );
            }

            if( FiMMethods.IsVariableTypeArray( left_type ) || FiMMethods.IsVariableTypeArray( right_type ) )
                throw FiMError.CreatePartial( FiMErrorType.CONDITINAL_NO_ARRAYS );

            if( left_type != right_type )
                throw FiMError.CreatePartial( FiMErrorType.CONDITIONAL_DIFFERENT_TYPES );

            if( (conditional == ">" || conditional == "<" || conditional == ">=" || conditional == "<=") && (left_type != VariableTypes.INTEGER || right_type != VariableTypes.INTEGER) )
                throw FiMError.CreatePartial( FiMErrorType.CONDITIONALS_INTEGER_ONLY );

            bool check = false;

            switch( conditional ) {
                case "==": check = Equals(new FiMVariableStruct(left_value,left_type), new FiMVariableStruct(right_value,right_type)); break;
                case "!=": check = !Equals(new FiMVariableStruct(left_value,left_type), new FiMVariableStruct(right_value,right_type)); break;
                case ">":  check = Convert.ToDouble(left_value) >  Convert.ToDouble(right_value); break;
                case "<":  check = Convert.ToDouble(left_value) <  Convert.ToDouble(right_value); break;
                case ">=": check = Convert.ToDouble(left_value) >= Convert.ToDouble(right_value); break;
                case "<=": check = Convert.ToDouble(left_value) <= Convert.ToDouble(right_value); break;
            }
            return check;
        }
        public static bool Calculate(string line, FiMReport report, Dictionary<string, FiMVariable> variables)
        {
            string left, right;
            string kw; string conditional;
            {
                var c = GetConditional( line );
                kw = c.Keyword; conditional = c.Sign;
            }
            
            left  = line.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[0];
            right = line.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[1];

            return Calculate( left, conditional, right, report, variables);
        }
    }
}
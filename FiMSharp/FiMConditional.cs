using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FiMSharp.GlobalVars;

namespace FiMSharp
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

        public static (string, string) GetConditional( string line ) {
            if( Globals.Methods.Conditional_LessThanEqual.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_LessThanEqual.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, "<=");
            }
            if( Globals.Methods.Conditional_GreaterThan.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_GreaterThan.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, ">");
            }
            if( Globals.Methods.Conditional_GreaterThanEqual.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_GreaterThanEqual.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, ">=");
            }
            if( Globals.Methods.Conditional_LessThan.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_LessThan.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, "<");
            }
            if( Globals.Methods.Conditional_Not.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_Not.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, "!=");
            }
            if( Globals.Methods.Conditional_Equal.Any(x => line.Contains($" {x} ")) ) {
                string keyword = Globals.Methods.Conditional_Equal.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                return (keyword, "==");
            }
            throw new Exception("Conditional not found");
        }

        public static bool HasConditional( string line ) {
            if( Globals.Methods.Conditional_LessThanEqual.Any(x => line.Contains($" {x} ")) ) return true;
            if( Globals.Methods.Conditional_GreaterThan.Any(x => line.Contains($" {x} ")) ) return true;
            if( Globals.Methods.Conditional_GreaterThanEqual.Any(x => line.Contains($" {x} ")) ) return true;
            if( Globals.Methods.Conditional_LessThan.Any(x => line.Contains($" {x} ")) ) return true;
            if( Globals.Methods.Conditional_Not.Any(x => line.Contains($" {x} ")) ) return true;
            if( Globals.Methods.Conditional_Equal.Any(x => line.Contains($" {x} ")) ) return true;

            return false;
        }

        public static bool HasConditional( string line, out (string, string) result ) {
            try {
                result = GetConditional( line ); return true;
            } catch( Exception ) {
                result = ("", ""); return false;
            }
        }

        // what
        private static bool Equals( (object, VariableTypes) x, (object, VariableTypes) y ) {
            dynamic _x;
            dynamic _y;

            switch( x.Item2 ) {
                case VariableTypes.BOOLEAN: _x = Convert.ToBoolean(x.Item1); break;
                case VariableTypes.INTEGER: _x = Convert.ToSingle(x.Item1); break;
                case VariableTypes.STRING: _x = x.Item1.ToString(); break;
                case VariableTypes.CHAR: _x = Convert.ToChar(x.Item1); break;
                default: _x = null; break;
            }
            switch( y.Item2 ) {
                case VariableTypes.BOOLEAN: _y = Convert.ToBoolean(y.Item1); break;
                case VariableTypes.INTEGER: _y = Convert.ToSingle(y.Item1); break;
                case VariableTypes.STRING: _y = y.Item1.ToString(); break;
                case VariableTypes.CHAR: _y = Convert.ToChar(y.Item1); break;
                default: _y = null; break;
            }

            return _x == _y;
        }
        public static bool Calculate(string left, string conditional, string right, FiMReport report, Dictionary<string, FiMVariable> variables) {
            object left_value; VariableTypes left_type;
            if( FiMMethods.HasVariableTypeDeclaration(left) ) {
                VariableTypes _ = FiMMethods.GetVariableTypeFromDeclaration( left, out string keyword );
                left = left.Substring( keyword.Length + 1 );
            }
            left_value = FiMMethods.ParseVariable(left, report, variables, out left_type);

            object right_value; VariableTypes right_type;
            if( FiMMethods.HasVariableTypeDeclaration(right) ) {
                VariableTypes _ = FiMMethods.GetVariableTypeFromDeclaration( right, out string keyword );
                right = right.Substring( keyword.Length + 1 );
            }
            right_value = FiMMethods.ParseVariable(right, report, variables, out right_type);

            if( left_value == null && right_value != null ) {
                left_type = right_type;
                left_value = FiMMethods.GetNullValue( left_type );
            }
            else if( right_value == null && left_value != null ) {
                right_type = left_type;
                right_value = FiMMethods.GetNullValue( left_type );
            }

            if( FiMMethods.IsVariableTypeArray( left_type ) || FiMMethods.IsVariableTypeArray( right_type ) )
                throw new FiMException("Cannot do conditionals on arrays");

            if( left_type != right_type )
                throw new FiMException("Cannot do conditionals on different variable types");

            if( (conditional == ">" || conditional == "<" || conditional == ">=" || conditional == "<=") && (left_type != VariableTypes.INTEGER || right_type != VariableTypes.INTEGER) )
                throw new FiMException("Greater than/Less than conditionals can only be used on integers.");

            bool check = false;

            switch( conditional ) {
                case "==": check = Equals((left_value,left_type), (right_value,right_type)); break;
                case "!=": check = !Equals((left_value,left_type), (right_value,right_type)); break;
                case ">":  check = Convert.ToSingle(left_value) >  Convert.ToSingle(right_value); break;
                case "<":  check = Convert.ToSingle(left_value) <  Convert.ToSingle(right_value); break;
                case ">=": check = Convert.ToSingle(left_value) >= Convert.ToSingle(right_value); break;
                case "<=": check = Convert.ToSingle(left_value) <= Convert.ToSingle(right_value); break;
            }
            return check;
        }
        public static bool Calculate(string line, FiMReport report, Dictionary<string, FiMVariable> variables)
        {
            string left, right;
            (string kw, string conditional) = GetConditional( line );
            left  = line.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[0];
            right = line.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[1];

            return Calculate( left, conditional, right, report, variables);
        }
    }
}
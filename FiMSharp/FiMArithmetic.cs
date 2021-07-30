// Disabling this will make the DLL use DataTable instead of the switch statement.
#define ARITH_HARDCODED

using System;
using System.Collections.Generic;
using System.Linq;

using FiMSharp.GlobalStructs;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Core
{
    public class FiMArithmetic
    {
        public readonly string Left;
        public readonly string Arithmetic;
        public readonly string Right;

        private readonly Dictionary<string, string> shorthand = new Dictionary<string, string>()
        {
            { "Add", "+" },
            { "Subtract", "-" },
            { "Multiply", "*" },
            { "Divide", "/" },
            { "Remainder", "%" },
        };
        public FiMArithmetic(string left_, string arithmetic_, string right_) 
        {
            Left = left_;
            Arithmetic = shorthand[ arithmetic_ ];
            Right = right_;
        }
        public FiMArithmetic(string _line, FiMArithmethicResult check_result)
        {
            string t = check_result.Type;
            Arithmetic = shorthand[ t ];;

            string line = _line;
            if( check_result.IsPrefix ) {
                // Prefix
                string pre = Globals.Arithmetic[t].Prefix.Where(x => line.StartsWith(x)).FirstOrDefault();
                string preinf = Globals.Arithmetic[t].PrefixInfix.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                line = line.Substring( pre.Length + 1 );
                string[] s = line.Split( new string[] { $" {preinf} " }, StringSplitOptions.None );

                Left = s[0].Trim();
                Right = s[1].Trim();
            } else {
                // Infix
                string inf = Globals.Arithmetic[t].Infix.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                string[] s = line.Split( new string[] { $" {inf} " }, StringSplitOptions.None );
                Left = s[0].Trim();
                Right = s[1].Trim();
            }
        }

        public double Evaluate(FiMReport report, Dictionary<string, FiMVariable> variables)
        {
            object left_variable = FiMMethods.ParseVariable( Left, report, variables, out VariableTypes left_type );
            object right_variable = FiMMethods.ParseVariable( Right, report, variables, out VariableTypes right_type );

            if( left_type != VariableTypes.INTEGER || right_type != VariableTypes.INTEGER )
                throw FiMError.CreatePartial(FiMErrorType.ARITHMETIC_NUMBERS_ONLY);

            return Evaluate( Convert.ToDouble( left_variable ), Arithmetic, Convert.ToDouble( right_variable ) );
        }
        public static double Evaluate(double left_, string arithmetic_, double right_)
        {
            #if ARITH_HARDCODED
                double value = 0.0f;
                switch( arithmetic_ ) {
                    case "+": value = left_ + right_; break;
                    case "-": value = left_ - right_; break;
                    case "*": value = left_ * right_; break;
                    case "/": value = left_ / right_; break;
                    case "%": value = left_ % right_; break;
                }
                return value;
            #else
                var dt = new DataTable(); // /shrug
                var value = Convert.ToDouble(dt.Compute($"{ left_ } { arithmetic_ } {right_ }", ""));

                return value;
            #endif
        }

        /// <returns>out: (isPrefix, type)</returns>
        public static bool IsArithmetic( string line, out FiMArithmethicResult result ) {
            result = new FiMArithmethicResult( false, "" );

            foreach( string type in Globals.Arithmetic.Keys ) {
                var keywords = Globals.Arithmetic[ type ];
                
                if( keywords.Prefix.Any(x => line.StartsWith(x)) && keywords.PrefixInfix.Any(x => line.Contains($" {x} ")) ) {
                    result.IsPrefix = true;
                    result.Type = type;
                    return true;
                }
                else if( keywords.Infix.Any(x => line.Contains($" {x} ")) ) {
                    result.Type = type;
                    return true;
                }
            }

            return false;
        }
    }
}
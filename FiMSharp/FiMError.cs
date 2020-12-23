using System;
using FiMSharp.GlobalVars;

namespace FiMSharp.Error {

    public enum FiMErrorType {
        
        // General stuffs
        MISSING_PUNCTUATION,
        UNEXPECTED_TOKEN, // X
        VARAIBLE_DOESNT_EXIST, // X
        VARIABLE_ALREADY_EXISTS, // X
        UNEXPECTED_TYPE, // expected X, got Y
        TOO_MANY_CHARACTERS,
        RANGE_MUST_BE_NUMBER,
        EMPTY_INTERVAL,
        MISSING_PARAGRAPH, // X

        // Report
        TOO_MANY_MAIN_PARAGRAPH, // Might change soon when multiple main paragraphs becomes official in a next version. This is messy
        REPORT_VARIABLE_CREATION_ONLY,
        NO_END_OF_REPORT,
        NO_END_OF_PARAGRAPH, // X
        EXPECTED_END_OF_REPORT,
        REPORT_NAME_NOT_FOUND,
        STUDENT_NAME_NOT_FOUND,
        SCOPE_ERROR,
        SCOPE_REACHED_EOF,
        IF_STATEMENT_EXPECTED_END,
        INVALID_FOR_LOOP_TYPE,
        SWITCH_MULTIPLE_DEFAULT,
        MISSING_ELIPSIS,

        // Arithmetic
        ARITHMETIC_NUMBERS_ONLY,

        // Conditional
        CONDITIONAL_NOT_FOUND,
        CONDITIONAL_DIFFERENT_TYPES,
        CONDITINAL_NO_ARRAYS,
        CONDITIONALS_INTEGER_ONLY, // Only for greater than/less than

        // Method
        METHOD_INVALID_STRING, // X
        METHOD_INVALID_SUB_TYPE, // X
        METHOD_NON_NUMBER_ASCII,
        METHOD_NON_CHAR_ASCII,
        METHOD_CANNOT_GET_NULL, // X
        METHOD_NON_ARRAY_LENGTH,
        METHOD_NON_NUMBER_SQRT,
        METHOD_CANNOT_CONVERT,
        METHOD_ARRAY_INDEX_MUST_BE_INTEGER,

        // Paragraph
        INCREMENT_ONLY_NUMBERS,
        DECREMENT_ONLY_NUMBERS,
        PARAGRAPH_RETURNED_DIFFERENT_TYPE, // Expected X, got Y
        FORIN_VARIABLE_ARRAY_ONLY,
        FORTO_ELEMENT_NUMBER_ONLY,
        PARAGRAPH_NO_RETURN,

        // Tokenizer
        INVALID_VARIABLE_NAME,

        // Variable
        CANNOT_MODIFY_CONSTANT,
    }
    public class FiMError {

        private static string GetErrorString( FiMErrorType t ) {
            string e = "";

            switch( t ) {
                case FiMErrorType.MISSING_PUNCTUATION: e = "Line doesn't end with a punctuation"; break;
                case FiMErrorType.UNEXPECTED_TOKEN: e = "Unexpected token '{0}'"; break;
                case FiMErrorType.VARAIBLE_DOESNT_EXIST: e = "Variable '{0}' doesn't exist"; break;
                case FiMErrorType.UNEXPECTED_TYPE: e = "Expected type '{0}', got '{1}'"; break;
                case FiMErrorType.TOO_MANY_CHARACTERS: e = "Too many characters in character literal"; break;
                case FiMErrorType.RANGE_MUST_BE_NUMBER: e= "Range must be a number"; break;
                case FiMErrorType.EMPTY_INTERVAL: e = "Interval is empty"; break;
                case FiMErrorType.MISSING_PARAGRAPH: e = "Paragraph '{0}' doesn't exist"; break;

                case FiMErrorType.TOO_MANY_MAIN_PARAGRAPH: e = "Expected only one main paragraph"; break;
                case FiMErrorType.REPORT_VARIABLE_CREATION_ONLY: e = "Expected only variable creation outside of paragraphs"; break;
                case FiMErrorType.NO_END_OF_REPORT: e = "End of report not found"; break;
                case FiMErrorType.NO_END_OF_PARAGRAPH: e = "End of paragraph '{0}' not found"; break;
                case FiMErrorType.REPORT_NAME_NOT_FOUND: e = "Report name not found"; break;
                case FiMErrorType.STUDENT_NAME_NOT_FOUND: e = "Student name not found"; break;
                case FiMErrorType.SCOPE_ERROR: e = "Paragraph '{0}' has scoping error"; break;
                case FiMErrorType.SCOPE_REACHED_EOF: e = "Scope reached end of report"; break;
                case FiMErrorType.IF_STATEMENT_EXPECTED_END: e = "Expected else to be end of if-statement"; break;
                case FiMErrorType.INVALID_FOR_LOOP_TYPE: e = "Invalid for loop type"; break;
                case FiMErrorType.SWITCH_MULTIPLE_DEFAULT: e = "Multiple defaults in a switch statement"; break;
                case FiMErrorType.MISSING_ELIPSIS: e = "Missing elipsis"; break;

                case FiMErrorType.ARITHMETIC_NUMBERS_ONLY: e = "Only numbers can be used in arithmetics"; break;

                case FiMErrorType.CONDITIONAL_NOT_FOUND: e = "Conditional not found"; break;
                case FiMErrorType.CONDITIONAL_DIFFERENT_TYPES: e = "Cannot do conditionals on different variable types"; break;
                case FiMErrorType.CONDITINAL_NO_ARRAYS: e = "Cannot do conditionals on arrays"; break;
                case FiMErrorType.CONDITIONALS_INTEGER_ONLY: e = "Greater than or Less than conditions can only be used on variables"; break;

                case FiMErrorType.METHOD_INVALID_STRING: e = "Invalid string '{0}'"; break;
                case FiMErrorType.METHOD_INVALID_SUB_TYPE: e = "Cannot get sub-type of array type '{0}'"; break;
                case FiMErrorType.METHOD_NON_NUMBER_ASCII: e = "Cannot get ASCII value of a non-number value"; break;
                case FiMErrorType.METHOD_NON_CHAR_ASCII: e = "Canot get ASCII number of a non-char value"; break;
                case FiMErrorType.METHOD_CANNOT_GET_NULL: e = "Cannot get null value of '{0}'"; break;
                case FiMErrorType.METHOD_NON_ARRAY_LENGTH: e = "Cannot get length of a non-array variable"; break;
                case FiMErrorType.METHOD_NON_NUMBER_SQRT: e = "Cannot get square root of a non-number value"; break;
                case FiMErrorType.METHOD_CANNOT_CONVERT: e = "Cannot convert value '{0}'"; break;
                case FiMErrorType.METHOD_ARRAY_INDEX_MUST_BE_INTEGER: e = "Array index must be an integer"; break;

                case FiMErrorType.INCREMENT_ONLY_NUMBERS: e = "Cannot increment non-number variables"; break;
                case FiMErrorType.DECREMENT_ONLY_NUMBERS: e = "Cannot decrement non-number variables"; break;
                case FiMErrorType.PARAGRAPH_RETURNED_DIFFERENT_TYPE: e = "Paragraph expected return type '{0}', got '{1}'"; break;
                case FiMErrorType.FORIN_VARIABLE_ARRAY_ONLY: e = "For-in statement can only be used with arrays"; break;
                case FiMErrorType.FORTO_ELEMENT_NUMBER_ONLY: e = "For-to element must be a number type"; break;
                case FiMErrorType.PARAGRAPH_NO_RETURN: e = "Paragraph expected to return a variable"; break;

                case FiMErrorType.INVALID_VARIABLE_NAME: e = "Invalid variable name '{0}'"; break;

                case FiMErrorType.CANNOT_MODIFY_CONSTANT: e = "Cannot modify a constant variable"; break;

                default:
                    throw new NotImplementedException();
            }
            
            return e;
        }

        public static FiMPartialException CreatePartial( FiMErrorType t, params object[] f ) {
            string e = GetErrorString( t );
            string result = "";
            {
                switch( t ) {
                    
                    case FiMErrorType.METHOD_INVALID_SUB_TYPE: {
                        VariableTypes x = (VariableTypes)f[0];

                        result += string.Format(e, x.ToReadableString());
                        result += "\n";
                    }
                    break;

                    case FiMErrorType.PARAGRAPH_RETURNED_DIFFERENT_TYPE:
                    case FiMErrorType.UNEXPECTED_TYPE: {
                        VariableTypes x = (VariableTypes)f[0];
                        VariableTypes y = (VariableTypes)f[1];

                        result = string.Format(e, x.ToReadableString(), y.ToReadableString());
                    }
                    break;

                    default: {
                        result = string.Format(e, f) ;
                    }
                    break;
                }
            }
            return new FiMPartialException( result );
        }

        public static FiMException Create( FiMPartialException partial, string l, int i ) {
            string result = $"(Line { i+1 }): ";
            result += $"{ partial.Message }\n";
            result += $"{ l }\n";
            return new FiMException( result );
        }

        /// <summary>
        /// Create a FiMException with pre-made text
        /// </summary>
        /// <param name="i"><b>0-based</b> line index</param>
        /// <returns></returns>
        public static FiMException Create( FiMErrorType t, string l = "", int i = -1, params object[] f ) {
            
            string e = GetErrorString( t );
                
            string result = "";
            if (i >= 0)
                result = $"(Line { i+1 }): ";

            {
                switch( t ) {
                    
                    case FiMErrorType.METHOD_INVALID_SUB_TYPE: {
                        VariableTypes x = (VariableTypes)f[0];

                        result += string.Format(e, x.ToReadableString());
                        result += "\n";
                    }
                    break;

                    case FiMErrorType.PARAGRAPH_RETURNED_DIFFERENT_TYPE:
                    case FiMErrorType.UNEXPECTED_TYPE: {
                        VariableTypes x = (VariableTypes)f[0];
                        VariableTypes y = (VariableTypes)f[1];

                        result += string.Format(e, x.ToReadableString(), y.ToReadableString());
                        result += "\n";
                    }
                    break;

                    default: {
                        result += $"{ string.Format(e, f) }\n";
                    }
                    break;
                }
            }

            result += l + (string.IsNullOrWhiteSpace(l) ? "" : "\n" );

            return new FiMException( result );
        }

    }
}